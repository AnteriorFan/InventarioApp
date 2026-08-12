# Diseño — Revisión de área (toma de inventario físico)

Documento de diseño, todavía **sin implementar**. Recoge el flujo, las tablas y
las decisiones que hay que tomar antes de escribir código.

---

## 1. Qué resuelve

Hoy la app sabe hacer dos cosas con un código:

- **Movimientos** (`ENTRADA` / `SALIDA`) — un flujo de cantidad sobre un item.
- **Escanear** — una consulta suelta: leo un código y veo su ficha.

Lo que falta es una tercera operación distinta a las dos: ir a un lugar físico y
**verificar que lo que el sistema dice que está ahí, efectivamente está, y en qué
condición**.

**La idea central de todo este diseño:** eso no se resuelve con escaneos sueltos,
sino con una **lista cerrada**. El dato más valioso de una revisión no es
"encontré esto" — es **"estos tres faltan"**, y eso solo se puede saber cuando
terminas de recorrer y cierras. Un escáner que consulta de uno en uno nunca va a
poder decirte qué falta, por muchos códigos que leas.

De ahí sale todo lo demás: hace falta una **sesión** con principio y fin, no
eventos independientes.

---

## 2. Antes que nada: la FASE 4 va primero

La revisión **no reemplaza** el "historial de movimientos enriquecido" de la
FASE 4 — lo **alimenta**.

`movimientos_activos` es el libro mayor de "qué le ha pasado a este activo":
cambió de lugar, cambió de responsable, cambió de estado. La revisión es una de
las **fuentes** que generan esos renglones (la otra es el traslado manual del día
a día). Si se construye la revisión primero, al confirmarla no habría dónde
escribir lo que pasó, y el activo quedaría cambiado sin rastro de por qué.

Además, ahí es donde `tipos_movimiento` deja de ser un catálogo muerto: el sketch
original de la FASE 4 ya tiene `id_tipo_movimiento` como FK. Los tipos que hacen
falta son algo como `TRASLADO`, `ASIGNACION`, `CAMBIO_ESTADO`, `REVISION`.

**Orden recomendado: FASE 4 → revisión.**

---

## 3. El flujo

```
   1. ABRIR                2. RECORRER              3. CERRAR           4. APLICAR
   ───────────             ───────────              ─────────           ──────────
   Elegir espacio    →     Escanear lo que      →   Lo no escaneado  →  Se escriben
   Se carga la lista       hay realmente            queda FALTANTE      los cambios
   de lo que el            + marcar estado          Resumen             en activos +
   sistema espera            y observaciones                            movimientos
```

**1. Abrir.** El usuario elige un espacio. Se crea la revisión y se carga la
lista de activos que el sistema dice que viven ahí
(`WHERE id_ubicacion_actual = :espacio`).

**2. Recorrer.** La pantalla muestra esa lista y cada escaneo la va palomeando.
Tres casos posibles:

| Lo que pasa | Resultado |
|---|---|
| Estaba en la lista y lo escaneo | `ENCONTRADO` |
| Lo escaneo, pero el sistema lo tenía en otro espacio | `MOVIDO` (se guarda de dónde venía) |
| El código no corresponde a ningún activo | va a `revision_desconocidos` |

Sobre cualquiera se puede marcar **estado** (Bueno / Dañado / Requiere
mantenimiento) y una **observación**.

**3. Cerrar.** Todo lo que estaba en la lista y nunca se escaneó pasa
automáticamente a `FALTANTE`. Se muestra el resumen.

**4. Aplicar.** Se escriben los cambios reales.

### Un detalle que sale gratis

Un activo que el sistema cree en el espacio A pero que físicamente está en el B
se reconcilia solo: sale `FALTANTE` en la revisión de A y `MOVIDO` en la de B.
No hay que hacer nada especial — las dos revisiones cuentan la misma historia
desde su lado.

---

## 4. Tablas

### 4.1 `revisiones`

```sql
CREATE TABLE revisiones (
    id_revision       NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_espacio        NUMBER        NOT NULL,
    id_usuario        NUMBER        NOT NULL,   -- quien la realiza
    estado            VARCHAR2(10)  DEFAULT 'ABIERTA' NOT NULL,
    fecha_inicio      DATE          DEFAULT SYSDATE NOT NULL,
    fecha_cierre      DATE,
    id_usuario_aplica NUMBER,                   -- quien la autoriza
    fecha_aplicacion  DATE,
    observaciones     VARCHAR2(500),
    CONSTRAINT fk_rev_espacio FOREIGN KEY (id_espacio)        REFERENCES espacios (id_espacio),
    CONSTRAINT fk_rev_usuario FOREIGN KEY (id_usuario)        REFERENCES usuarios (id_usuario),
    CONSTRAINT fk_rev_aplica  FOREIGN KEY (id_usuario_aplica) REFERENCES usuarios (id_usuario),
    CONSTRAINT ck_rev_estado  CHECK (estado IN ('ABIERTA','CERRADA','APLICADA','CANCELADA'))
);

-- Oracle no indexa las FK solo (ver "Decisiones tomadas" del ROADMAP).
CREATE INDEX ix_revisiones_espacio ON revisiones (id_espacio);
CREATE INDEX ix_revisiones_usuario ON revisiones (id_usuario);
```

