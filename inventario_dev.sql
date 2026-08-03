CREATE TABLE categorias (
    id_categoria    NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre          VARCHAR2(100)   NOT NULL,
    descripcion     VARCHAR2(500)
);

CREATE TABLE items (
    id_item         NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo          VARCHAR2(50)    NOT NULL,
    nombre          VARCHAR2(200)   NOT NULL,
    descripcion     VARCHAR2(1000),
    id_categoria    NUMBER,
    cantidad        NUMBER          DEFAULT 0 NOT NULL,
    unidad_medida   VARCHAR2(20),
    ubicacion       VARCHAR2(100),
    imagen_s3_key   VARCHAR2(500),
    activo          CHAR(1)         DEFAULT 'S' NOT NULL,
    fecha_creacion  DATE            DEFAULT SYSDATE,
    fecha_modif     DATE,
    CONSTRAINT uq_items_codigo UNIQUE (codigo),
    CONSTRAINT fk_items_categoria FOREIGN KEY (id_categoria) REFERENCES categorias (id_categoria),
    CONSTRAINT ck_items_cantidad CHECK (cantidad >= 0),
    CONSTRAINT ck_items_activo CHECK (activo IN ('S','N'))
);

CREATE TABLE movimientos_inventario (
    id_movimiento    NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_item          NUMBER          NOT NULL,
    tipo_movimiento  VARCHAR2(10)    NOT NULL,
    cantidad         NUMBER          NOT NULL,
    fecha            DATE            DEFAULT SYSDATE,
    observaciones    VARCHAR2(500),
    CONSTRAINT fk_mov_item FOREIGN KEY (id_item) REFERENCES items (id_item),
    CONSTRAINT ck_mov_tipo CHECK (tipo_movimiento IN ('ENTRADA','SALIDA')),
    CONSTRAINT ck_mov_cantidad CHECK (cantidad > 0)
);

CREATE TABLE usuarios (
    id_usuario      NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    nombre          VARCHAR2(100)   NOT NULL,
    apellido        VARCHAR2(100)   NOT NULL,
    usuario_login   VARCHAR2(50)    NOT NULL,
    password_hash   VARCHAR2(256)   NOT NULL,
    rol             VARCHAR2(20)    NOT NULL,
    activo          CHAR(1)         DEFAULT 'S' NOT NULL,
    fecha_creacion  DATE            DEFAULT SYSDATE,
    CONSTRAINT uq_usuarios_login UNIQUE (usuario_login),
    CONSTRAINT ck_usuarios_rol CHECK (rol IN ('EMPLEADO','ADMIN','JEFE')),
    CONSTRAINT ck_usuarios_activo CHECK (activo IN ('S','N'))
);

CREATE TABLE historial_items (
    id_historial    NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_item         NUMBER          NOT NULL,
    id_usuario      NUMBER          NOT NULL,
    accion          VARCHAR2(20)    NOT NULL,
    fecha           DATE            DEFAULT SYSDATE,
    detalle         VARCHAR2(1000),
    CONSTRAINT fk_hist_item FOREIGN KEY (id_item) REFERENCES items (id_item),
    CONSTRAINT fk_hist_usuario FOREIGN KEY (id_usuario) REFERENCES usuarios (id_usuario),
    CONSTRAINT ck_hist_accion CHECK (accion IN ('ALTA','MODIFICACION','BAJA'))
);


