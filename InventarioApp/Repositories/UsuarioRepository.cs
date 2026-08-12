using InventarioApp.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace InventarioApp.Repositories
{
    public interface IUsuarioRepository
    {
        Usuario ObtenerPorLogin(string login);
        List<Usuario> Listar();
        List<Usuario> ListarParaAdmin();
        Usuario ObtenerPorId(int id);
        void CambiarRol(int idUsuario, int? idRol);
        int Insertar(string nombre, string apellido, string login, string passwordHash, int? idRol);
    }

    public class UsuarioRepository : IUsuarioRepository
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString; }
        }

        public Usuario ObtenerPorLogin(string login)
        {
            var pLogin = new OracleParameter("p_login", OracleDbType.Varchar2) { Value = login };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Usuario>(
                    "BEGIN pkg_usuarios.sp_obtener_por_login(:p_login, :p_cursor); END;",
                    pLogin, pCursor).FirstOrDefault();
            }
        }

        /// <summary>Lista corta (Id/Nombre/Apellido) para dropdowns.</summary>
        public List<Usuario> Listar()
        {
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Usuario>("BEGIN pkg_usuarios.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        /// <summary>Lista con rol resuelto y conteo de excepciones, para la pantalla de administración.</summary>
        public List<Usuario> ListarParaAdmin()
        {
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Usuario>("BEGIN pkg_usuarios.sp_listar_admin(:p_cursor); END;", pCursor).ToList();
            }
        }

        public Usuario ObtenerPorId(int id)
        {
            var pId = new OracleParameter("p_id_usuario", OracleDbType.Int32) { Value = id };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Usuario>(
                    "BEGIN pkg_usuarios.sp_obtener_por_id(:p_id_usuario, :p_cursor); END;",
                    pId, pCursor).FirstOrDefault();
            }
        }

        //  Recibe el hash ya calculado, no la contraseña. Este repositorio
        //  nunca ve la contraseña en claro: el hasheo se hace en UsuarioService
        //  con PasswordHasher antes de llegar aquí.
        public int Insertar(string nombre, string apellido, string login, string passwordHash, int? idRol)
        {
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = nombre };
            var pApellido = new OracleParameter("p_apellido", OracleDbType.Varchar2) { Value = apellido };
            var pLogin = new OracleParameter("p_usuario_login", OracleDbType.Varchar2) { Value = login };
            var pHash = new OracleParameter("p_password_hash", OracleDbType.Varchar2) { Value = passwordHash };
            var pIdRol = new OracleParameter("p_id_rol", OracleDbType.Int32)
            {
                Value = idRol.HasValue ? (object)idRol.Value : DBNull.Value
            };
            var pIdOut = new OracleParameter("p_id_usuario_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_usuarios.sp_registrar(:p_nombre, :p_apellido, :p_usuario_login, :p_password_hash, :p_id_rol, :p_id_usuario_out); END;",
                    pNombre, pApellido, pLogin, pHash, pIdRol, pIdOut);

                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void CambiarRol(int idUsuario, int? idRol)
        {
            var pIdUsuario = new OracleParameter("p_id_usuario", OracleDbType.Int32) { Value = idUsuario };

            // "— Sin rol —" es una opción válida en la pantalla: deja al usuario
            // sin ningún permiso heredado, solo con sus excepciones individuales.
            var pIdRol = new OracleParameter("p_id_rol", OracleDbType.Int32)
            {
                Value = idRol.HasValue ? (object)idRol.Value : DBNull.Value
            };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_usuarios.sp_cambiar_rol(:p_id_usuario, :p_id_rol); END;",
                    pIdUsuario, pIdRol);
            }
        }
    }
}
