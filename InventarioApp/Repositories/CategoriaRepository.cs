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
    public interface ICategoriaRepository
    {
        List<Categoria> Listar();
        int Insertar(Categoria categoria);
        void Actualizar(Categoria categoria);
        void Eliminar(int id);
    }

    public class CategoriaRepository : ICategoriaRepository
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString; }
        }

        public List<Categoria> Listar()
        {
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Categoria>(
                    "BEGIN pkg_categorias.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        //  categorias no lleva creado_por / actualizado_por: es una tabla de la
        //  versión 1 y nunca tuvo columnas de auditoría, a diferencia de marcas
        //  o modelos. Por eso estos métodos no reciben idUsuario.
        public int Insertar(Categoria categoria)
        {
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = categoria.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)categoria.Descripcion ?? DBNull.Value };
            var pAbreviatura = new OracleParameter("p_abreviatura", OracleDbType.Varchar2) { Value = (object)categoria.Abreviatura ?? DBNull.Value };
            var pIdOut = new OracleParameter("p_id_categoria_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_categorias.sp_insertar(:p_nombre, :p_descripcion, :p_abreviatura, :p_id_categoria_out); END;",
                    pNombre, pDescripcion, pAbreviatura, pIdOut);

                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void Actualizar(Categoria categoria)
        {
            var pId = new OracleParameter("p_id_categoria", OracleDbType.Int32) { Value = categoria.Id };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = categoria.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)categoria.Descripcion ?? DBNull.Value };
            var pAbreviatura = new OracleParameter("p_abreviatura", OracleDbType.Varchar2) { Value = (object)categoria.Abreviatura ?? DBNull.Value };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_categorias.sp_actualizar(:p_id_categoria, :p_nombre, :p_descripcion, :p_abreviatura); END;",
                    pId, pNombre, pDescripcion, pAbreviatura);
            }
        }

        public void Eliminar(int id)
        {
            var pId = new OracleParameter("p_id_categoria", OracleDbType.Int32) { Value = id };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_categorias.sp_eliminar(:p_id_categoria); END;", pId);
            }
        }
    }
}
