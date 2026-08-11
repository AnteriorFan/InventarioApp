# Roadmap futuro — Sistema de gestión de activos

## ✅ Progreso — qué ya está implementado

**FASE 1 — Ubicaciones** (Edificios → Áreas → Espacios): tablas + packages PL/SQL (`PKG_EDIFICIOS`, `PKG_AREAS`, `PKG_ESPACIOS`) + CRUD completo en C# (Model/Repository/Service/Controller/Vistas), con dropdowns encadenados (una Área elige su Edificio, un Espacio elige su Área).

**Sistema de Roles/Permisos** (reemplazó el `[Authorize(Roles=...)]` original de MVC5 en todo el proyecto): tablas `roles`, `permisos`, `rol_permisos` (permisos que trae cada rol por defecto) y `usuario_permisos` (excepciones por usuario individual, con flag `concedido` S/N — permite dar un permiso extra a alguien puntual, o quitarle uno que su rol sí traería). Procedure `pkg_permisos.sp_obtener_por_usuario` resuelve el permiso "efectivo" combinando ambas fuentes. `AuthorizePermisoAttribute` personalizado (hereda de `AuthorizeAttribute`, sobreescribe `AuthorizeCore`) reemplazó todos los checks de rol del proyecto. Se limpió el código viejo (`usuarios.rol`, reconstrucción de roles en `Global.asax`, ya no se usaban).

**FASE 2 — Catálogos**: `Marca`, `Modelo` (FK a Marca), `Estados`, `Tipos_Movimiento` — packages + CRUD completo cada uno.

**FASE 3 — Activos**: tabla con 7 FKs (categoría, marca, modelo, estado, ubicación origen, ubicación actual, responsable) + columnas de auditoría. `PKG_ACTIVOS` usa `LEFT JOIN` en las 7 relaciones (todas opcionales — con `JOIN` normal, cualquier activo sin alguna FK asignada desaparecería de la lista). CRUD completo en C# con 7 dropdowns en el formulario de Crear/Editar (agrupados en un método `CargarDropdowns()` para no repetir la misma carga en las dos actions). Se agregó `PKG_USUARIOS.sp_listar` (antes solo existía `sp_obtener_por_login`) para poder listar usuarios en el dropdown de "Responsable". `codigo` quedó de solo lectura al editar (mismo criterio que `Items`, por el QR/código de barras).

**Decisión de arquitectura clave**: `Activos` NO reemplaza ni migra `Items` — son dos sistemas paralelos que conviven, ver detalle abajo.

**Pendiente**: FASE 4 (historial de movimientos de Activos) y FASE 5 (galería de imágenes de Activos).

---

## Decisiones tomadas (con el porqué)

- **Items vs. Activos: NO es una migración, son dos sistemas paralelos.** `Items` modela existencias/consumibles (cantidad, unidad de medida — "hay 20 tornillos"). `Activos` modela unidades individuales rastreables (número de serie, responsable, garantía — "este proyector"). Explotar un `Items.cantidad=20` en 20 filas de `Activos` fabricaría datos (números de serie, ubicaciones individuales) que nunca existieron. Por eso: `Activos` nace vacío, sin migrar nada de `Items`/`movimientos_inventario`/`historial_items` — esas tablas se quedan intactas, sirviendo al sistema de Items. `ItemsController` no se toca.
- El "historial de movimientos enriquecido" (FASE 4) siempre fue pensado para `Activos` (`id_activo`, no `id_item`) — es tabla nueva, no una modificación de `movimientos_inventario`.
- **Índices en las FK de `activos`**: Oracle indexa automáticamente PK y `UNIQUE`, pero NUNCA las Foreign Keys — sin índice, cada dropdown filtrado (categoría, marca, modelo, estado, ubicación, responsable) hace table scan completo, y Oracle puede tomar locks más agresivos en el padre durante ciertos DML. Se agregaron índices manuales sobre las FK que sí se filtran activamente desde la UI.
- **Estrategia de migración**: empezar limpio, sin migrar los datos de prueba actuales (`items`, `movimientos_inventario`, `historial_items` se quedan como están, sin tocar, y las tablas nuevas se crean aparte).
- **Tabla `Espacio`** (nunca definida en el diseño original), ya resuelta:
```sql
Espacios
| Campo               | Tipo             |
| id_espacio          | INT (PK)         |
| id_area             | INT (FK)         |
| nombre              | VARCHAR(100)     |  -- ej. "Oficina 204", "Bodega 2"
| descripcion         | TEXT             |
| activo              | BOOLEAN          |
| creado_por          | INT (FK Usuario) |
| actualizado_por     | INT (FK Usuario) |
| fecha_creacion      | DATETIME         |
| fecha_actualizacion | DATETIME         |
```