**Por qué el alcance es el espacio y no el área.** Un área puede tener varias
oficinas. Es tentador abrir una revisión por área y recorrerlas todas, pero
`activos.id_ubicacion_actual` apunta a un **espacio**: "está donde debe estar"
solo se puede contestar a ese nivel. Si el alcance fuera el área, un activo que
está en la oficina de al lado saldría como correcto, y justamente ese es el
hallazgo que interesa. Para revisar un área completa se abren varias revisiones.

### 4.2 `revision_detalle`

```sql
CREATE TABLE revision_detalle (
    id_revision          NUMBER        NOT NULL,
    id_activo            NUMBER        NOT NULL,
    resultado            VARCHAR2(12)  NOT NULL,
    id_estado_encontrado NUMBER,        -- FK a estados; NULL = no se tocó
    id_espacio_anterior  NUMBER,        -- solo para MOVIDO: de dónde venía
    observaciones        VARCHAR2(500),
    fecha_escaneo        DATE,
    CONSTRAINT pk_revision_detalle PRIMARY KEY (id_revision, id_activo),
    CONSTRAINT fk_rd_revision FOREIGN KEY (id_revision)          REFERENCES revisiones (id_revision),
    CONSTRAINT fk_rd_activo   FOREIGN KEY (id_activo)            REFERENCES activos (id_activo),
    CONSTRAINT fk_rd_estado   FOREIGN KEY (id_estado_encontrado) REFERENCES estados (id_estado),
    CONSTRAINT fk_rd_espacio  FOREIGN KEY (id_espacio_anterior)  REFERENCES espacios (id_espacio),
    CONSTRAINT ck_rd_resultado CHECK (resultado IN ('ENCONTRADO','FALTANTE','MOVIDO'))
);

CREATE INDEX ix_rd_activo ON revision_detalle (id_activo);
```

La PK compuesta `(id_revision, id_activo)` impide por diseño que un activo
aparezca dos veces en la misma revisión — que es exactamente lo que pasa cuando
alguien escanea dos veces el mismo estante.

### 4.3 `revision_desconocidos`

```sql
CREATE TABLE revision_desconocidos (
    id_revision   NUMBER        NOT NULL,
    codigo        VARCHAR2(50)  NOT NULL,
    observaciones VARCHAR2(500),
    fecha_escaneo DATE DEFAULT SYSDATE,
    CONSTRAINT pk_revision_desconocidos PRIMARY KEY (id_revision, codigo),
    CONSTRAINT fk_rdesc_revision FOREIGN KEY (id_revision) REFERENCES revisiones (id_revision)
);
```

Tabla aparte porque **no hay `id_activo` que referenciar**: es un código que no
existe en el sistema. Meterlo en `revision_detalle` obligaría a hacer nullable la
FK y a perder la PK compuesta. Su salida es una lista de pendientes: *"da de alta
estos tres"*.

---

## 5. Estados de una revisión

```
  ABIERTA ──cerrar──> CERRADA ──aplicar──> APLICADA
     │                   │
     └────cancelar───────┴──> CANCELADA
```

| Estado | Significa | Se puede |
|---|---|---|
| `ABIERTA` | Recorrido en curso | Escanear, marcar, cancelar |
| `CERRADA` | Recorrido terminado, faltantes ya calculados | Aplicar o cancelar |
| `APLICADA` | Cambios escritos en `activos` | Nada — es histórico |
| `CANCELADA` | Se abandonó | Nada |

`APLICADA` es terminal a propósito: una vez que los cambios se escribieron,
"deshacer" sería otro movimiento, no borrar el registro. Un inventario que se
puede editar hacia atrás no sirve como evidencia de nada.

---

## 6. Qué se escribe al aplicar

| Resultado | Qué se hace en `activos` | Movimiento que genera |
|---|---|---|
| `ENCONTRADO` sin cambio de estado | nada | ninguno |
| `ENCONTRADO` con estado nuevo | `id_estado` = el encontrado | `CAMBIO_ESTADO` |
| `MOVIDO` | `id_ubicacion_actual` = espacio de la revisión | `TRASLADO` (origen → destino) |
| `FALTANTE` | `id_estado` = «No localizado» | `CAMBIO_ESTADO` |
| desconocido | nada | ninguno (queda como pendiente de alta) |

