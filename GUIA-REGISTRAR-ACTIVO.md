# Guía — Cómo registrar un activo

Ejemplo que se usa en toda la guía: una **laptop Lenovo LOQ 15ARP9**.

---

## 0. Antes que nada: ¿es un Activo o es un Item?

La pregunta que decide:

> **¿Necesito saber dónde está y quién tiene cada unidad, una por una?**

| Respuesta | Va en | Ejemplo |
|---|---|---|
| Sí | **Activos** | Una laptop asignada a alguien, un proyector, un torno |
| No, solo cuántas hay | **Items** | Tornillos, cables HDMI, hojas, 40 sillas iguales en bodega |

No es una preferencia: son dos sistemas paralelos con tablas distintas. Un Item lleva *cantidad*; un Activo lleva *número de serie, responsable y garantía*. Meter 40 sillas como 40 activos te obliga a inventar 40 números de serie que no existen.

---

## 1. Los catálogos van primero

`Modelo` depende de `Marca`, y `Activo` depende de las tres. El orden no es opcional:

```
Categoría  →  Marca  →  Modelo  →  Activo
```

### Los tres se crean desde la app

Dos caminos para cualquiera de los tres:

- **Catálogos → Categorías / Marcas / Modelos**, con el botón «+ Nueva».
- **Desde el propio formulario del activo**, con el «+ nueva» que está junto a cada dropdown. Se abre un panel chico, lo creas, y la opción queda seleccionada sin perder nada de lo que ya llevabas escrito.

**La abreviatura no es opcional** en Categoría ni en Marca: son las dos primeras partes del código automático. El formulario ya no te deja guardarlas sin ella, justamente porque el error aparecería mucho después — al dar de alta el activo, con un `ORA-20051` que no dice de dónde viene.

Para el ejemplo:

| Catálogo | Nombre | Abreviatura |
|---|---|---|
| Marca | `Lenovo` | `LEN` |
| Modelo | `LOQ 15ARP9` *(marca: Lenovo)* | — |

> **Sobre las mayúsculas**: la abreviatura va en mayúsculas (la app la convierte sola). El nombre va **como lo escribe el fabricante**: `Lenovo`, `ThinkPad`, `iMac`. Forzar mayúsculas destruiría `ThinkPad` → `THINKPAD`.

> **`LOQ 15` y `LOQ 15ARP9` son modelos distintos.** El sufijo identifica la variante. Si mañana entra una `LOQ 15IAX9`, va como modelo aparte.

---

## 2. Dar de alta el activo

**Activos → Nuevo activo.** Necesitas el permiso `ACTIVOS_ADMINISTRAR`.

| Campo | Qué poner | Ejemplo |
|---|---|---|
| **Código** | **Déjalo vacío** | se genera: `COMLEN120826001` |
| **Nombre** ⚠️ | Cómo le dice la gente | `Laptop` |
| Categoría | | Cómputo |
| Marca | | Lenovo |
| Modelo | | LOQ 15ARP9 |
| Nº de serie | El de la etiqueta del fabricante | `PF4XK2R9` |
| Estado | | Disponible |
| Ubicación de origen | De dónde vino, no cambia nunca | Bodega de compras |
| Ubicación actual | Dónde está hoy | Oficina 204 |
| Responsable | Quién la tiene asignada | Juan Pérez |
| Fecha de compra, Costo, Garantía | De la factura | |

⚠️ **Nombre es el único obligatorio** además de los que tienen `*`.

### El nombre: no repitas la marca ni el modelo

Este es el error más fácil de cometer. **No** pongas `Laptop Lenovo LOQ 15ARP9`:

- Se desincroniza: si corriges la marca en el dropdown, el nombre sigue diciendo la vieja.
- Se ve duplicado: el listado ya tiene columna *Marca / Modelo*.

Pon `Laptop`. En pantalla lees:

> **Laptop** · `COMLEN120826001` · Lenovo LOQ 15ARP9 · Oficina 204 · Juan Pérez

Si quieres distinguir para qué es, ahí sí gana: `Laptop de diseño`, `Laptop de recepción`. Eso no está en ningún otro campo.

### El código: déjalo vacío

Mientras eliges categoría y marca, abajo del campo verás cómo va quedando: `COMLEN120826###`.

Los `###` son a propósito. **El número lo asigna Oracle al guardar**, no el navegador — si lo calculara el navegador, dos personas dando de alta al mismo tiempo verían las dos el `001`.

Puedes escribir un código a mano si lo necesitas (para equipos que ya traen etiqueta de otro sistema). Si escribes algo, se respeta tal cual.

---

## 3. Después de guardar: la etiqueta