**Plan por fases**:
1. ✅ Ubicaciones: Edificios → Áreas → Espacios
2. ✅ Catálogos nuevos: Marca, Modelo, Estados, Tipo de Movimiento
3. ✅ Activos (paralelo a Items, sin migración)
4. ⬜ Historial de movimientos enriquecido (de Activos)
5. ⬜ Galería de imágenes (Activos_Imagenes)

---

## Patrones y gotchas técnicos de esta implementación (bueno para repasar)

| Concepto | Detalle |
|---|---|
| `LEFT JOIN` vs `JOIN` en FKs opcionales | Si una FK puede ser `NULL` (como todas las de `activos`), usar `JOIN` normal hace que cualquier fila sin ese dato desaparezca por completo del resultado — bug silencioso. `LEFT JOIN` trae `NULL` en esa columna pero conserva la fila. |
| Self-join con dos alias | `activos` tiene DOS FKs a la misma tabla (`espacios`, para ubicación origen y actual) — hay que unir la misma tabla dos veces con alias distintos (`eo`/`ea`), no se puede repetir el mismo alias. |
| `int?` (nullable) en el Model de C# | Cuando una FK es opcional en la BD, la propiedad en C# tiene que ser `int?`, no `int` — si no, no hay forma de representar "sin asignar". Combinado con `(object)valor ?? DBNull.Value` al armar el `OracleParameter`. |
| `AuthorizeAttribute` personalizado | Se puede heredar de `AuthorizeAttribute` (la misma clase detrás de `[Authorize(Roles=...)]`) y sobreescribir `AuthorizeCore(HttpContextBase)` para reemplazar la lógica de "quién puede pasar" — de ahí salió `AuthorizePermisoAttribute`, que consulta permisos resueltos en vez de nombres de rol fijos. |
| Excepciones individuales sobre un rol | Tabla puente con flag `concedido CHAR(1) CHECK IN ('S','N')` (no solo "agregar permisos") permite tanto dar un permiso extra a una persona puntual como quitarle uno que su rol le daría por defecto — dos direcciones con una sola tabla. |
| `CargarDropdowns()` como método privado del Controller | Cuando un formulario necesita varios `SelectList` (Activos necesita 7), conviene un método privado que arma todos los `ViewBag` de una vez, reusado entre las actions `Create` GET y `Edit` GET — evita repetir 7 líneas casi idénticas dos veces. |
| Índices en FK no son automáticos en Oracle | A diferencia de la PK y `UNIQUE`, las FK se quedan sin índice a menos que lo crees a mano — impacta cualquier `WHERE`/`JOIN` sobre esa columna, y el comportamiento de locking en el padre. |

---

## Pendientes menores (no requieren el rediseño completo)

- Convertir `UnidadMedida` (hoy texto libre) a un `<select>` con valores fijos (unidad, caja, kg, litro, metro, etc.) — mismo patrón que se usó para el dropdown de Categoría.
- **Organizar el almacenamiento de imágenes por item/fecha.** Ahora mismo `LocalImageStorage.Guardar` mete todas las imágenes de todos los items en una sola carpeta plana (`Content/uploads/`), con nombre aleatorio (GUID), sin ninguna relación visible con el item al que pertenecen (solo existe en la BD, columna `imagen_s3_key`). Se decidió esperar a implementar esto hasta el rediseño completo, porque ahí tiene más sentido: la tabla `Items_Imagenes` del esquema nuevo ya contempla varias imágenes por activo (no solo una), con tipo (`Llegada`, `Actual`, `Serie`, `Daño`, `Mantenimiento`, `Otro`) e imagen principal — así que la organización de carpetas/nombres se diseña junto con esa tabla, no antes.

### Generador automático de `codigo` (identificador "inteligente")

Idea propuesta: en vez de que el usuario escriba el `codigo` a mano al crear un item, generarlo automáticamente concatenando:

```
[ABREVIATURA_CATEGORIA][ABREVIATURA_TIPO][FECHA][NUMERO_SECUENCIAL]
```
Ejemplo: `SA` (área/categoría, ej. Sistemas) + `CM` (tipo, ej. Computadora) + `050626` (fecha DDMMYY) + `0001` (consecutivo) → `SACM0506260001`.

