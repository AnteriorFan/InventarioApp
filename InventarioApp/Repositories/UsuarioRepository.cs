using InventarioApp.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace InventarioApp.Repositories
{
    public interface IUsuarioRepository
    {
        Usuario ObtenerPorLogin(string login);
        List<Usuario> Listar();
    }

    public class UsuarioRepository : IUsuarioRepository
    {
        public Usuario ObtenerPorLogin(string login)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pLogin = new OracleParameter("p_login", OracleDbType.Varchar2) { Value = login };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Usuario>(
                    "BEGIN pkg_usuarios.sp_obtener_por_login(:p_login, :p_cursor); END;",
                    pLogin, pCursor).FirstOrDefault();
            }
        }

        public List<Usuario> Listar()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Usuario>("BEGIN pkg_usuarios.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }
    }
}