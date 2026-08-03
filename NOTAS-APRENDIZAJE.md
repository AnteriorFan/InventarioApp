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

## Progreso (roadmap)

- [x] Esquema Oracle diseñado (tablas, constraints, FKs)
- [x] Tablas creadas en `inventario_dev`
- [x] `PKG_ITEMS` (listar, buscar_por_codigo, insertar, actualizar, eliminar)
- [x] Web.config (connection string + NuGet EF6/Oracle.ManagedDataAccess/.EntityFramework)
- [x] CRUD de Items — **Listar** (Index) funcionando contra Oracle real
- [x] CRUD de Items — **Insertar** (Create) funcionando contra Oracle real
- [ ] CRUD de Items — Editar / Eliminar (soft delete) contra Oracle real
- [ ] Login real (Forms Authentication + hash de contraseñas) + roles
- [ ] Historial/auditoría de acciones sobre items
- [ ] Listado con DataTables + modales Bootstrap AJAX
- [ ] Generación de código de barras/QR + pantalla de escaneo por cámara
- [ ] Integración AWS S3 para imágenes de items
- [ ] Módulo de movimientos de inventario (entradas/salidas)
- [ ] Inicializar repo Git

---

## Errores ya resueltos (buenos para repasar antes de entrevista)

| Error | Causa | Fix |
|---|---|---|
| "No hay proveedores de scaffolding compatibles" | Componente de Scaffolding no instalado/deprecado en VS2022 reciente | Usar "Add → Class" / "Add → New Item" en vez de los wizards |
| `ORA-12541: no listener` | Servicio `OracleServiceXE`/TNSListener no corría (instalación de Oracle XE falló a medias) | Reinstalar como Administrador; verificar servicios con `Get-Service` |
| `PLS-00201: identificador SP_LISTAR_ITEMS debe declararse` | Se llamó al procedure sin el prefijo del package | `pkg_items.sp_listar` en vez de `sp_listar_items` suelto |
| Página `/Items` sin datos, sin error | Los datos de prueba se insertaron después de la primera carga / no se había hecho refresh | Simplemente recargar la página tras el INSERT+COMMIT |
| `InvalidCastException: no se puede convertir OracleDecimal a IConvertible` en `Convert.ToInt32(param.Value)` | Oracle `NUMBER` es de precisión arbitraria, no mapea a un tipo .NET nativo — ODP.NET regresa el valor de un parámetro `OUT` envuelto en su propio tipo `OracleDecimal`, que no implementa `IConvertible` | `Convert.ToInt32(param.Value.ToString())` — convertir a string primero evita el problema |