**Decisiones pendientes antes de implementar**:
1. **Ancho del consecutivo**: ¿3 dígitos (hasta 999) o más? Si se llega al límite, ¿el código truena o hay que ampliar el campo? Definir esto ANTES de generar el primer código real, porque cambiar el ancho después rompe el formato de los códigos ya impresos/pegados en objetos físicos.
2. **Alcance del consecutivo**: ¿es global (nunca se reinicia, necesita más dígitos desde el inicio) o se reinicia por período — por día, por categoría+día, etc.? Si se reinicia, la combinación completa (con fecha) sigue siendo única aunque el número se repita.
3. **Requiere un catálogo de "Tipo"** que hoy no existe en el esquema simple (solo hay `Categoria`) — se relaciona directo con `Modelo`/`Marca` del esquema completo de gestión de activos ya documentado arriba en este archivo.
4. **Tradeoff de diseño a tener presente**: esto es una "llave natural/inteligente" (codifica info legible) vs. la `IDENTITY` autoincremental que ya usa `id_item` (una "llave surrogate", sin significado). Ventaja: la etiqueta física es legible a simple vista. Desventaja real: si la categorización cambia después (ej. un área se divide en dos), los códigos viejos quedan con el prefijo "obsoleto" para siempre — no hay forma limpia de recodificar sin invalidar etiquetas ya impresas y pegadas en objetos físicos. Buen tema para hablar en entrevista sobre diseño de identificadores.

Implementación sugerida (cuando se retome): un procedure en PL/SQL que reciba la abreviatura de categoría/tipo, formatee `SYSDATE`, y calcule el siguiente consecutivo (vía `SELECT COUNT(*)` o `MAX()` filtrado por el prefijo del día/categoría, o una `SEQUENCE` dedicada si el conteo es global) — y regrese el código completo armado, para que el Controller lo asigne antes de llamar `sp_insertar`.

---

Diseño de esquema más completo, propuesto como evolución del inventario simple actual hacia un **sistema de gestión de activos de TI** (tipo Snipe-IT/ServiceNow simplificado). No se implementa todavía — se guarda aquí para retomarlo cuando el CRUD básico, login, historial, DataTables/modales y demás pendientes del roadmap actual estén completos.

Ver progreso actual en [NOTAS-APRENDIZAJE.md](NOTAS-APRENDIZAJE.md).

## Por qué es un salto grande respecto al esquema actual

- Jerarquía completa de ubicación (`Edificio → Área → Espacio`) en vez de un campo `ubicacion` de texto libre en `items`.
- Catálogos normalizados de verdad: `Marca` y `Modelo` como tablas propias (con `Modelo` referenciando `Marca`), en vez de campos sueltos.
- Tabla `Estados` (Disponible, Prestado, En reparación, Baja, Extraviado) en vez de nuestro `activo` binario S/N.
- `Tipo de Movimiento` como catálogo (Alta, Reubicación, Préstamo, Devolución, Mantenimiento, Baja) — nuestro `historial_items.accion` actual es un CHECK con 3 valores fijos, esto es más flexible.
- Galería de imágenes por activo (`Items_Imagenes`, con tipo: Llegada/Actual/Serie/Daño/Mantenimiento/Otro, y bandera de imagen principal) — nosotros solo contemplamos una imagen (`imagen_s3_key`).
- Historial de movimiento mucho más rico: origen/destino, responsable anterior/nuevo, motivo, quién lo realizó y quién lo autorizó — nuestro `historial_items` es más simple (una sola tabla de auditoría de cambios, sin distinguir "movimiento físico" de "modificación de datos").
- Campos de auditoría completos (`creado_por`, `actualizado_por`, `fecha_creacion`, `fecha_actualizacion`) en cada tabla, no solo en algunas.

**Nota**: el esquema original menciona una tabla `Espacio` (tercer nivel bajo `Área`) en las relaciones de FK de `Activo`/`Movimientos`, pero nunca se definió su estructura explícita — falta definirla cuando se retome esto.

---

## Esquema completo (tal como se propuso)

### Usuarios
| Campo | Tipo | Descripción |
|---|---|---|
| id_usuario | INT (PK) | Identificador único |
| nombre | VARCHAR(150) | Nombre completo |
| correo | VARCHAR(150) | Correo electrónico |
| contraseña | VARCHAR(255) | Contraseña cifrada |
| rol | VARCHAR(50) | Administrador, Supervisor, Almacén, etc. |
| activo | BOOLEAN | Estado del usuario |
| fecha_creacion | DATETIME | Fecha de registro |
| fecha_actualizacion | DATETIME | Última actualización |

### Ubicaciones

#### Edificios
| Campo | Tipo |
|---|---|
| id_edificio | INT (PK) |
| nombre | VARCHAR(100) |
| descripcion | TEXT |
| activo | BOOLEAN |
| creado_por | INT (FK Usuario) |
| actualizado_por | INT (FK Usuario) |
| fecha_creacion | DATETIME |
| fecha_actualizacion | DATETIME |

#### Áreas
*Ejemplos: RRHH, SISTEMAS, DESARROLLO, CONTROL, etc.*

