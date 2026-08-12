# Diseño de interfaz — InventarioApp

Documento de referencia del rediseño de UX/UI. Igual que [NOTAS-APRENDIZAJE.md](NOTAS-APRENDIZAJE.md), prioriza el **por qué** sobre el **qué**: el CSS se puede leer en el archivo, lo que no se puede leer ahí es la razón de cada decisión.

**Estado:** implementado. Ver [Qué se construyó](#qué-se-construyó).

---

## El punto de partida

La app funcionaba por dentro (Oracle, PL/SQL, Forms Auth, permisos, escaneo de códigos) pero se veía como la plantilla default de Visual Studio:

- Navbar que decía literalmente "Nombre de la aplicación".
- Páginas "Acerca de" y "Contacto" del template, en inglés, hablando de ASP.NET.
- `Site.css` con 20 líneas, de las cuales una era un bug activo.
- Home page que promocionaba el framework en vez de mostrar el inventario.

El problema no era estético. Era que **la app tenía datos valiosos que no le enseñaba a nadie**: movimientos, historial, existencias, roles. Todo estaba en la base, y la pantalla de inicio hablaba de NuGet.

---

## Los tres objetivos

1. **Identidad visual consistente y minimalista**, sin romper lo que ya funcionaba.
2. **Crear/Editar item como tarjeta**: imagen a un lado, datos al otro, códigos abajo.
3. **Home convertida en dashboard operativo**, con métricas que de verdad sirvan para operar un almacén.

---

## Decisiones de diseño (con el porqué)

### Capa de tema sobre Bootstrap, no CSS desde cero

Bootstrap 5.3 expone casi todo su diseño como variables CSS propias (`--bs-body-bg`, `--bs-primary`, `--bs-border-radius`…). Redefinirlas en `:root` cambia **todos** los componentes que las consumen — botones, cards, tablas, modales, badges — desde un solo lugar.

La alternativa (CSS propio desde cero) obligaba a reconstruir a mano navbar, modales, dropdowns, tablas y la integración con DataTables. Semanas de trabajo para llegar al mismo sitio, con el riesgo de romper las 12 pantallas que ya funcionaban.

**Regla derivada:** si hay que pelear con un selector a punta de `!important`, casi siempre es que existe una variable `--bs-*` que hace el mismo trabajo sin pelear.

### Un solo color de acento

Todo lo interactivo es azul (`--accent`). El rojo, el ámbar y el verde se reservan para **estado**, no para acciones.

Los `btn-outline-*` de Bootstrap pintan el borde del color completo, así que una fila con cinco botones (Detalles, Editar, Historial, Código, Eliminar) quedaba como un semáforo pidiendo atención en cinco direcciones. Se unificaron a un botón neutro; solo Eliminar conserva texto rojo.

> Cuando todo puede resaltar, nada resalta.

### Cards planas, sin sombra

El fondo de la app es un gris casi blanco (`#f7f8fa`) y las tarjetas son blanco puro. Esa diferencia mínima ya comunica "esto está encima". Añadir sombra encima es redundante y ensucia.

La sombra (`--shadow`, una sola capa muy suave) se reserva para lo que **de verdad flota**: modales y dropdowns.

### Badges con fondo tinte, no sólido

Bootstrap los pinta de color sólido saturado. Eso funciona para uno; una tabla llena de badges sólidos se vuelve ilegible. Aquí van con fondo tinte suave + texto oscuro del mismo tono: se distinguen igual y no gritan.

### Tipografía del sistema

Stack `-apple-system, "Segoe UI", …`. En Windows cae en Segoe UI, que ya se ve limpio. Sin CDN de fuentes: cero dependencia externa, cero parpadeo al cargar, y funciona sin internet.

### Números tabulares

`font-variant-numeric: tabular-nums` en KPIs y cantidades. Sin esto, los dígitos tienen anchos distintos y las columnas de números bailan al actualizarse.

---

## Design tokens

Definidos en [`Content/Site.css`](InventarioApp/Content/Site.css), sección 1.

| Token | Valor | Para qué |
|---|---|---|
| `--bg` | `#f7f8fa` | Fondo de la app |
| `--surface` | `#ffffff` | Cards, inputs, navbar |
| `--surface-alt` | `#f9fafb` | Hover de fila, footer de modal |
| `--border` | `#e5e7eb` | Separadores, bordes de card |
| `--border-strong` | `#d1d5db` | Bordes de input (necesitan más contraste) |
| `--text` | `#111827` | Texto principal |
| `--text-soft` | `#4b5563` | Labels, links de nav |
| `--muted` | `#6b7280` | Texto secundario, headers de tabla |
| `--accent` | `#2563eb` | Acciones y estado activo |
| `--ok` / `--warn` / `--danger` | `#059669` / `#b45309` / `#dc2626` | Estado |
| `--radius` | `10px` | Cards, modales, alertas |
| `--radius-sm` | `8px` | Botones, inputs, badges |

Cada color semántico tiene su variante `-soft` para fondos de badge.

---

## Qué se construyó

### 1. Capa de tema

- [`Content/Site.css`](InventarioApp/Content/Site.css) — reescrito completo, organizado en 12 secciones numeradas.
- [`Views/Shared/_Layout.cshtml`](InventarioApp/Views/Shared/_Layout.cshtml) — navbar con marca real, sección activa resaltada, chip de usuario con dropdown.

**Un bug que se llevó por delante:** la hoja vieja tenía

```css
input, select, textarea { max-width: 280px; }
```

Eso le clavaba 280px de ancho máximo a **todos** los campos de la app, sin importar la columna que los contuviera. Por eso los formularios se veían encogidos y desalineados. El ancho lo debe decidir la grilla (`.col-*`), que es quien sabe cuánto espacio hay.

**Avisos unificados.** Antes había dos caminos: `TempData["Error"]` se pintaba en el layout, y el mensaje de éxito se armaba con HTML duplicado dentro del JS de `Items/Index`. Ahora los dos terminan en el mismo `#flashContainer`, y el JS solo llama a tres helpers globales:

```js
mostrarFlash(mensaje, tipo)          // pinta un aviso ahora
guardarFlashPendiente(mensaje, tipo) // lo guarda para después de un reload
mostrarFlashPendiente()              // lo saca al cargar la página
```

Los dos últimos usan `sessionStorage` porque una variable JS normal no sobrevive a `location.reload()`.

### 2. Tarjeta de Crear/Editar item

```
┌──────────────────────────────────────────────────────┐
│  Nuevo item                                       ✕  │
├────────────────────┬─────────────────────────────────┤
│                    │  Código ⓘ        Nombre         │
│   [ imagen ]       │  [_______]       [_________]    │
│   preview 1:1      │                                 │
│                    │  Categoría       Unidad         │
│   Arrastra o       │  [ select v]     [_______]      │
│   haz clic         │                                 │
│                    │  Cantidad ⓘ      Stock mín. ⓘ   │
│   JPG/PNG · 10MB   │  [___]           [___]          │
│                    │  Ubicación                      │
│                    │  [_____________________________]│
├────────────────────┴─────────────────────────────────┤
│   VISTA PREVIA DE LA ETIQUETA                        │
│   ▌▌▐▌▐▐▌▌▐▌ CODE128          [ ▪▫▪ QR ]             │
├──────────────────────────────────────────────────────┤
│                          Cancelar   Guardar item     │
└──────────────────────────────────────────────────────┘
```

[`Views/Items/_ItemFormCard.cshtml`](InventarioApp/Views/Items/_ItemFormCard.cshtml) es el cuerpo compartido. `Create.cshtml` y `Edit.cshtml` quedaron como cascarones de ~20 líneas: header, `BeginForm`, partial, footer. Lo único que cambia entre las dos es `ViewBag.EsCreacion`.

**Por qué un partial y no dos vistas:** los dos formularios eran casi idénticos y ya habían empezado a divergir (Edit tenía el label sin `form-label`, Create sí). Cada campo nuevo había que acordarse de agregarlo dos veces. Con el partial es imposible que se desincronicen.

**La zona de imagen es cuadrada** (`aspect-ratio: 1/1`) para que el layout no salte según la foto: una apaisada y una vertical ocupan el mismo hueco.

**El `<input type="file">` real está dentro, transparente y estirado sobre toda la caja.** Así el clic en cualquier parte abre el selector, sin interceptar nada por JS. Un input de archivo nativo no se puede estilizar de forma confiable entre navegadores; esta es la técnica estándar para rodearlo.

**Los códigos se generan en vivo.** En Crear, un listener con debounce de 250 ms regenera barras y QR conforme escribes. En Editar el código es `readonly` (es la llave del item: cambiarlo desconectaría las etiquetas ya impresas) y se pinta al abrir.

#### Las ayudas, en dos niveles

| Nivel | Cuándo | Ejemplo |
|---|---|---|
| `form-text` bajo el campo | Siempre visible | *Ej. `TEC-HDMI-002` — usar `ÁREA-TIPO-NÚM` mantiene juntos los items parecidos al ordenar.* |
| Ícono ⓘ con tooltip | Solo al pasar el cursor | *Identificador único. Es lo que lee el escáner y lo que se imprime, así que después ya no se puede cambiar.* |

La regla de reparto: **el ejemplo concreto va siempre visible**; el tooltip guarda la regla de negocio, que es más larga y no se necesita cada vez. Un ejemplo escondido tras un hover no ayuda a nadie que esté llenando el form por primera vez.

Las ayudas solo se muestran en Crear (`EsCreacion == true`). Quien edita ya conoce los campos, y repetírselos es ruido.

### 3. Dashboard

```
┌────────────────────────────────────────────────────────┐
│  🔍 Escanea un código o escríbelo y pulsa Enter…       │
├──────────┬──────────┬──────────┬───────────────────────┤
│ 3        │ 0        │ 2        │ 0                     │
│ activos  │ agotados │ bajo mín │ movimientos hoy       │
├──────────────────────────┬─────────────────────────────┤
│ ⚠ Reposición urgente     │ Más movidos (30 d)          │
├──────────────────────────┼─────────────────────────────┤
│ Clasificación ABC        │ Actividad reciente          │
└──────────────────────────┴─────────────────────────────┘
```

Cada widget responde una pregunta concreta:

| Widget | Pregunta |
|---|---|
| Barra de escaneo | *Tengo esto en la mano, ¿qué es?* |
| KPIs | *¿Cómo está el inventario ahora mismo?* |
| Reposición urgente | *¿Qué tengo que comprar, y qué tan urgente?* |
| Más movidos | *¿Qué es lo que más se mueve?* |
| Clasificación ABC | *¿Qué pocos items concentran casi toda la actividad?* |
| Actividad reciente | *¿Quién hizo qué?* |

**Sin librería de gráficas.** Las barras de proporción y la barra segmentada ABC son `div` con CSS; el resto son tablas. Cero dependencias nuevas, y consistente con el estilo minimalista.

**Estados vacíos con explicación.** Un widget sin datos dice *por qué* está vacío ("Sin movimientos registrados en los últimos 30 días"), nunca una tabla en blanco — que se lee como un error de la app.

**La barra de escaneo tiene el foco al cargar.** La pistola lectora funciona en modo *keyboard wedge*: el sistema la ve como un teclado y al escanear "escribe" el código seguido de un Enter. Con el foco ya puesto, llegas, escaneas y caes en el item sin tocar el mouse. Reutiliza el endpoint `BuscarPorCodigo` que ya existía.

#### Días de cobertura — la métrica que reemplaza a "stock bajo"

```
consumo_diario = salidas de los últimos 30 días / 30
días_cobertura = existencia actual / consumo_diario
```

Un item con 3 unidades que nadie pide **no es urgente**. Uno con 40 que salen 8 al día se acaba el jueves. La lista se ordena por eso, no por la cantidad absoluta.

`DiasCobertura` es `int?` a propósito: **`NULL` significa "no se puede proyectar porque no hay salidas recientes"**, que no es lo mismo que cero días de cobertura. Un `int` normal aplastaría esa diferencia y la pantalla diría "se agota hoy" de algo que nadie usa.

Esto es lo que hizo falta la columna `items.stock_minimo`: antes el umbral era un `< 10` hardcodeado en la vista, que trataba igual a un tornillo y a un servidor.

#### Clasificación ABC — el 80/20 del almacén

Se ordenan los items por volumen movido y se van acumulando. Los que juntos suman el primer 80% son **clase A** (pocos items, casi toda la actividad); hasta el 95%, **B**; la cola larga, **C**.

La parte interesante en PL/SQL son las **funciones analíticas**, que a diferencia de un `GROUP BY` **no colapsan filas**: cada item conserva su renglón y además recibe un valor calculado sobre todo el conjunto.

```sql
RATIO_TO_REPORT(vol) OVER ()                      -- % que representa esta fila
SUM(vol) OVER (ORDER BY vol DESC, id_item
               ROWS BETWEEN UNBOUNDED PRECEDING
                        AND CURRENT ROW)          -- acumulado hasta aquí
```

Con `GROUP BY` habría que hacer un self-join para lograr lo mismo.

---

## Permisos en la interfaz

`HomeController` resuelve los permisos y los pasa como booleanos en el ViewModel. **La vista no consulta permisos por su cuenta**: pregunta por esos flags. Así la regla de "quién ve qué" vive en un solo lugar.

| Flag | Permiso | Efecto |
|---|---|---|
| `PuedeVerBitacora` | `HISTORIAL_VER` | Sin él, el feed de auditoría no aparece |
| `PuedeRegistrarMovimiento` | `MOVIMIENTOS_REGISTRAR` | Controla el botón "Reponer" |

**Detalle que importa:** el de bitácora además decide si la consulta **se ejecuta**. Esconder el widget con un `@if` bastaría para que no se vea, pero el dato ya habría viajado desde Oracle hasta el servidor. Un EMPLEADO ve el estado del inventario; quién hizo cada cosa es otra conversación.

---

## Gotchas que costaron tiempo

### El `.cshtml` sin BOM sale con los acentos rotos

Síntoma: "Últimos 30 días" se muestra como **"Ãºltimos 30 dÃ­as"**. En el navbar, en los títulos, en todos lados.

**Qué pasa:** un archivo guardado en UTF-8 **sin BOM** lo lee ASP.NET con la codepage de Windows (1252 en un Windows en español) al compilarlo. Cada acento, que en UTF-8 ocupa dos bytes, se convierte en dos caracteres distintos. Luego eso se envía como UTF-8 y queda doblemente codificado.

**Lo confuso:** el `<meta charset="utf-8">` del HTML **no tiene nada que ver**. Ese define cómo se *envía* la respuesta; el problema está en cómo se *lee* el código fuente. Los dos tienen que ser UTF-8 o el texto se rompe, y es fácil perder horas mirando el que ya estaba bien.

**Por qué aparece de repente:** los archivos que crea Visual Studio llevan BOM. Los que crea otro editor, normalmente no. Así que el bug solo afecta a los archivos nuevos, lo que hace parecer que el problema es el código nuevo.

**Arreglado de raíz** en `Web.config`:

```xml
<globalization fileEncoding="utf-8" />
```

Con eso da igual si el archivo trae BOM o no.

### El `.csproj` de MVC5 lista cada archivo a mano

No hay comodines: si un `.cs` o un `.cshtml` no está listado con su `<Compile Include>` o `<Content Include>`, **simplemente no se compila**. Sin error, sin aviso. El archivo sigue en disco, se ve normal en el explorador, y no existe para la app.

Así estuvo un tiempo el módulo entero de Activos/Ubicaciones/Catálogos: 30 archivos escritos, ninguno registrado.

**Corolario peligroso:** si dos editores tienen el proyecto abierto a la vez (Visual Studio + otro), el que guarde último pisa al otro y se pierden entradas en silencio. Cuando VS pregunta *"El proyecto se ha modificado fuera del entorno"* → **Recargar todo**. Elegir "Omitir" reescribe la versión vieja encima.

### Clasificar el ABC con el acumulado equivocado

El primer intento clasificaba con el acumulado **incluyendo** la fila actual. Se ve el error en el caso más simple: si solo un item tuvo movimiento, su acumulado es 100% — o sea > 95% — y **el item más movido del almacén salía etiquetado como clase C**.

La pregunta correcta no es "cuánto llevamos contando este item", sino **"cuánto llevábamos antes de llegar a él"**: si antes de este item aún no se cubría el 80%, entonces este item forma parte del grupo que lo cubre, y es clase A.

```sql
ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING   -- acumulado previo
```

La primera fila da `NULL` (no hay filas antes) → `NVL(...,0)` → siempre clase A. Que es justo lo correcto: el item más movido nunca puede ser C.

### Anchos en `style` inline y la coma decimal

```csharp
pct.ToString("0.##", CultureInfo.InvariantCulture)
```

El `InvariantCulture` no es cosmético. El servidor corre en español, donde el separador decimal es la coma; sin él, `width:12,5%` es CSS inválido, el navegador descarta la regla entera y la barra se ve rota. Es el tipo de bug que solo aparece en la máquina con la configuración regional equivocada.

### Los tooltips de Bootstrap no se auto-activan

Hay que instanciarlos: `new bootstrap.Tooltip(el)`. Y como el formulario entra al DOM por AJAX, tiene que ser **después de inyectar el HTML**, no en un `$(document).ready()` que ya pasó hace rato.

### `bootstrap.js` no incluye Popper — los dropdowns truenan

Síntoma: todo funciona (modales, alertas, tablas), pero al hacer clic en un menú desplegable la consola tira

```
Uncaught TypeError: Popper__namespace.createPopper is not a function
```

Bootstrap 5 se distribuye en dos versiones de JavaScript:

| Archivo | Contiene |
|---|---|
| `bootstrap.js` | Solo Bootstrap |
| `bootstrap.bundle.js` | Bootstrap **+ Popper incrustado** |

Popper es la librería que calcula dónde colocar un elemento flotante para que no se salga de la pantalla. Solo la necesitan los componentes que se despliegan **encima** del resto de la página: **dropdown, tooltip y popover**. Modal, alert, collapse y tabs funcionan sin ella.

Por eso el error tarda en aparecer: Bootstrap está cargado, casi todo responde, y el instinto es buscar el bug en el HTML del dropdown. No está ahí — falta una dependencia que el `BundleConfig.cs` nunca incluyó.

En este proyecto afectaba a los menús **Ubicaciones** y **Catálogos**, al chip de usuario y a los tooltips ⓘ del formulario de item. Fix en [`App_Start/BundleConfig.cs`](InventarioApp/App_Start/BundleConfig.cs):

```csharp
bundles.Add(new Bundle("~/bundles/bootstrap").Include(
          "~/Scripts/bootstrap.bundle.js"));   // .bundle, no bootstrap.js
```

**Cómo verificarlo sin adivinar:** `bootstrap.bundle.js` pesa 203 KB y `bootstrap.js` 142 KB. Esos 61 KB de diferencia son Popper. Buscar `createPopper` dentro del archivo lo confirma en un segundo.

### El `<script>` va dentro del partial

`_ItemFormCard.cshtml` lleva su propio `<script>` al final. Es a propósito: jQuery **ejecuta los `<script>` que llegan en el HTML de un `.html()`**, así que el formulario se inicializa solo tanto cuando se carga en el modal como cuando se abre como página completa (`/Items/Create`).

Si ese JS viviera en `Index.cshtml`, habría que acordarse de llamarlo a mano tras cada inyección, y la versión de página completa se quedaría sin nada.

---

## Qué quedó fuera

Decisiones conscientes, no olvidos:

- **Bitácora de accesos/logins** — hoy no existe ninguna tabla que registre quién inició sesión y cuándo. Requiere DDL nuevo y escribir desde `AccountController`.
- **Stock muerto** — items con existencia pero sin movimiento en 60+ días. Capital parado; es el KPI que más pide gerencia.
- **Detección de movimientos atípicos** — salidas fuera del comportamiento normal del item (`AVG()` y `STDDEV() OVER (PARTITION BY id_item)`), para cazar errores de captura o merma.
- **La tarjeta de dos columnas en Activos** — el patrón ya está resuelto en `_ItemFormCard`; replicarlo en `Activos/Create` y `Activos/Edit` es buen ejercicio para hacerlo sin ayuda, como se hizo con Movimientos.

---

## Archivos

**Nuevos**

| Archivo | Qué es |
|---|---|
| `Views/Items/_ItemFormCard.cshtml` | Cuerpo compartido del formulario de item |
| `Models/DashboardViewModel.cs` | ViewModel + los 5 POCOs del dashboard |
| `Repositories/DashboardRepository.cs` | Único punto de contacto con `pkg_dashboard` |
| `Services/DashboardService.cs` | Arma el resumen completo en una llamada |
| `pkg_dashboard` (en `inventario_dev.sql`, sección 4.16) | Los 5 procedures de métricas |

**Reescritos**

`Content/Site.css` · `Views/Shared/_Layout.cshtml` · `Views/Home/Index.cshtml` · `Views/Items/{Index,Create,Edit,Detalles,Historial,Escanear}.cshtml` · `Views/Account/Login.cshtml` · `Controllers/HomeController.cs`

**Modificados**

`Models/Item.cs` (+`StockMinimo`) · `Repositories/ItemRepository.cs` · `Controllers/{Items,Account}Controller.cs` · `pkg_items` · `Web.config` (`fileEncoding`) · `InventarioApp.csproj`

**Eliminados**

`Views/Home/{About,Contact,Saludo}.cshtml` y sus actions.
