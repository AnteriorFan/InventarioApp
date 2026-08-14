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
    public interface IMarcaRepository
    {
        List<Marca> Listar();
        int Insertar(Marca marca, int idUsuario);
        void Actualizar(Marca marca, int idUsuario);
        void Eliminar(int id);
    }

    public class MarcaRepository : IMarcaRepository
    {
        public List<Marca> Listar()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Marca>("BEGIN pkg_marcas.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public int Insertar(Marca marca, int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = marca.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)marca.Descripcion ?? DBNull.Value };
            var pAbreviatura = new OracleParameter("p_abreviatura", OracleDbType.Varchar2) { Value = (object)marca.Abreviatura ?? DBNull.Value };
            var pCreadoPor = new OracleParameter("p_creado_por", OracleDbType.Int32) { Value = idUsuario };
            var pIdOut = new OracleParameter("p_id_marca_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_marcas.sp_insertar(:p_nombre, :p_descripcion, :p_abreviatura, :p_creado_por, :p_id_marca_out); END;",
                    pNombre, pDescripcion, pAbreviatura, pCreadoPor, pIdOut);
                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void Actualizar(Marca marca, int idUsuario)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_marca", OracleDbType.Int32) { Value = marca.Id };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = marca.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)marca.Descripcion ?? DBNull.Value };
            var pAbreviatura = new OracleParameter("p_abreviatura", OracleDbType.Varchar2) { Value = (object)marca.Abreviatura ?? DBNull.Value };
            var pActualizadoPor = new OracleParameter("p_actualizado_por", OracleDbType.Int32) { Value = idUsuario };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_marcas.sp_actualizar(:p_id_marca, :p_nombre, :p_descripcion, :p_abreviatura, :p_actualizado_por); END;",
                    pId, pNombre, pDescripcion, pAbreviatura, pActualizadoPor);
            }
        }

        public void Eliminar(int id)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var pId = new OracleParameter("p_id_marca", OracleDbType.Int32) { Value = id };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_marcas.sp_eliminar(:p_id_marca); END;", pId);
            }
        }
    }
}
