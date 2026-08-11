using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using Oracle.ManagedDataAccess.Client;

namespace InventarioApp.Repositories
{
    public interface IPermisoRepository
    {
        List<string> ObtenerPorUsuario(int idUsuario);
    }

    public class PermisoRepository : IPermisoRepository
    {
        public List<string> ObtenerPorUsuario(int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pIdUsuario = new OracleParameter("p_id_usuario", OracleDbType.Int32) { Value = idUsuario };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<string>(
                    "BEGIN pkg_permisos.sp_obtener_por_usuario(:p_id_usuario, :p_cursor); END;",
                    pIdUsuario, pCursor).ToList();
            }
        }
    }
}
