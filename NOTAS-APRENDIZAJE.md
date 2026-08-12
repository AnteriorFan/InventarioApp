# Notas de aprendizaje — InventarioApp

Proyecto de práctica para dominar el stack: **ASP.NET MVC5 (.NET Framework) + Oracle 19c/PL-SQL + jQuery/Bootstrap5 + EF6 + Oracle.ManagedDataAccess + AWS S3**, orientado a entrevistas/trabajos que usan este stack legacy.

Este archivo se va actualizando conforme avanzamos. Es para repasar antes de entrevistas — prioriza el "por qué" sobre el "qué".

---

## Stack y entorno

- **Visual Studio Community 2022**, workload "ASP.NET and web development" + componentes individuales extra necesarios (no vienen por defecto): **".NET Framework project and item templates"** y **".NET Framework 4.8 SDK/targeting pack"**. Sin esto, la plantilla "ASP.NET Web Application (.NET Framework)" no aparece en New Project.
- El **scaffolding automático** ("Add → Controller", "Add → View...") no funciona en esta instalación (falta un componente que ya no se encuentra fácil en versiones recientes de VS). Workaround permanente: usar **"Add → Class"** para Controllers/Services/Repositories, y **"Add → New Item → MVC View Page (Razor)"** para vistas — nunca los wizards con "...".
- **Oracle Database XE 21c** (no es 19c exacto, pero el SQL/PL-SQL es equivalente para practicar). Usa arquitectura **multitenant**: conectar por **Nombre del Servicio = `XEPDB1`**, NO por SID `xe` (el SID apunta al contenedor raíz CDB, no es donde debe vivir tu app).
- Después de instalar Oracle XE, verificar que los servicios de Windows `OracleServiceXE` y `OracleXXTNSListener` estén **Running** (`Get-Service -Name "Oracle*"`). El error `ORA-12541: no listener` significa que estos servicios no están corriendo.
- Usuario dedicado para la app: `inventario_user` (nunca desarrollar contra `system`/`sys`). Roles: `CONNECT, RESOURCE` + `QUOTA UNLIMITED ON USERS`.

---

## Arquitectura de la app

```
Controllers/     → HTTP-facing, valida input, llama al Service
Services/        → lógica de negocio, orquesta Repositories
Repositories/    → único lugar con código Oracle/EF6 (Database.SqlQuery / ExecuteSqlCommand)
Models/          → POCOs planos, sin lógica
```

MVC5 en sí **solo define Model-View-Controller** — Service y Repository son un patrón que se agrega encima (no viene generado), pero es el estándar en shops reales con este stack.

**Patrón de "DI a mano"** (MVC5 clásico no trae contenedor de inyección de dependencias como Spring/`@Autowired`):
```csharp
public ItemService() : this(new ItemRepository()) { }        // constructor de conveniencia
public ItemService(IItemRepository itemRepository) { ... }    // para tests con mocks
```
El constructor vacío centraliza cómo se arma la dependencia por defecto — evita que cada Controller que use el Service tenga que saber de qué está hecho por dentro.

---

## Esquema de base de datos (Oracle)

```
CATEGORIAS (id_categoria PK, nombre, descripcion)

ITEMS (id_item PK, codigo UNIQUE, nombre, descripcion, id_categoria FK,
       cantidad CHECK>=0, unidad_medida, ubicacion, imagen_s3_key,
       activo CHECK IN('S','N'), fecha_creacion, fecha_modif)

MOVIMIENTOS_INVENTARIO (id_movimiento PK, id_item FK, tipo_movimiento CHECK IN('ENTRADA','SALIDA'),
                         cantidad CHECK>0, fecha, observaciones)

USUARIOS (id_usuario PK, nombre, apellido, usuario_login UNIQUE, password_hash,
          rol CHECK IN('EMPLEADO','ADMIN','JEFE'), activo, fecha_creacion)

HISTORIAL_ITEMS (id_historial PK, id_item FK, id_usuario FK, accion CHECK IN('ALTA','MODIFICACION','BAJA'),
                  fecha, detalle)
```

