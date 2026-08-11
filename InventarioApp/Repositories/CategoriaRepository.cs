using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using InventarioApp.Models;

namespace InventarioApp.Repositories
{
    public interface ICategoriaRepository
    {
        List<Categoria> Listar();
    }

    public class CategoriaRepository : ICategoriaRepository
    {
        public List<Categoria> Listar()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Categoria>(
                    "BEGIN pkg_categorias.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }
    }
}