| Campo | Tipo |
|---|---|
| id_area | INT (PK) |
| id_edificio | INT (FK) |
| nombre | VARCHAR(100) |
| descripcion | TEXT |
| activo | BOOLEAN |
| creado_por | INT (FK Usuario) |
| actualizado_por | INT (FK Usuario) |
| fecha_creacion | DATETIME |
| fecha_actualizacion | DATETIME |

*(Falta definir la tabla `Espacio`, tercer nivel bajo Área — referenciada por FK en Activos/Movimientos pero nunca detallada)*

### Equipo/Ítems

#### Catálogos (Categorías)
| Campo | Tipo |
|---|---|
| id_categoria | INT (PK) |
| nombre | VARCHAR(100) |
| descripcion | TEXT |
| activo | BOOLEAN |
| creado_por | INT |
| actualizado_por | INT |
| fecha_creacion | DATETIME |
| fecha_actualizacion | DATETIME |

#### Marca
| Campo | Tipo |
|---|---|
| id_marca | INT (PK) |
| nombre | VARCHAR(100) |
| descripcion | TEXT |
| activo | BOOLEAN |
| creado_por | INT |
| actualizado_por | INT |
| fecha_creacion | DATETIME |
| fecha_actualizacion | DATETIME |

#### Modelos
| Campo | Tipo |
|---|---|
| id_modelo | INT (PK) |
| id_marca | INT (FK) |
| nombre | VARCHAR(100) |
| descripcion | TEXT |
| activo | BOOLEAN |
| creado_por | INT |
| actualizado_por | INT |
| fecha_creacion | DATETIME |
| fecha_actualizacion | DATETIME |

#### Estados
*Ejemplos: Disponible, Prestado, En reparación, Baja, Extraviado*

| Campo | Tipo |
|---|---|
| id_estado | INT (PK) |
| nombre | VARCHAR(50) |
| descripcion | TEXT |
| activo | BOOLEAN |

#### Tipo de Movimiento
*Ejemplo: Alta, Reubicación, Préstamo, Devolución, Mantenimiento, Baja*

| Campo | Tipo |
|---|---|
| id_tipo_movimiento | INT (PK) |
| nombre | VARCHAR(50) |
| descripcion | TEXT |
| activo | BOOLEAN |

#### Proveedores (Opcional)
| Campo | Tipo |
|---|---|
| id_proveedor | INT (PK) |
| nombre | VARCHAR(150) |
| telefono | VARCHAR(20) |
| correo | VARCHAR(150) |
| direccion | TEXT |
| activo | BOOLEAN |

### Activos (tabla completa)
| Campo | Tipo |
|---|---|
| id_activo | INT (PK) |
| codigo | VARCHAR(100) UNIQUE |
| nombre | VARCHAR(150) |
| descripcion | TEXT |
| id_categoria | INT (FK) |
| id_marca | INT (FK) |
| id_modelo | INT (FK) |
| numero_serie | VARCHAR(150) |
| id_estado | INT (FK) |
| id_ubicacion_origen | INT (FK Espacio) |
| id_ubicacion_actual | INT (FK Espacio) |
| responsable | INT (FK Usuario o Empleado) |
| fecha_compra | DATE |
| costo | DECIMAL(12,2) |
| garantia_hasta | DATE |
| observaciones | TEXT |
| activo | BOOLEAN |
| creado_por | INT (FK Usuario) |
| actualizado_por | INT (FK Usuario) |
| fecha_creacion | DATETIME |
| fecha_actualizacion | DATETIME |

### Ítems_Imágenes
| Campo | Tipo |
|---|---|
| id_imagen | BIGINT (PK) |
| id_activo | BIGINT (FK) |
| nombre_archivo | VARCHAR(255) |
| ruta | VARCHAR(500) |
| tipo | ENUM('Llegada','Actual','Serie','Daño','Mantenimiento','Otro') |
| descripcion | TEXT |
| principal | BOOLEAN |
| creado_por | BIGINT |
| fecha_creacion | DATETIME |

### Historial de movimiento
| Campo | Tipo |
|---|---|
| id_movimiento | INT (PK) |
| id_activo | INT (FK) |
| id_tipo_movimiento | INT (FK) |
| ubicacion_origen | INT (FK Espacio) |
| ubicacion_destino | INT (FK Espacio) |
| responsable_anterior | INT |
| responsable_nuevo | INT |
| motivo | VARCHAR(200) |
| observaciones | TEXT |
| realizado_por | INT (FK Usuario) |
| autorizado_por | INT (FK Usuario) |
| fecha_movimiento | DATETIME |

## Flujo/jerarquía

```
Edificio
   │
   ▼
Área
   │
   ▼
Espacio
   │
   ▼
Activo
   │
   ▼
Movimientos
```

`Usuario` es transversal: crea/audita tanto `Edificio` como `Catálogos` (y en general, todos los `creado_por`/`actualizado_por` de las demás tablas).
