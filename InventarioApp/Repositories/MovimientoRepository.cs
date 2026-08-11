
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
    public interface IMovimientosRepository
    {
        void RegistrarMovimiento(int idItem, string tipoMovimiento, int cantidad, string observaciones);

        List<MovimIentosinventario> ObtenerMovimientosPorItem(int idItem);
    }

    public class MovimientoRepository : IMovimientosRepository
    {
        public void RegistrarMovimiento(int idItem, string tipoMovimiento, int cantidad, string observaciones)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pItem = new OracleParameter("p_id_item", OracleDbType.Int32) { Value = idItem };
            var pTipoMovimiento = new OracleParameter("p_tipo_movimiento", OracleDbType.Varchar2) { Value = tipoMovimiento };
            var pCantidad = new OracleParameter("p_cantidad", OracleDbType.Int32) { Value = cantidad };
            var pObservaciones = new OracleParameter("p_observaciones", OracleDbType.Varchar2) { Value = (object)observaciones ?? DBNull.Value };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_movimientos.sp_registrar(:p_id_item, :p_tipo_movimiento, :p_cantidad, :p_observaciones); END;",
                    pItem, pTipoMovimiento, pCantidad, pObservaciones);
            }
        }


        public List<MovimIentosinventario> ObtenerMovimientosPorItem(int idItem)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pIdItem = new OracleParameter("p_id_item", OracleDbType.Int32) { Value = idItem };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<MovimIentosinventario>(
                    "BEGIN pkg_movimientos.sp_listar_por_item(:p_id_item, :p_cursor); END;",
                    pIdItem, pCursor).ToList();
            }
        }
    }
}