El activo nace con el aviso **«Falta imprimir la etiqueta»**. Es correcto: tiene código, pero todavía no hay sticker pegado — y sin sticker, escanearlo no lo encuentra.

1. **Activos → Etiquetas por imprimir** — están agrupadas por marca y modelo, que es como se imprimen en la práctica: te sientas una vez y sacas todas las de las laptops Lenovo.
2. Imprime y pega.
3. En el detalle del activo, **«Ya la imprimí»** quita el aviso.

Cada etiqueta lleva **su propio código**. Nunca imprimas la misma en varios equipos: escanear cualquiera te llevaría siempre al mismo registro.

---

## 4. El día a día: movimientos

Una vez dado de alta, **el activo no se edita para reflejar lo que le pasa**. Se le registra un movimiento.

**Detalles del activo → Registrar movimiento.** Permiso: `ACTIVOS_MOVER`.

Eso actualiza el activo *y* deja el renglón en el historial, en la misma operación. Es imposible que cambie sin que se sepa por qué.

Los campos que dejes en **«Sin cambio»** se quedan como están.

| Tipo | Para qué | Exige |
|---|---|---|
| Alta | Registro inicial | |
| Reubicación | Cambió de lugar | |
| Préstamo | Se asignó a alguien temporalmente | |
| Devolución | Regresó del préstamo | |
| Mantenimiento | Se mandó a reparar | motivo |
| **Reporte de daño** | Se detectó un daño | motivo + **foto** |
| **Baja** | Sale del inventario | motivo |
| Reetiquetado | Se le cambió el código | motivo |

Esas exigencias no están escritas en el código: son **casillas del catálogo**. En *Catálogos → Tipos de movimiento* puedes marcar «exigir motivo» o «exigir foto» en cualquier tipo, y el formulario y la base empiezan a pedirlo sin recompilar nada.

---

## 5. Casos que se repiten

### Diez laptops iguales

Diez activos. Comparten **una sola** fila de categoría, marca y modelo. Cada uno lleva su número de serie y su código: `...001`, `...002`, `...003`.

Hoy es llenar el formulario diez veces. Un alta por lote (elegir todo una vez y decir «cantidad: 10») es de las cosas que faltan.

### Le puse la categoría equivocada y ya la guardé

1. Edita el activo y corrige categoría o marca.
2. **Detalles → Regenerar código**, con un motivo.

Genera un código nuevo, lo deja en el historial como *Reetiquetado*, y vuelve a marcar la etiqueta como pendiente.

> ⚠️ **El código anterior no se guarda.** Cualquier etiqueta ya pegada con el código viejo deja de encontrar el activo al escanearla. Reimprime y reemplaza el sticker el mismo día.

### Encontré uno dañado

Movimiento tipo **Reporte de daño**: exige descripción y **foto**. La foto queda en el historial como evidencia, con fecha y quién la reportó.

### Escanear uno para ver qué es

**Escanear código** (menú del usuario, o el botón en Items). Lee items y activos indistintamente — no tienes que decirle cuál es. Sirve con pistola lectora o con la cámara del celular.

---

## 6. Referencia de permisos

| Para | Permiso |
|---|---|
| Ver activos y escanearlos | `ACTIVOS_VER` |
| Crear, editar, regenerar código | `ACTIVOS_ADMINISTRAR` |
| Registrar movimientos | `ACTIVOS_MOVER` |
| Crear marcas y modelos | `CATALOGOS_ADMINISTRAR` |

Un `EMPLEADO` solo tiene `ITEMS_VER`: no ve el menú de Activos. Si necesita registrar movimientos sin ser jefe, se le puede conceder `ACTIVOS_MOVER` **a él en particular** desde *Administración → Usuarios → Permisos*, sin cambiarle el rol.

---

## 7. Errores que vas a ver y qué significan

| Mensaje | Qué pasó |
|---|---|
| «Ponle un nombre al activo» | El nombre está vacío |
| «Ya existe un activo con ese código» | Escribiste a mano uno que ya estaba |
| «La categoría o la marca no tienen abreviatura configurada» | Falta la abreviatura — ponla en Catálogos (marca) o por SQL (categoría) |
| «Para generar el código hay que elegir categoría y marca» | Dejaste el código vacío pero sin elegir esos dos |
| «Se agotaron los 999 códigos de hoy…» | 999 altas de la misma categoría+marca en un día |
| «El movimiento "Baja" exige un motivo» | Falta explicar por qué |
| «El movimiento "Reporte de daño" exige una foto» | Falta adjuntar la evidencia |
| Te manda a «No tienes permiso» | Falta el permiso de la tabla de arriba |
