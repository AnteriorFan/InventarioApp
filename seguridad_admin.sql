--==============================================================================
--  PANTALLAS DE ADMINISTRACION DE ROLES Y USUARIOS
--------------------------------------------------------------------------------
--  Este script agrega lo que le faltaba a la base para poder administrar el
--  sistema de permisos DESDE LA APP, en vez de a punta de INSERT manuales.
--
--  Correr COMPLETO y en orden, con el mismo usuario de siempre.
--  Es idempotente en los packages (CREATE OR REPLACE), pero NO en la seccion 1
--  (los INSERT truenan con ORA-00001 si se corren dos veces; ver la nota ahi).
--
--  OJO CON LOS PACKAGES: cada uno se recrea COMPLETO, con los procedures que ya
--  existian mas los nuevos. Es a proposito. Un CREATE OR REPLACE PACKAGE BODY
--  reemplaza el cuerpo ENTERO: si solo pegaras los procedures nuevos, los viejos
--  desaparecerian y la app reventaria con PLS-00302 en cuanto los llamara.
--==============================================================================


--==============================================================================
--  1. PERMISO NUEVO
--==============================================================================
--  Las pantallas nuevas se protegen con su propio permiso. Sin esto, el
--  [AuthorizePermiso("SEGURIDAD_ADMINISTRAR")] de los Controllers nunca daria
--  true para nadie y te quedarias fuera de tus propias pantallas.
--
--  Si ya corriste esta seccion antes, saltatela: el UNIQUE de permisos.nombre
--  la hace fallar la segunda vez.

INSERT INTO permisos (nombre, descripcion)
VALUES ('SEGURIDAD_ADMINISTRAR', 'Administrar roles, permisos y usuarios');

--  Y hay que darselo al rol JEFE explicitamente.
--
--  El seed original le dio a JEFE "todo el catalogo" con un INSERT ... SELECT
--  sin filtro, pero eso fue UNA FOTO del catalogo en ese momento, no una regla
--  viva. Un permiso creado despues no le cae solo: hay que asignarlo.
INSERT INTO rol_permisos (id_rol, id_permiso)
    SELECT r.id_rol, p.id_permiso
    FROM roles r, permisos p
    WHERE r.nombre = 'JEFE'
      AND p.nombre = 'SEGURIDAD_ADMINISTRAR';

COMMIT;


--==============================================================================
--  2. PKG_ROLES  (completo: 4 procedures viejos + 3 nuevos)
--==============================================================================
CREATE OR REPLACE PACKAGE pkg_roles AS

    PROCEDURE sp_listar (p_cursor OUT SYS_REFCURSOR);

    PROCEDURE sp_obtener_por_id (
        p_id_rol IN  roles.id_rol%TYPE,
        p_cursor OUT SYS_REFCURSOR
    );

    PROCEDURE sp_registrar (
        p_nombre      IN  roles.nombre%TYPE,
        p_descripcion IN  roles.descripcion%TYPE,
        p_id_rol_out  OUT roles.id_rol%TYPE
    );

    PROCEDURE sp_modificar (
        p_id_rol      IN roles.id_rol%TYPE,
        p_nombre      IN roles.nombre%TYPE,
        p_descripcion IN roles.descripcion%TYPE
    );

    PROCEDURE sp_eliminar (p_id_rol IN roles.id_rol%TYPE);

    -- Nuevos: la matriz de permisos del rol.
    PROCEDURE sp_obtener_permisos (
        p_id_rol IN  roles.id_rol%TYPE,
        p_cursor OUT SYS_REFCURSOR
    );

    PROCEDURE sp_guardar_permisos (
        p_id_rol       IN roles.id_rol%TYPE,
        p_ids_permisos IN VARCHAR2
    );

END pkg_roles;
/

