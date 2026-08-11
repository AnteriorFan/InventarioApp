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
    public interface IEdificioRepository
    {
        List<Edificio> Listar();
        int Insertar(Edificio edificio, int idUsuario);
        void Actualizar(Edificio edificio, int idUsuario);
        void Eliminar(int id);
    }

    public class EdificioRepository : IEdificioRepository
    {
        public List<Edificio> Listar()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Edificio>(
                    "BEGIN pkg_edificios.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public int Insertar(Edificio edificio, int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = edificio.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)edificio.Descripcion ?? DBNull.Value };
            var pCreadoPor = new OracleParameter("p_creado_por", OracleDbType.Int32) { Value = idUsuario };
            var pIdOut = new OracleParameter("p_id_edificio_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_edificios.sp_insertar(:p_nombre, :p_descripcion, :p_creado_por, :p_id_edificio_out); END;",
                    pNombre, pDescripcion, pCreadoPor, pIdOut);

                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void Actualizar(Edificio edificio, int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pId = new OracleParameter("p_id_edificio", OracleDbType.Int32) { Value = edificio.Id };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = edificio.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)edificio.Descripcion ?? DBNull.Value };
            var pActualizadoPor = new OracleParameter("p_actualizado_por", OracleDbType.Int32) { Value = idUsuario };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_edificios.sp_actualizar(:p_id_edificio, :p_nombre, :p_descripcion, :p_actualizado_por); END;",
                    pId, pNombre, pDescripcion, pActualizadoPor);
            }
        }

        public void Eliminar(int id)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_edificio", OracleDbType.Int32) { Value = id };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_edificios.sp_eliminar(:p_id_edificio); END;", pId);
            }
        }
    }
}