**Sobre `FALTANTE`:** se cambia el estado pero **no** se borra la ubicación. Es
tentador dejarlo "sin ubicación" ya que no apareció, pero saber dónde *debería*
haber estado es justo lo que necesitas para ir a buscarlo. Requiere agregar un
estado «No localizado» al catálogo — y eso ya se puede hacer desde la pantalla de
Catálogos, sin tocar SQL.

---

## 7. Permisos

Dos permisos nuevos, no uno:

| Permiso | Quién | Para qué |
|---|---|---|
| `REVISION_REALIZAR` | Empleado | Abrir, escanear, cerrar |
| `REVISION_APLICAR` | Jefe | Confirmar y escribir los cambios |

**Por qué separarlos.** Que "este activo falta" lo declare y lo aplique la misma
persona sin que nadie revise es un hueco de control interno — es el principio de
*segregación de funciones*, y es de las razones por las que se hace un inventario
físico en primer lugar. El empleado reporta; alguien más confirma.

Y aquí es donde el sistema de excepciones por usuario que ya está construido
rinde de verdad: se le puede dar `REVISION_APLICAR` a un empleado de confianza en
particular, **sin** cambiarle el rol ni dárselo a todos los empleados.

---

## 8. Decisiones abiertas

Estas dos cambian la arquitectura y dependen de cómo se trabaje en la práctica.
No las puedo decidir yo.

### A. ¿Hay conexión mientras se recorre?

| Opción | Cómo funciona | A favor | En contra |
|---|---|---|---|
| **Cada escaneo se guarda** | POST por cada código leído | Nada se pierde; se puede retomar en otro dispositivo | Necesita señal en todo el recorrido; se traba en bodegas y sótanos |
| **Se acumula y se manda al cerrar** | Todo en `localStorage`, un POST al final | Funciona sin señal | Si se pierde el navegador o el celular, se pierde el recorrido |

Mi recomendación: **acumular en el navegador**, porque el caso típico es una
bodega o un sótano sin cobertura, y quedarse atorado a media fila es peor que
rehacer un recorrido. La fila de `revisiones` sí se crea al abrir, para que quede
constancia de quién empezó qué y cuándo.

### B. ¿El que revisa es el que aplica?

Si en la práctica va a ser la misma persona siempre, el estado `CERRADA` y el
permiso `REVISION_APLICAR` sobran, y el flujo se acorta a tres pasos.

Mi recomendación: **mantenerlos separados**. El costo es un estado más; a cambio
el diseño demuestra segregación de funciones, que es de las cosas que se
preguntan en una entrevista sobre sistemas de inventario.

---

## 9. Qué NO entra en esta versión

- **Conteo cíclico de items.** Es la operación hermana (el sistema dice 50
  tornillos, contaste 47 → ajuste de −3), pero tiene otra forma: cantidad
  esperada contra cantidad contada, no encontrado/faltante. Y genera un
  movimiento de tipo `AJUSTE` sobre `movimientos_inventario`, no sobre activos.
  Mezclarlo en la primera versión duplica el trabajo. Va después.
- **Fotos del daño.** Depende de la FASE 5 (galería de imágenes). Es el mejor
  caso de uso que tiene esa fase, así que conviene hacerlas seguidas.
- **Revisión de un área completa en una sola sesión.** Se resuelve abriendo
  varias revisiones, una por espacio.
- **Programar revisiones** (recordatorio cada N meses). Se puede sacar después
  desde la fecha de la última revisión por espacio.

---

## 10. Tamaño del trabajo

| Pieza | Qué incluye |
|---|---|
| SQL — FASE 4 | Tabla `movimientos_activos` + `pkg_movimientos_activos` + tipos de movimiento sembrados |
| SQL — revisión | 3 tablas + `pkg_revisiones` (abrir, guardar detalle, cerrar, aplicar, listar) |
| SQL — permisos | `REVISION_REALIZAR`, `REVISION_APLICAR` + asignarlos a `JEFE` |
| C# | 3 modelos, repository, service, `RevisionesController` |
| Vistas | Listado, pantalla de recorrido (la más pesada), resumen/cierre |

El procedure de aplicar es el que más cuidado necesita: recorre el detalle,
actualiza `activos` y escribe los movimientos, **todo en una sola transacción**.
Si se queda a medias, unos activos quedarían movidos y otros no, sin forma de
saber cuáles.

La pantalla de recorrido es la más pesada del proyecto hasta ahora: lista en
vivo, escaneo continuo, estado por fila y guardado local.