**Regla de modelado importante que se corrigió en el diseño**: en una relación 1:N, el FK siempre va en el lado "muchos". Un usuario tiene MUCHAS acciones en el historial, así que el FK (`id_usuario`) vive en `HISTORIAL_ITEMS`, no al revés.

`GENERATED ALWAYS AS IDENTITY` se usa en vez del patrón viejo secuencia+trigger (más limpio, disponible desde 12c).

`activo` en vez de `DELETE` físico — **borrado lógico**, estándar en sistemas de auditoría/inventario para no perder historial.

---

## PL/SQL — PKG_ITEMS

Package con: `sp_listar`, `sp_buscar_por_codigo` (ambos regresan `SYS_REFCURSOR`), `sp_insertar` (regresa el ID nuevo vía `OUT` + `RETURNING ... INTO`), `sp_actualizar`, `sp_eliminar` (soft delete).

Conceptos clave:
- **Package = Spec (contrato público) + Body (implementación)**, como interfaz + clase.
- **`SYS_REFCURSOR` como parámetro `OUT`**: un procedure no puede "devolver una tabla" como una función; se le pasa un cursor vacío por referencia, se hace `OPEN cursor FOR SELECT...`, y el código C# lo lee como un DataReader.
- **`%TYPE`** (ej. `items.codigo%TYPE`): ancla el tipo del parámetro al de la columna real — si la columna cambia de tamaño, el procedure se ajusta solo.
- **Siempre llamar `nombre_package.nombre_procedure`**, nunca el procedure suelto — es un error clásico (`PLS-00201: identificador debe declararse`) si se te olvida el prefijo del package.
- Los procedures **no hacen `COMMIT`/`ROLLBACK` internamente** — eso lo controla quien llama (el código C#/EF6), para poder envolver varias operaciones en una sola transacción atómica (relevante cuando construyamos `sp_registrar_movimiento`, que va a tocar dos tablas a la vez).
- **Gotcha de EF6 + REF CURSOR**: `Database.SqlQuery<Item>` mapea columnas a propiedades **por nombre exacto** (case-insensitive, pero NO convierte `snake_case` a `PascalCase`). Hay que ponerle `AS Id`, `AS IdCategoria`, etc. a cada columna del `SELECT` dentro del cursor para que coincida con las propiedades de la clase `Item`.

---

## C# / EF6 — patrón de acceso a datos

```csharp
// Lectura (regresa cursor)
var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
using (var connection = new OracleConnection(connectionString))
using (var db = new DbContext(connection, true))
{
    return db.Database.SqlQuery<Item>("BEGIN pkg_items.sp_listar(:p_cursor); END;", cursorParam).ToList();
}

// Escritura (sin cursor, con OUT param simple)
db.Database.ExecuteSqlCommand("BEGIN pkg_items.sp_insertar(...); END;", parametros...);
```

- `new DbContext(connection, true)` — un DbContext "vacío" sin modelo mapeado, usado solo como vehículo para `Database.SqlQuery`/`ExecuteSqlCommand`. No es el ORM tradicional de EF (mapear tablas a clases); es el patrón que se usa en shops que ya tienen toda la lógica en PL/SQL.
- `SqlQuery` = cuando esperas un result set de vuelta. `ExecuteSqlCommand` = cuando solo ejecutas (INSERT/UPDATE/DELETE).
- `(object)valor ?? DBNull.Value` — en C#, `null` y `DBNull.Value` son cosas distintas; hay que convertir explícitamente al mandar parámetros opcionales a Oracle.
- Se necesitan DOS paquetes NuGet de Oracle, no uno: `Oracle.ManagedDataAccess` (driver ADO.NET) **y** `Oracle.ManagedDataAccess.EntityFramework` (el "traductor" que EF6 necesita para saber hablar con ese driver). Sin el segundo, truena en runtime con "No Entity Framework provider found...".

---

## MVC5 — fundamentos cubiertos

- **Ciclo de vida de una request**: URL → `RouteConfig.cs` (tabla de rutas) → Controller → Action → busca la View por convención (`Views/{Controller}/{Action}.cshtml`) → HTML.
- **Razor (`.cshtml`)**: no es HTML plano, se compila a una clase C#. Por eso `@ViewBag`, `@Model`, `@foreach` funcionan ahí y no en un `.html` normal.
- **`_ViewStart.cshtml` + `_Layout.cshtml`**: layout compartido (navbar/footer) que envuelve automáticamente cada vista vía `@RenderBody()` — evita repetir HTML común. Una vista normal NO debe traer su propio `<html><head><body>`, solo el contenido.
- **Model Binding**: en un POST, MVC arma el objeto C# automáticamente a partir de los campos del form, siempre que el `name` del input coincida con el nombre de la propiedad (`Html.TextBoxFor(m => m.Codigo)` genera `name="Codigo"` solo).
- **PRG (Post-Redirect-Get)**: después de un POST que guarda datos, se hace `RedirectToAction` (no `return View()` directo) para que un refresh del navegador no reenvíe el formulario.
- **`ViewBag` vs `Model` tipado**: `ViewBag` es dinámico (sin IntelliSense, sin aviso de errores de nombre); `@model Item` + `Model.Nombre` es fuertemente tipado — se prefiere esto último.
- **MVC5 es Server-Side Rendering (SSR)**, no SPA: el servidor genera el HTML completo por request. jQuery + `$.ajax` se usa encima para actualizar partes sin recargar (progressive enhancement) — patrón híbrido típico en shops legacy con este stack.

---

## Decisiones de arquitectura (con el porqué)

- **Forms Authentication, no JWT**: JWT resuelve el problema de auth stateless entre un cliente separado y una API (SPA en otro dominio, apps móviles, microservicios). Esta app es SSR clásico con jQuery en el mismo dominio — Forms Authentication (cookie firmada, enviada automáticamente por el navegador en cada request, incluidos los AJAX) es la herramienta nativa de MVC5 y evita reinventar con JWT algo que las cookies ya resuelven gratis. `[Authorize(Roles="ADMIN")]` va a mapear directo con la columna `rol` de `usuarios`.
- **Password hashing con `Rfc2898DeriveBytes` (PBKDF2)**: viene incluido en `System.Security.Cryptography`, sin NuGet extra. Nunca contraseñas en texto plano ni hash simple sin salt (MD5/SHA256 solo son vulnerables a rainbow tables).
- **Carpetas `Controllers/Services/Repositories/Models` en la raíz del proyecto**, no una carpeta `src/` — no es idiomático en .NET (eso es convención de JS/Node). El equivalente real en .NET para separar capas físicamente es usar **proyectos de Class Library separados** dentro de la misma Solution, referenciados entre sí — considerarlo como ejercicio futuro, no ahora.
- **Rol como `VARCHAR2` + `CHECK`** en vez de tabla `ROLES` aparte — para 3 valores fijos es más simple; normalizar a tabla aparte solo si se necesita practicar joins extra o el set de roles crece.

---

## Login real — Forms Authentication + PBKDF2

- **Hashing**: `Security/PasswordHasher.cs` usa `Rfc2898DeriveBytes` (PBKDF2, SHA256, 100k iteraciones). Genera una sal aleatoria de 16 bytes por contraseña (guardada junto al hash en el mismo string Base64) — así dos usuarios con la misma contraseña nunca tienen el mismo hash guardado.
- **`PKG_USUARIOS.sp_obtener_por_login`**: trae el usuario (incluyendo `password_hash` y `rol`) por su login, filtrando `activo = 'S'`.
- **`AccountController`**: `Login()` GET muestra el form, `Login(LoginViewModel)` POST valida credenciales vía `AuthService`, arma un `FormsAuthenticationTicket` (guardando el **rol** en `UserData`), lo encripta con `FormsAuthentication.Encrypt` y lo manda como cookie. `Logout()` usa `FormsAuthentication.SignOut()`.
- **Pieza que casi se pasa por alto**: Forms Authentication por sí sola solo sabe *quién eres* (username), no *qué rol tienes*. Hay que reconstruir el `IPrincipal` con los roles en `Global.asax → Application_PostAuthenticateRequest`, leyendo el `UserData` del ticket y armando un `GenericPrincipal` — sin esto, `[Authorize(Roles="ADMIN")]` nunca funciona aunque el login sí.
- **`[Authorize]`** en la clase `ItemsController` protege todo el controller; `[Authorize(Roles = "ADMIN,JEFE")]` en `Delete` restringe solo esa action a esos roles.
- **Gotcha de debugging**: cambios en atributos de clase (`[Authorize]`, etc.) no se reflejan solo guardando el archivo — hay que **detener la app y recompilar** (a diferencia de las vistas `.cshtml`, que sí actualizan en caliente). Si un `[Authorize]` "no funciona", antes de sospechar del código, primero descarta que estés corriendo una versión vieja compilada.
- **`Request.IsAuthenticated` / `User.Identity.Name`** en las vistas (`_Layout.cshtml`) para mostrar "Hola, {usuario}" / "Cerrar sesión" vs "Iniciar sesión" — viene gratis una vez que Forms Auth está configurado, sin lógica extra.

## DataTables + modales Bootstrap por AJAX

- **Vistas reutilizables como partial + full page**: `if (Request.IsAjaxRequest()) { Layout = null; }` al inicio de una vista permite que el MISMO archivo (`Create.cshtml`, `Edit.cshtml`) sirva tanto para navegación directa (con layout completo) como para contenido cargado dentro de un modal por AJAX (sin layout, para no duplicar navbar/footer dentro del modal).
- **Flujo del modal**: click en botón (`data-url="..."`) → `$.get(url)` trae el HTML del form → se inyecta en `#modalContent` → `bootstrap.Modal(...).show()`. Al enviar el form, jQuery intercepta el `submit` (`e.preventDefault()`), lo manda por `$.post()`, y si el Controller responde `{success:true}`, se recarga la página (`location.reload()` — más simple que reconciliar DataTables a mano con los datos nuevos).
- **Regla crítica**: el `if (Request.IsAjaxRequest()) return Json(new {success=true});` va **solo en las actions que GUARDAN datos** (los POST). El GET que **muestra** el formulario siempre regresa `View(...)`, nunca `Json` — si se te cuela ese `if` en el GET, truena con `InvalidOperationException` porque MVC bloquea `Json` en respuesta a un GET por seguridad (a menos que uses `JsonRequestBehavior.AllowGet` — que si SÍ quieres exponer un endpoint JSON por GET a propósito, como hicimos con `BuscarPorCodigo`, ahí sí hay que ponerlo explícito).
- **`Html.BeginForm` tiene varios overloads y es fácil confundirlos**: `Html.BeginForm(new { id = "x" })` usa el overload de **route values**, NO de atributos HTML — el form generado NO tendrá `id="x"` como atributo. Para atributos HTML explícitos: `Html.BeginForm("Action", "Controller", FormMethod.Post, new { id = "x" })` (el 3er argumento tiene que ser el enum `FormMethod`, no un objeto, para que el compilador elija el overload correcto).
- **Flash messages tras un `location.reload()`**: una variable JS normal no sobrevive a un reload. `sessionStorage` sí — se guarda el mensaje justo antes de recargar, y en el siguiente `$(document).ready()` se lee, se muestra como alert de Bootstrap, y se borra.
- **DataTables**: se inicializa con `$('#tabla').DataTable({...})` — CDN de DataTables + su integración de Bootstrap 5 (`dataTables.bootstrap5.min.css/js`), cargados después de jQuery en `_Layout.cshtml`.

## Código de barras / QR

- **Generación**: 100% client-side, sin ir al servidor — el `Codigo` de cada item ya está en el HTML (`data-codigo="@item.Codigo"`), así que `JsBarcode("#svg", codigo, {format:"CODE128"})` y `new QRCode(div, codigo)` (ambos por CDN) generan la imagen directo en el navegador.
- **Pistola lectora física**: casi todas funcionan en modo "keyboard wedge" — el sistema operativo la reconoce como un teclado (`HID Keyboard Device`), y al escanear "escribe" el texto decodificado en el campo que tenga el foco, seguido de un Enter automático. No requiere ninguna librería ni permisos especiales — un `<input>` enfocado + un listener de `keypress` (código 13 = Enter) es suficiente. Confirmado funcionando con hardware real.
- **Cámara del celular**: librería `html5-qrcode` (CDN) usa `getUserMedia()` para acceder a la cámara y decodificar en tiempo real — soporta QR y varios formatos de barras 1D.
- Ambos flujos (pistola y cámara) convergen en el mismo endpoint: `GET /Items/BuscarPorCodigo?codigo=X`, que sí usa `JsonRequestBehavior.AllowGet` a propósito (ver arriba).

## Interfaz: capa de tema, tarjeta de item y dashboard

Rediseño completo de UX/UI. Decisiones de diseño, design tokens, anatomía de cada componente y los gotchas técnicos (encoding sin BOM, `.csproj` desincronizado, funciones analíticas de Oracle para el ABC) están documentados aparte en [UI-UX.md](UI-UX.md).

Lo más importante para repasar de ahí:

- **`.cshtml` guardado en UTF-8 sin BOM sale con los acentos rotos** ("Ãºltimos" en vez de "últimos"). ASP.NET lo lee con la codepage de Windows al compilarlo. El `<meta charset>` del HTML NO tiene nada que ver: ese define cómo se *envía* la respuesta, el problema es cómo se *lee* el fuente. Fix de raíz: `<globalization fileEncoding="utf-8" />` en Web.config.
- **El `.csproj` de MVC5 lista cada archivo a mano.** Un `.cs` que no esté en un `<Compile Include>` no se compila, sin error ni aviso. Si dos editores tienen el proyecto abierto a la vez, el que guarde último pisa al otro y se pierden entradas en silencio.
- **Funciones analíticas de Oracle** (`RATIO_TO_REPORT`, `SUM() OVER (ORDER BY ... ROWS BETWEEN ...)`): a diferencia de `GROUP BY`, no colapsan filas — cada fila conserva su renglón y además recibe un valor calculado sobre todo el conjunto.
- **Un `int?` en un ViewModel puede ser una decisión de diseño, no un descuido.** `DiasCobertura = NULL` significa "no se puede proyectar"; un `int` lo aplastaría a cero y la pantalla mentiría.

## Aclaración de arquitectura (repaso)

- **Model** no es "plantilla de la base de datos" — es la forma de los datos que la app maneja en C#, no siempre 1:1 con una tabla (`LoginViewModel` no corresponde a ninguna tabla).
- **Repository** = único lugar con código Oracle/EF6.
- **Service** = donde debería vivir la lógica de negocio — honestamente, en este proyecto la mayoría son passthrough delgado hacia el Repository; reglas como el `[Authorize(Roles=...)]` de `Delete` viven en el Controller, no en el Service, que en rigor es una zona gris.
- **Controller** no "llama" a la vista como una función — regresa un `ActionResult` (`View(modelo)`), y es el motor de Razor el que combina eso con el `.cshtml` para generar el HTML final.

## Roadmap futuro — sistema de gestión de activos (EN CONSTRUCCIÓN)

Se propuso un esquema mucho más completo (sistema de gestión de activos de TI: Edificios/Áreas/Espacios, Marca/Modelo normalizados, Estados, historial de movimientos físicos, galería de imágenes) — un salto grande respecto al inventario simple de abajo. **Ya no es solo una propuesta: se está construyendo de verdad, en paralelo al inventario simple** (que se queda intacto, sin tocar). Progreso completo, decisiones de arquitectura y patrones técnicos nuevos (roles/permisos, `LEFT JOIN` en FKs opcionales, `AuthorizeAttribute` personalizado, índices en FK) documentados en [ROADMAP-FUTURO.md](ROADMAP-FUTURO.md).

**Estado**: Ubicaciones, Roles/Permisos, Catálogos (Marca/Modelo/Estados/TipoMovimiento) y Activos ya completos. Falta: historial de movimientos de Activos, galería de imágenes de Activos.

## Progreso (roadmap)

- [x] Esquema Oracle diseñado (tablas, constraints, FKs)
- [x] Tablas creadas en `inventario_dev`
- [x] `PKG_ITEMS` (listar, buscar_por_codigo, obtener_por_id, insertar, actualizar, eliminar)
- [x] Web.config (connection string + NuGet EF6/Oracle.ManagedDataAccess/.EntityFramework)
- [x] CRUD de Items completo (Listar, Insertar, Editar, Eliminar) funcionando contra Oracle real
- [x] Repo Git inicializado y conectado a GitHub
- [x] Login real (Forms Authentication + hash PBKDF2) + roles + protección de rutas
- [x] Historial/auditoría de acciones sobre items (con usuario responsable)
- [x] Dropdown de Categoría en Crear/Editar (reemplazó el campo numérico crudo)
- [x] Listado con DataTables + modales Bootstrap AJAX (Crear/Editar/Historial en modal)
- [x] Generación de código de barras/QR + pantalla de escaneo (cámara y pistola física, ambas probadas)
- [x] Módulo de movimientos de inventario (entradas/salidas, con transacción real y validación de stock)
- [ ] **Rotar la contraseña de Oracle expuesta** (sigue pendiente — `ConnectionStrings.config` local todavía tiene la contraseña vieja que se filtró; recordatorio #4 y contando)
- [ ] Integración AWS S3 para imágenes de items

## Movimientos de inventario — transacciones reales + manejo de errores

Este fue el primer feature del proyecto construido en su mayoría por cuenta propia (con revisión guiada) — buen resumen de errores propios que vale la pena repasar.

- **`sp_registrar`**: primer procedure del proyecto que hace más de una operación de escritura en la misma transacción — lee la cantidad actual con `SELECT ... FOR UPDATE` (bloquea la fila para evitar condiciones de carrera entre dos movimientos simultáneos sobre el mismo item), valida stock suficiente si es `SALIDA`, actualiza `items.cantidad`, e inserta el movimiento — todo sin `COMMIT` interno, igual que el resto de los packages.
- **`RAISE_APPLICATION_ERROR(-20001, 'mensaje')`**: la forma de lanzar un error personalizado en PL/SQL (equivalente a `throw new Exception(...)`). El código debe estar en el rango -20000 a -20999. Llega a C# como una `OracleException`.
- **Bug real que se cometió y corrigió**: `ModelState.AddModelError(...)` seguido de `RedirectToAction(...)` — el mensaje de error se pierde silenciosamente, porque `ModelState` no sobrevive un redirect (solo sirve si la MISMA request hace `return View(...)`). Fix: usar `TempData["Error"] = "..."` en su lugar, que sí está diseñado para sobrevivir exactamente un redirect, y leerlo en la vista destino con `@if (TempData["Error"] != null)`.
- **`Database.SqlQuery<T>` requiere propiedades (`{ get; set; }`), no campos públicos** (`public int Id;`) — con campos, el mapeo falla en silencio (todos los valores se quedan en su default) sin ningún error visible. Bug sutil, fácil de cometer al escribir un Model rápido.
- **ViewModel para combinar datos de más de una fuente**: `DetalleItemViewModel` (con `Item` + `List<MovimientoInventario>`) para la vista `Detalles` — mismo principio que `LoginViewModel`, no fuerces un Model existente a cargar datos que no le corresponden.
- [ ] Módulo de movimientos de inventario (entradas/salidas) — **siguiente ejercicio, a hacer sin ayuda de código completo**

---

## Errores ya resueltos (buenos para repasar antes de entrevista)

| Error | Causa | Fix |
|---|---|---|
| "No hay proveedores de scaffolding compatibles" | Componente de Scaffolding no instalado/deprecado en VS2022 reciente | Usar "Add → Class" / "Add → New Item" en vez de los wizards |
| `ORA-12541: no listener` | Servicio `OracleServiceXE`/TNSListener no corría (instalación de Oracle XE falló a medias) | Reinstalar como Administrador; verificar servicios con `Get-Service` |
| `PLS-00201: identificador SP_LISTAR_ITEMS debe declararse` | Se llamó al procedure sin el prefijo del package | `pkg_items.sp_listar` en vez de `sp_listar_items` suelto |
| Página `/Items` sin datos, sin error | Los datos de prueba se insertaron después de la primera carga / no se había hecho refresh | Simplemente recargar la página tras el INSERT+COMMIT |
| `InvalidCastException: no se puede convertir OracleDecimal a IConvertible` en `Convert.ToInt32(param.Value)` | Oracle `NUMBER` es de precisión arbitraria, no mapea a un tipo .NET nativo — ODP.NET regresa el valor de un parámetro `OUT` envuelto en su propio tipo `OracleDecimal`, que no implementa `IConvertible` | `Convert.ToInt32(param.Value.ToString())` — convertir a string primero evita el problema |
| Credenciales de Oracle expuestas en un repo público de GitHub | Se agregó una regla a `.gitignore` DESPUÉS de que el archivo ya estaba commiteado — `.gitignore` solo previene que archivos nuevos se empiecen a trackear, no oculta retroactivamente uno que ya está en el historial | `git rm --cached archivo` + commit + push para dejar de rastrearlo; **rotar la credencial siempre**, porque el valor viejo sigue visible en los commits anteriores del historial para siempre. Patrón correcto desde el inicio: `<connectionStrings configSource="ConnectionStrings.config" />` en Web.config, con el archivo real listado en `.gitignore` *antes* del primer commit |
| `PLS-00323: subprograma declarado en el spec debe definirse en el body` | Se agregó un procedure nuevo al **spec** del package pero se corrió un body viejo que no lo incluía (`CREATE OR REPLACE PACKAGE BODY` reemplaza TODO el body) | Spec y body siempre deben declarar exactamente los mismos procedures — al agregar uno, hay que re-correr el body COMPLETO con el nuevo incluido |
| `InvalidOperationException: JsonResult bloqueado en GET, falta JsonRequestBehavior.AllowGet` | Un action GET (el que muestra un formulario) tenía por error el mismo `if (Request.IsAjaxRequest()) return Json(...)` que solo debía ir en el POST que guarda | Ese `if` va solo en actions que GUARDAN datos; el GET que muestra el form siempre regresa `View(...)` |
| `El tipo ya define un miembro 'X' con los mismos tipos de parámetro` | Se pegó una versión nueva de un método sin borrar la versión vieja — quedaron dos métodos con el mismo nombre y firma | Reemplazar el bloque completo del método viejo, no pegar el nuevo aparte |
| Bug de un solo carácter (paréntesis faltante en Razor) que generaba un error de runtime totalmente distinto y confuso | Una vista con error de sintaxis no compila; VS a veces sigue sirviendo la última versión compilada, dando errores que no corresponden al código visible en pantalla | Cuando un error no tiene sentido con el código que ves, sospecha de un error de sintaxis en otro archivo que impide recompilar — revisa la vista completa, no solo la lógica |
| Subida de imagen "no truena pero tampoco funciona" (sin excepción visible ni en el servidor) | `form.serialize()` de jQuery **no incluye archivos** — solo serializa campos de texto a un string. El `enctype="multipart/form-data"` del `<form>` HTML solo aplica a un submit nativo del navegador; al interceptar el submit con JS y mandarlo con `$.post()`, ese enctype se ignora por completo | Usar `new FormData(form[0])` en vez de `.serialize()`, y en la llamada AJAX poner `processData: false, contentType: false` — así el navegador arma el `Content-Type: multipart/form-data` correcto con el boundary, y si archivos SÍ viajan |
| Subida de imagen truena la app / IIS Express se cae | El límite default de ASP.NET clásico para el tamaño de un request es de solo 4 MB (`httpRuntime.maxRequestLength`) — una foto de celular fácilmente lo supera | Configurar explícitamente `<httpRuntime maxRequestLength="10240" />` (en KB) Y `<security><requestFiltering><requestLimits maxAllowedContentLength="10485760" /></requestFiltering></security>` (en bytes, límite de IIS) — son DOS capas distintas, hay que subir ambas |
| Debugger no atrapa una excepción real | Por defecto VS solo rompe en excepciones NO manejadas; algo puede estar "tragándose" el error antes de que llegue a mostrarse | `Depurar → Windows → Configuración de excepciones` (`Ctrl+Alt+E`) → marcar "Common Language Runtime Exceptions" — rompe en CUALQUIER excepción lanzada, aunque algo la atrape después |
