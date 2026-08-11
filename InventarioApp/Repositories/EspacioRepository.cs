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
    public interface IEspacioRepository
    {
        List<Espacio> Listar();
        int Insertar(Espacio area, int idUsuario);
        void Actualizar(Espacio area, int idUsuario);
        void Eliminar(int id);
    }

    public class EspacioRepository : IEspacioRepository
    {
        public List<Espacio> Listar()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Espacio>(
                    "BEGIN pkg_espacios.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public int Insertar(Espacio area, int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pIdArea = new OracleParameter("p_id_area", OracleDbType.Int32) { Value = area.IdArea };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = area.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)area.Descripcion ?? DBNull.Value };
            var pCreadoPor = new OracleParameter("p_creado_por", OracleDbType.Int32) { Value = idUsuario };
            var pIdOut = new OracleParameter("p_id_espacio_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_espacios.sp_insertar(:p_id_area, :p_nombre, :p_descripcion, :p_creado_por, :p_id_espacio_out); END;",
                    pIdArea, pNombre, pDescripcion, pCreadoPor, pIdOut);

                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }


        public void Actualizar(Espacio area, int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pIdEspacio = new OracleParameter("p_id_espacio", OracleDbType.Int32) { Value = area.Id };
            var pIdArea = new OracleParameter("p_id_area", OracleDbType.Int32) { Value = area.IdArea };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = area.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)area.Descripcion ?? DBNull.Value };
            var pActualizadoPor = new OracleParameter("p_actualizado_por", OracleDbType.Int32) { Value = idUsuario };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_espacios.sp_actualizar( :p_id_espacio, :p_id_area, :p_nombre, :p_descripcion, :p_actualizado_por); END;",
                    pIdEspacio ,pIdArea, pNombre, pDescripcion, pActualizadoPor);
            }
        }

        public void Eliminar(int id)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_espacio", OracleDbType.Int32) { Value = id };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_espacios.sp_eliminar(:p_id_espacio); END;", pId);
            }
        }
    }
}