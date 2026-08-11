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
    public interface IEstadoRepository
    {
        List<Estado> Listar();
        int Insertar(Estado estado);
        void Actualizar(Estado estado);
        void Eliminar(int id);
    }

    public class EstadoRepository : IEstadoRepository
    {
        public List<Estado> Listar()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Estado>("BEGIN pkg_estados.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public int Insertar(Estado estado)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = estado.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)estado.Descripcion ?? DBNull.Value };
            var pIdOut = new OracleParameter("p_id_estado_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_estados.sp_insertar(:p_nombre, :p_descripcion, :p_id_estado_out); END;",
                    pNombre, pDescripcion, pIdOut);
                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void Actualizar(Estado estado)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_estado", OracleDbType.Int32) { Value = estado.Id };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = estado.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)estado.Descripcion ?? DBNull.Value };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_estados.sp_actualizar(:p_id_estado, :p_nombre, :p_descripcion); END;",
                    pId, pNombre, pDescripcion);
            }
        }

        public void Eliminar(int id)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_estado", OracleDbType.Int32) { Value = id };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_estados.sp_eliminar(:p_id_estado); END;", pId);
            }
        }
    }
}