CREATE OR REPLACE PACKAGE BODY pkg_roles AS

    --  Se le agregaron dos contadores respecto a la version anterior, para que
    --  el listado pueda decir "este rol tiene 9 permisos y 3 usuarios" sin
    --  hacer una consulta extra por fila desde C# (el clasico problema N+1).
    PROCEDURE sp_listar (p_cursor OUT SYS_REFCURSOR) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT r.id_rol      AS Id,
                   r.nombre      AS Nombre,
                   r.descripcion AS Descripcion,
                   (SELECT COUNT(*)
                      FROM rol_permisos rp
                     WHERE rp.id_rol = r.id_rol)        AS NumPermisos,
                   (SELECT COUNT(*)
                      FROM usuarios u
                     WHERE u.id_rol = r.id_rol
                       AND u.activo = 'S')              AS NumUsuarios
            FROM roles r
            WHERE r.activo = 'S'
            ORDER BY r.nombre;
    END sp_listar;

    PROCEDURE sp_obtener_por_id (
        p_id_rol IN  roles.id_rol%TYPE,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        --  Devuelve las mismas columnas que sp_listar, con los conteos de
        --  verdad. Podrian ir en cero (la pantalla de editar no los usa), pero
        --  entonces el mismo Model tendria datos reales o falsos segun por donde
        --  se cargo, y eso es justo el tipo de sorpresa que cuesta depurar.
        OPEN p_cursor FOR
            SELECT r.id_rol      AS Id,
                   r.nombre      AS Nombre,
                   r.descripcion AS Descripcion,
                   (SELECT COUNT(*)
                      FROM rol_permisos rp
                     WHERE rp.id_rol = r.id_rol)        AS NumPermisos,
                   (SELECT COUNT(*)
                      FROM usuarios u
                     WHERE u.id_rol = r.id_rol
                       AND u.activo = 'S')              AS NumUsuarios
            FROM roles r
            WHERE r.id_rol = p_id_rol;
    END sp_obtener_por_id;

    PROCEDURE sp_registrar (
        p_nombre      IN  roles.nombre%TYPE,
        p_descripcion IN  roles.descripcion%TYPE,
        p_id_rol_out  OUT roles.id_rol%TYPE
    ) IS
    BEGIN
        INSERT INTO roles (nombre, descripcion)
        VALUES (p_nombre, p_descripcion)
        RETURNING id_rol INTO p_id_rol_out;
    END sp_registrar;

    PROCEDURE sp_modificar (
        p_id_rol      IN roles.id_rol%TYPE,
        p_nombre      IN roles.nombre%TYPE,
        p_descripcion IN roles.descripcion%TYPE
    ) IS
    BEGIN
        UPDATE roles
           SET nombre      = p_nombre,
               descripcion = p_descripcion
         WHERE id_rol = p_id_rol;
    END sp_modificar;

    -- Borrado logico: el rol no se elimina, se marca como inactivo.
    PROCEDURE sp_eliminar (p_id_rol IN roles.id_rol%TYPE) IS
        v_usuarios NUMBER;
    BEGIN
        --  Un rol con gente adentro no se puede desactivar: esos usuarios se
        --  quedarian apuntando a un rol inactivo y perderian TODOS sus permisos
        --  de golpe, sin ninguna señal de por que.
        SELECT COUNT(*)
          INTO v_usuarios
          FROM usuarios
         WHERE id_rol = p_id_rol
           AND activo = 'S';

        IF v_usuarios > 0 THEN
            RAISE_APPLICATION_ERROR(-20010,
                'No se puede eliminar el rol: todavia tiene ' || v_usuarios ||
                ' usuario(s) asignado(s). Muevelos a otro rol primero.');
        END IF;

        UPDATE roles
           SET activo = 'N'
         WHERE id_rol = p_id_rol;
    END sp_eliminar;

    --  Devuelve el catalogo COMPLETO de permisos, marcando cuales tiene el rol.
    --
    --  Es un LEFT JOIN a proposito: la pantalla necesita pintar tambien los
    --  permisos que el rol NO tiene (para poder palomearlos). Con un JOIN
    --  normal solo saldrian los que ya estan asignados y no habria forma de
    --  agregar ninguno.
    --
    --  AsignadoFlag sale como 'S'/'N' en vez de 1/0 porque Oracle no tiene tipo
    --  BOOLEAN en SQL, y un NUMBER hacia bool de C# se convierte mal. La misma
    --  convencion S/N que ya usan las columnas 'activo' del resto del esquema.
    PROCEDURE sp_obtener_permisos (
        p_id_rol IN  roles.id_rol%TYPE,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT p.id_permiso  AS Id,
                   p.nombre      AS Nombre,
                   p.descripcion AS Descripcion,
                   CASE WHEN rp.id_permiso IS NULL THEN 'N' ELSE 'S' END AS AsignadoFlag
            FROM permisos p
            LEFT JOIN rol_permisos rp
                   ON rp.id_permiso = p.id_permiso
                  AND rp.id_rol     = p_id_rol
            ORDER BY p.nombre;
    END sp_obtener_permisos;

    --  Guarda la matriz completa de un jalon: borra lo que habia y reinserta lo
    --  que venga palomeado.
    --
    --  Recibe los ids como texto ("3,7,11") en vez de hacer una llamada por
    --  permiso desde C#. Dos razones:
    --    1. Es UNA sola llamada a la base en vez de N.
    --    2. Es ATOMICA: si algo truena a la mitad, el DELETE tambien se
    --       deshace. Con N llamadas sueltas te puedes quedar con el rol a
    --       medio guardar, sin permisos.
    --
    --  El CONNECT BY sobre dual es el modismo clasico de Oracle para partir un
    --  string en filas: REGEXP_SUBSTR con LEVEL saca el 1er, 2do, 3er... trozo
    --  entre comas, y el CONNECT BY se detiene cuando ya no hay mas.
    PROCEDURE sp_guardar_permisos (
        p_id_rol       IN roles.id_rol%TYPE,
        p_ids_permisos IN VARCHAR2
    ) IS
    BEGIN
        DELETE FROM rol_permisos WHERE id_rol = p_id_rol;

        -- NULL = no palomearon nada = el rol se queda sin permisos. Es valido.
        IF p_ids_permisos IS NOT NULL THEN
            INSERT INTO rol_permisos (id_rol, id_permiso)
            SELECT p_id_rol,
                   TO_NUMBER(REGEXP_SUBSTR(p_ids_permisos, '[^,]+', 1, LEVEL))
            FROM dual
            CONNECT BY REGEXP_SUBSTR(p_ids_permisos, '[^,]+', 1, LEVEL) IS NOT NULL;
        END IF;
    END sp_guardar_permisos;

END pkg_roles;
/


--==============================================================================
--  3. PKG_PERMISOS  (completo: 2 procedures viejos + 2 nuevos)
--==============================================================================
CREATE OR REPLACE PACKAGE pkg_permisos AS

    PROCEDURE sp_listar (p_cursor OUT SYS_REFCURSOR);

    PROCEDURE sp_obtener_por_usuario (
        p_id_usuario IN  usuarios.id_usuario%TYPE,
        p_cursor     OUT SYS_REFCURSOR
    );

    -- Nuevos: la matriz de excepciones individuales.
    PROCEDURE sp_obtener_matriz_usuario (
        p_id_usuario IN  usuarios.id_usuario%TYPE,
        p_cursor     OUT SYS_REFCURSOR
    );

    PROCEDURE sp_guardar_overrides (
        p_id_usuario     IN usuarios.id_usuario%TYPE,
        p_ids_concedidos IN VARCHAR2,
        p_ids_negados    IN VARCHAR2
    );

END pkg_permisos;
/

CREATE OR REPLACE PACKAGE BODY pkg_permisos AS

    PROCEDURE sp_listar (p_cursor OUT SYS_REFCURSOR) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT id_permiso  AS Id,
                   nombre      AS Nombre,
                   descripcion AS Descripcion
            FROM permisos
            ORDER BY nombre;
    END sp_listar;

    -- Devuelve los permisos EFECTIVOS del usuario:
    --   (permisos del rol + concedidos 'S') - denegados 'N'
    PROCEDURE sp_obtener_por_usuario (
        p_id_usuario IN  usuarios.id_usuario%TYPE,
        p_cursor     OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT DISTINCT p.nombre AS Nombre
            FROM permisos p
            WHERE p.id_permiso IN (
                    -- permisos que trae el rol del usuario, por defecto
                    SELECT rp.id_permiso
                    FROM rol_permisos rp
                    JOIN usuarios u ON u.id_rol = rp.id_rol
                    WHERE u.id_usuario = p_id_usuario
                    UNION
                    -- permisos concedidos individualmente (excepcion positiva)
                    SELECT up.id_permiso
                    FROM usuario_permisos up
                    WHERE up.id_usuario = p_id_usuario
                      AND up.concedido = 'S'
                  )
              AND p.id_permiso NOT IN (
                    -- permisos denegados individualmente (gana sobre el rol)
                    SELECT up.id_permiso
                    FROM usuario_permisos up
                    WHERE up.id_usuario = p_id_usuario
                      AND up.concedido = 'N'
                  );
    END sp_obtener_por_usuario;

    --  La foto completa para la pantalla de excepciones de un usuario.
    --
    --  Por cada permiso del catalogo devuelve DOS datos independientes:
    --    DelRolFlag   -> 'S' si su rol se lo da por defecto
    --    OverrideFlag -> 'S' concedido a mano, 'N' negado a mano, NULL sin tocar
    --
    --  Se necesitan los dos por separado, no el resultado ya combinado: la
    --  pantalla tiene que poder decir "esto lo hereda del rol" vs "esto se lo
    --  concediste tu". Con solo el efectivo no se puede distinguir, y el
    --  administrador no sabria que esta quitando.
    PROCEDURE sp_obtener_matriz_usuario (
        p_id_usuario IN  usuarios.id_usuario%TYPE,
        p_cursor     OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT p.id_permiso  AS Id,
                   p.nombre      AS Nombre,
                   p.descripcion AS Descripcion,
                   CASE WHEN rp.id_permiso IS NULL THEN 'N' ELSE 'S' END AS DelRolFlag,
                   up.concedido  AS OverrideFlag
            FROM permisos p
            LEFT JOIN rol_permisos rp
                   ON rp.id_permiso = p.id_permiso
                  AND rp.id_rol = (SELECT u.id_rol
                                     FROM usuarios u
                                    WHERE u.id_usuario = p_id_usuario)
            LEFT JOIN usuario_permisos up
                   ON up.id_permiso = p.id_permiso
                  AND up.id_usuario = p_id_usuario
            ORDER BY p.nombre;
    END sp_obtener_matriz_usuario;

    --  Mismo criterio que pkg_roles.sp_guardar_permisos: borron y cuenta nueva,
    --  en una sola llamada atomica. Aca son dos listas porque usuario_permisos
    --  guarda el sentido de la excepcion en la columna 'concedido'.
    --
    --  Los permisos que el usuario simplemente HEREDA del rol no van en ninguna
    --  de las dos listas: no se guardan como excepcion, justamente porque no lo
    --  son. Por eso la pantalla tiene tres estados y no un checkbox.
    PROCEDURE sp_guardar_overrides (
        p_id_usuario     IN usuarios.id_usuario%TYPE,
        p_ids_concedidos IN VARCHAR2,
        p_ids_negados    IN VARCHAR2
    ) IS
    BEGIN
        DELETE FROM usuario_permisos WHERE id_usuario = p_id_usuario;

        IF p_ids_concedidos IS NOT NULL THEN
            INSERT INTO usuario_permisos (id_usuario, id_permiso, concedido)
            SELECT p_id_usuario,
                   TO_NUMBER(REGEXP_SUBSTR(p_ids_concedidos, '[^,]+', 1, LEVEL)),
                   'S'
            FROM dual
            CONNECT BY REGEXP_SUBSTR(p_ids_concedidos, '[^,]+', 1, LEVEL) IS NOT NULL;
        END IF;

        IF p_ids_negados IS NOT NULL THEN
            INSERT INTO usuario_permisos (id_usuario, id_permiso, concedido)
            SELECT p_id_usuario,
                   TO_NUMBER(REGEXP_SUBSTR(p_ids_negados, '[^,]+', 1, LEVEL)),
                   'N'
            FROM dual
            CONNECT BY REGEXP_SUBSTR(p_ids_negados, '[^,]+', 1, LEVEL) IS NOT NULL;
        END IF;
    END sp_guardar_overrides;

END pkg_permisos;
/


--==============================================================================
--  4. PKG_USUARIOS  (completo: 2 procedures viejos + 3 nuevos)
--==============================================================================
CREATE OR REPLACE PACKAGE pkg_usuarios AS

    PROCEDURE sp_obtener_por_login (
        p_login  IN  usuarios.usuario_login%TYPE,
        p_cursor OUT SYS_REFCURSOR
    );

    PROCEDURE sp_listar (p_cursor OUT SYS_REFCURSOR);

    -- Nuevos: administracion de usuarios.
    PROCEDURE sp_listar_admin (p_cursor OUT SYS_REFCURSOR);

    PROCEDURE sp_obtener_por_id (
        p_id_usuario IN  usuarios.id_usuario%TYPE,
        p_cursor     OUT SYS_REFCURSOR
    );

    PROCEDURE sp_cambiar_rol (
        p_id_usuario IN usuarios.id_usuario%TYPE,
        p_id_rol     IN usuarios.id_rol%TYPE
    );

END pkg_usuarios;
/

CREATE OR REPLACE PACKAGE BODY pkg_usuarios AS

    PROCEDURE sp_obtener_por_login (
        p_login  IN  usuarios.usuario_login%TYPE,
        p_cursor OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT id_usuario    AS Id,
                   nombre        AS Nombre,
                   apellido      AS Apellido,
                   usuario_login AS UsuarioLogin,
                   password_hash AS PasswordHash
            FROM usuarios
            WHERE usuario_login = p_login
              AND activo = 'S';
    END sp_obtener_por_login;

    -- Alimenta el dropdown de Responsable del formulario de activos.
    -- NO devuelve password_hash: es una lista para elegir gente, no para
    -- autenticar. Ese dato solo sale por sp_obtener_por_login.
    PROCEDURE sp_listar (p_cursor OUT SYS_REFCURSOR) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT id_usuario AS Id,
                   nombre     AS Nombre,
                   apellido   AS Apellido
            FROM usuarios
            WHERE activo = 'S'
            ORDER BY nombre, apellido;
    END sp_listar;

    --  Version para la pantalla de administracion: trae el rol resuelto y
    --  cuantas excepciones individuales tiene cada quien.
    --
    --  LEFT JOIN a roles, no JOIN: usuarios.id_rol es nullable (se agrego
    --  despues, con ALTER TABLE). Un usuario sin rol asignado tiene que salir
    --  en la lista igual — de hecho es EXACTAMENTE al que hay que arreglarle
    --  el rol, y con un JOIN normal seria invisible.
    --
    --  Tampoco devuelve password_hash, por lo mismo que sp_listar.
    PROCEDURE sp_listar_admin (p_cursor OUT SYS_REFCURSOR) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT u.id_usuario    AS Id,
                   u.nombre        AS Nombre,
                   u.apellido      AS Apellido,
                   u.usuario_login AS UsuarioLogin,
                   u.id_rol        AS IdRol,
                   r.nombre        AS NombreRol,
                   (SELECT COUNT(*)
                      FROM usuario_permisos up
                     WHERE up.id_usuario = u.id_usuario) AS NumExcepciones
            FROM usuarios u
            LEFT JOIN roles r ON r.id_rol = u.id_rol
            WHERE u.activo = 'S'
            ORDER BY u.nombre, u.apellido;
    END sp_listar_admin;

    PROCEDURE sp_obtener_por_id (
        p_id_usuario IN  usuarios.id_usuario%TYPE,
        p_cursor     OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN p_cursor FOR
            SELECT u.id_usuario    AS Id,
                   u.nombre        AS Nombre,
                   u.apellido      AS Apellido,
                   u.usuario_login AS UsuarioLogin,
                   u.id_rol        AS IdRol,
                   r.nombre        AS NombreRol,
                   (SELECT COUNT(*)
                      FROM usuario_permisos up
                     WHERE up.id_usuario = u.id_usuario) AS NumExcepciones
            FROM usuarios u
            LEFT JOIN roles r ON r.id_rol = u.id_rol
            WHERE u.id_usuario = p_id_usuario;
    END sp_obtener_por_id;

    PROCEDURE sp_cambiar_rol (
        p_id_usuario IN usuarios.id_usuario%TYPE,
        p_id_rol     IN usuarios.id_rol%TYPE
    ) IS
    BEGIN
        UPDATE usuarios
           SET id_rol = p_id_rol
         WHERE id_usuario = p_id_usuario;
    END sp_cambiar_rol;

END pkg_usuarios;
/


--==============================================================================
--  5. VERIFICACION
--==============================================================================
--  Si algo salio mal, esto lo dice. Debe devolver CERO filas.
SELECT object_name, object_type, status
FROM user_objects
WHERE object_type IN ('PACKAGE', 'PACKAGE BODY')
  AND status <> 'VALID';

--  Y esto debe devolver 'SEGURIDAD_ADMINISTRAR' para tu usuario admin.
--  Si no sale, no vas a poder entrar a las pantallas nuevas.
SELECT p.nombre
FROM permisos p
JOIN rol_permisos rp ON rp.id_permiso = p.id_permiso
JOIN usuarios u      ON u.id_rol = rp.id_rol
WHERE u.usuario_login = 'admin'
  AND p.nombre = 'SEGURIDAD_ADMINISTRAR';
