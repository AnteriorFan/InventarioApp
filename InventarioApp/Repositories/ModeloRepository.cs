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
    public interface IModeloRepository
    {
        List<Modelo> Listar();
        int Insertar(Modelo modelo, int idUsuario);
        void Actualizar(Modelo modelo, int idUsuario);
        void Eliminar(int id);
    }

    public class ModeloRepository : IModeloRepository
    {
        public List<Modelo> Listar()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Modelo>("BEGIN pkg_modelos.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public int Insertar(Modelo modelo, int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pIdMarca = new OracleParameter("p_id_marca", OracleDbType.Int32) { Value = modelo.IdMarca };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = modelo.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)modelo.Descripcion ?? DBNull.Value };
            var pCreadoPor = new OracleParameter("p_creado_por", OracleDbType.Int32) { Value = idUsuario };
            var pIdOut = new OracleParameter("p_id_modelo_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_modelos.sp_insertar(:p_id_marca, :p_nombre, :p_descripcion, :p_creado_por, :p_id_modelo_out); END;",
                    pIdMarca, pNombre, pDescripcion, pCreadoPor, pIdOut);
                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void Actualizar(Modelo modelo, int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_modelo", OracleDbType.Int32) { Value = modelo.Id };
            var pIdMarca = new OracleParameter("p_id_marca", OracleDbType.Int32) { Value = modelo.IdMarca };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = modelo.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)modelo.Descripcion ?? DBNull.Value };
            var pActualizadoPor = new OracleParameter("p_actualizado_por", OracleDbType.Int32) { Value = idUsuario };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_modelos.sp_actualizar(:p_id_modelo, :p_id_marca, :p_nombre, :p_descripcion, :p_actualizado_por); END;",
                    pId, pIdMarca, pNombre, pDescripcion, pActualizadoPor);
            }
        }

        public void Eliminar(int id)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_modelo", OracleDbType.Int32) { Value = id };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_modelos.sp_eliminar(:p_id_modelo); END;", pId);
            }
        }
    }
}
