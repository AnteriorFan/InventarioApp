using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using InventarioApp.Models;

namespace InventarioApp.Repositories
{
    public interface ITipoMovimientoRepository
    {
        List<TipoMovimiento> Listar();
        int Insertar(TipoMovimiento tipo);
        void Actualizar(TipoMovimiento tipo);
        void Eliminar(int id);
    }

    public class TipoMovimientoRepository : ITipoMovimientoRepository
    {
        public List<TipoMovimiento> Listar()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<TipoMovimiento>("BEGIN pkg_tipos_movimiento.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public int Insertar(TipoMovimiento tipo)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = tipo.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)tipo.Descripcion ?? DBNull.Value };
            var pIdOut = new OracleParameter("p_id_tipo_movimiento_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_tipos_movimiento.sp_insertar(:p_nombre, :p_descripcion, :p_id_tipo_movimiento_out); END;",
                    pNombre, pDescripcion, pIdOut);
                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void Actualizar(TipoMovimiento tipo)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_tipo_movimiento", OracleDbType.Int32) { Value = tipo.Id };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = tipo.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)tipo.Descripcion ?? DBNull.Value };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_tipos_movimiento.sp_actualizar(:p_id_tipo_movimiento, :p_nombre, :p_descripcion); END;",
                    pId, pNombre, pDescripcion);
            }
        }

        public void Eliminar(int id)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_tipo_movimiento", OracleDbType.Int32) { Value = id };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_tipos_movimiento.sp_eliminar(:p_id_tipo_movimiento); END;", pId);
            }
        }
    }
}
