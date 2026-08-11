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
    public interface IHistorialRepository
    {
        void Registrar(int idItem, int idUsuario, string accion, string detalle);
        List<HistorialItem> ListarPorItem(int idItem);
    }

    public class HistorialRepository : IHistorialRepository
    {
        public void Registrar(int idItem, int idUsuario, string accion, string detalle)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pIdItem = new OracleParameter("p_id_item", OracleDbType.Int32) { Value = idItem };
            var pIdUsuario = new OracleParameter("p_id_usuario", OracleDbType.Int32) { Value = idUsuario };
            var pAccion = new OracleParameter("p_accion", OracleDbType.Varchar2) { Value = accion };
            var pDetalle = new OracleParameter("p_detalle", OracleDbType.Varchar2) { Value = (object)detalle ?? DBNull.Value };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_historial.sp_registrar(:p_id_item, :p_id_usuario, :p_accion, :p_detalle); END;",
                    pIdItem, pIdUsuario, pAccion, pDetalle);
            }
        }

        public List<HistorialItem> ListarPorItem(int idItem)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pIdItem = new OracleParameter("p_id_item", OracleDbType.Int32) { Value = idItem };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<HistorialItem>(
                    "BEGIN pkg_historial.sp_listar_por_item(:p_id_item, :p_cursor); END;",
                    pIdItem, pCursor).ToList();
            }
        }
    }
}