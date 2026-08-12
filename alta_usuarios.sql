--==============================================================================
--  ALTA DE USUARIOS DESDE LA APP
--------------------------------------------------------------------------------
--  Hasta ahora los usuarios solo se podian crear con un INSERT a mano, y peor
--  aun: habia que calcular el hash de la contraseña por fuera, porque el hasheo
--  vive en C# (PasswordHasher, PBKDF2). Esto agrega el procedure que falta.
--
--  Correr DESPUES de seguridad_admin.sql.
--
--  OJO: el package se recrea COMPLETO otra vez (5 procedures anteriores + 1
--  nuevo). Un CREATE OR REPLACE PACKAGE BODY reemplaza el cuerpo entero.
--==============================================================================

CREATE OR REPLACE PACKAGE pkg_usuarios AS

    PROCEDURE sp_obtener_por_login (
        p_login  IN  usuarios.usuario_login%TYPE,
        p_cursor OUT SYS_REFCURSOR
    );

    PROCEDURE sp_listar (p_cursor OUT SYS_REFCURSOR);

    PROCEDURE sp_listar_admin (p_cursor OUT SYS_REFCURSOR);

    PROCEDURE sp_obtener_por_id (
        p_id_usuario IN  usuarios.id_usuario%TYPE,
        p_cursor     OUT SYS_REFCURSOR
    );

    PROCEDURE sp_cambiar_rol (
        p_id_usuario IN usuarios.id_usuario%TYPE,
        p_id_rol     IN usuarios.id_rol%TYPE
    );

    -- Nuevo: alta de usuario.
    PROCEDURE sp_registrar (
        p_nombre         IN  usuarios.nombre%TYPE,
        p_apellido       IN  usuarios.apellido%TYPE,
        p_usuario_login  IN  usuarios.usuario_login%TYPE,
        p_password_hash  IN  usuarios.password_hash%TYPE,
        p_id_rol         IN  usuarios.id_rol%TYPE,
        p_id_usuario_out OUT usuarios.id_usuario%TYPE
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

    --  Da de alta un usuario.
    --
    --  p_password_hash llega YA HASHEADO desde C#. El procedure nunca ve la
    --  contraseña real, y eso es a proposito: si el hasheo se hiciera aca,
    --  la contraseña en claro viajaria por la red hasta Oracle y quedaria
    --  escrita en cualquier traza o log de sesion que estuviera activo.
    --
    --  Tampoco se hashea en PL/SQL porque el algoritmo (PBKDF2 con 100.000
    --  iteraciones) ya vive en Security/PasswordHasher.cs, y tener DOS
    --  implementaciones del mismo hash es la receta para que un dia dejen de
    --  coincidir y nadie pueda entrar.
    --
    --  Si el login ya existe, el UNIQUE de usuario_login lanza ORA-00001 y el
    --  Controller lo traduce a un mensaje entendible.
    PROCEDURE sp_registrar (
        p_nombre         IN  usuarios.nombre%TYPE,
        p_apellido       IN  usuarios.apellido%TYPE,
        p_usuario_login  IN  usuarios.usuario_login%TYPE,
        p_password_hash  IN  usuarios.password_hash%TYPE,
        p_id_rol         IN  usuarios.id_rol%TYPE,
        p_id_usuario_out OUT usuarios.id_usuario%TYPE
    ) IS
    BEGIN
        INSERT INTO usuarios (nombre, apellido, usuario_login, password_hash, id_rol, activo)
        VALUES (p_nombre, p_apellido, p_usuario_login, p_password_hash, p_id_rol, 'S')
        RETURNING id_usuario INTO p_id_usuario_out;
    END sp_registrar;

END pkg_usuarios;
/


--==============================================================================
--  VERIFICACION
--==============================================================================
--  Cero filas = todo compilo bien.
SELECT object_name, object_type, status
FROM user_objects
WHERE object_type IN ('PACKAGE', 'PACKAGE BODY')
  AND status <> 'VALID';

--  Los 6 procedures de pkg_usuarios deben aparecer aca.
SELECT procedure_name
FROM user_procedures
WHERE object_name = 'PKG_USUARIOS'
ORDER BY procedure_name;
