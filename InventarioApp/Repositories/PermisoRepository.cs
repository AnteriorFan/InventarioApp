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
    public interface IPermisoRepository
    {
        List<string> ObtenerPorUsuario(int idUsuario);
        List<Permiso> Listar();
        List<PermisoDeUsuario> ObtenerMatrizUsuario(int idUsuario);
        void GuardarOverrides(int idUsuario, IEnumerable<int> idsConcedidos, IEnumerable<int> idsNegados);
    }

    public class PermisoRepository : IPermisoRepository
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString; }
        }

        public List<string> ObtenerPorUsuario(int idUsuario)
        {
            var pIdUsuario = new OracleParameter("p_id_usuario", OracleDbType.Int32) { Value = idUsuario };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<string>(
                    "BEGIN pkg_permisos.sp_obtener_por_usuario(:p_id_usuario, :p_cursor); END;",
                    pIdUsuario, pCursor).ToList();
            }
        }

        public List<Permiso> Listar()
        {
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Permiso>("BEGIN pkg_permisos.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public List<PermisoDeUsuario> ObtenerMatrizUsuario(int idUsuario)
        {
            var pIdUsuario = new OracleParameter("p_id_usuario", OracleDbType.Int32) { Value = idUsuario };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<PermisoDeUsuario>(
                    "BEGIN pkg_permisos.sp_obtener_matriz_usuario(:p_id_usuario, :p_cursor); END;",
                    pIdUsuario, pCursor).ToList();
            }
        }

        //  Dos listas separadas porque usuario_permisos guarda el SENTIDO de la
        //  excepción en la columna 'concedido' (S/N). Los permisos que el
        //  usuario simplemente hereda del rol no van en ninguna de las dos:
        //  no son excepciones.
        public void GuardarOverrides(int idUsuario, IEnumerable<int> idsConcedidos, IEnumerable<int> idsNegados)
        {
            var pIdUsuario = new OracleParameter("p_id_usuario", OracleDbType.Int32) { Value = idUsuario };
            var pConcedidos = new OracleParameter("p_ids_concedidos", OracleDbType.Varchar2) { Value = ComoCsv(idsConcedidos) };
            var pNegados = new OracleParameter("p_ids_negados", OracleDbType.Varchar2) { Value = ComoCsv(idsNegados) };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_permisos.sp_guardar_overrides(:p_id_usuario, :p_ids_concedidos, :p_ids_negados); END;",
                    pIdUsuario, pConcedidos, pNegados);
            }
        }

        private static object ComoCsv(IEnumerable<int> ids)
        {
            if (ids == null) return DBNull.Value;

            string csv = string.Join(",", ids);
            return string.IsNullOrEmpty(csv) ? (object)DBNull.Value : csv;
        }
    }
}