CREATE OR REPLACE PACKAGE BODY pkg_items AS

    PROCEDURE sp_listar (
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT i.id_item        AS Id,
                   i.codigo         AS Codigo,
                   i.nombre         AS Nombre,
                   i.descripcion    AS Descripcion,
                   i.id_categoria   AS IdCategoria,
                   c.nombre         AS NombreCategoria,
                   i.cantidad       AS Cantidad,
                   i.unidad_medida  AS UnidadMedida,
                   i.ubicacion      AS Ubicacion,
                   i.imagen_s3_key  AS ImagenS3Key
            FROM items i
            LEFT JOIN categorias c ON c.id_categoria = i.id_categoria
            WHERE i.activo = 'S'
            ORDER BY i.nombre;
    END sp_listar;

    PROCEDURE sp_buscar_por_codigo (
        p_codigo IN items.codigo%TYPE,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT i.id_item        AS Id,
                   i.codigo         AS Codigo,
                   i.nombre         AS Nombre,
                   i.descripcion    AS Descripcion,
                   i.id_categoria   AS IdCategoria,
                   c.nombre         AS NombreCategoria,
                   i.cantidad       AS Cantidad,
                   i.unidad_medida  AS UnidadMedida,
                   i.ubicacion      AS Ubicacion,
                   i.imagen_s3_key  AS ImagenS3Key
            FROM items i
            LEFT JOIN categorias c ON c.id_categoria = i.id_categoria
            WHERE i.codigo = p_codigo
              AND i.activo = 'S';
    END sp_buscar_por_codigo;

    PROCEDURE sp_obtener_por_id (
        p_item_id IN items.id_item%TYPE,
        p_cursor  OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT i.id_item        AS Id,
                   i.codigo         AS Codigo,
                   i.nombre         AS Nombre,
                   i.descripcion    AS Descripcion,
                   i.id_categoria   AS IdCategoria,
                   c.nombre         AS NombreCategoria,
                   i.cantidad       AS Cantidad,
                   i.unidad_medida  AS UnidadMedida,
                   i.ubicacion      AS Ubicacion,
                   i.imagen_s3_key  AS ImagenS3Key
            FROM items i
            LEFT JOIN categorias c ON c.id_categoria = i.id_categoria
            WHERE i.id_item = p_item_id
              AND i.activo = 'S';
    END sp_obtener_por_id;

    PROCEDURE sp_insertar (
        p_codigo        IN  items.codigo%TYPE,
        p_nombre        IN  items.nombre%TYPE,
        p_descripcion   IN  items.descripcion%TYPE,
        p_id_categoria  IN  items.id_categoria%TYPE,
        p_cantidad      IN  items.cantidad%TYPE,
        p_unidad_medida IN  items.unidad_medida%TYPE,
        p_ubicacion     IN  items.ubicacion%TYPE,
        p_id_item_out   OUT items.id_item%TYPE
    ) IS
    BEGIN
        INSERT INTO items (codigo, nombre, descripcion, id_categoria, cantidad, unidad_medida, ubicacion)
        VALUES (p_codigo, p_nombre, p_descripcion, p_id_categoria, p_cantidad, p_unidad_medida, p_ubicacion)
        RETURNING id_item INTO p_id_item_out;
    END sp_insertar;

    PROCEDURE sp_actualizar (
        p_id_item       IN items.id_item%TYPE,
        p_nombre        IN items.nombre%TYPE,
        p_descripcion   IN items.descripcion%TYPE,
        p_id_categoria  IN items.id_categoria%TYPE,
        p_cantidad      IN items.cantidad%TYPE,
        p_unidad_medida IN items.unidad_medida%TYPE,
        p_ubicacion     IN items.ubicacion%TYPE
    ) IS
    BEGIN
        UPDATE items
           SET nombre        = p_nombre,
               descripcion   = p_descripcion,
               id_categoria  = p_id_categoria,
               cantidad      = p_cantidad,
               unidad_medida = p_unidad_medida,
               ubicacion     = p_ubicacion,
               fecha_modif   = SYSDATE
         WHERE id_item = p_id_item;
    END sp_actualizar;

    PROCEDURE sp_eliminar (
        p_id_item IN items.id_item%TYPE
    ) IS
    BEGIN
        UPDATE items
           SET activo = 'N',
               fecha_modif = SYSDATE
         WHERE id_item = p_id_item;
    END sp_eliminar;

END pkg_items;
/


INSERT INTO categorias (nombre, descripcion) VALUES ('Periféricos', 'Mouse, teclados, etc.');
INSERT INTO items (codigo, nombre, descripcion, id_categoria, cantidad, unidad_medida, ubicacion) VALUES ('A001', 'Mouse', 'Mouse inalámbrico', 1, 15, 'unidad', 'Bodega A');
INSERT INTO items (codigo, nombre, descripcion, id_categoria, cantidad, unidad_medida, ubicacion) VALUES ('A002', 'Teclado', 'Teclado mecánico', 1, 8, 'unidad', 'Bodega A');
COMMIT;
SELECT * FROM items;


SELECT line, position, text
FROM user_errors
WHERE name = 'PKG_ITEMS'
  AND type = 'PACKAGE BODY'
ORDER BY sequence;