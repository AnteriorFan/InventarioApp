using System.Collections.Generic;
using System.Web.Mvc;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using InventarioApp.Models;

namespace InventarioApp.Repositories
{
    public interface IItemRepository
    {
        List<Item> Listar();
        int Insertar(Item item);
        Item ObtenerPorId(int id);
        void Actualizar(Item item);
        void Eliminar(int id);
    }

    public class ItemRepository : IItemRepository
    {

        public List<Item> Listar()
        {
            // Obtiene la cadena de conexión desde la configuración
            var connectionString = ConfigurationManager.
                ConnectionStrings["OracleDbContext"].ConnectionString;
            // Crea un parámetro de salida para recibir el cursor de la base de datos
            var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
            {
                Direction = ParameterDirection.Output
            };
            // Abre la conexión a Oracle (se cierra automáticamente al terminar el using)
            using (var connection = new OracleConnection(connectionString))
            // Crea un contexto de base de datos (se cierra automáticamente)
            using (var db = new DbContext(connection, true))
            {
                // Ejecuta el procedimiento almacenado SP_LISTAR_ITEMS
                // Convierte los resultados a una lista de Items
                return db.Database.SqlQuery<Item>("BEGIN pkg_items.sp_listar(:p_cursor); END;",
                    cursorParam).ToList();
            }
        }

        public int Insertar(Item item)
        {
            // Obtiene la cadena de conexión desde la configuración (cadena de conexión a Oracle)
            var connectionString = ConfigurationManager.
                ConnectionStrings["OracleDbContext"].ConnectionString;

            // === PARÁMETROS OBLIGATORIOS (no pueden ser nulos) ===
            // p_codigo: Código único del item (texto/Varchar2)
            var pCodigo = new OracleParameter("p_codigo", OracleDbType.Varchar2) { Value = item.Codigo };

            // p_nombre: Nombre del item (texto/Varchar2)
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = item.Nombre };

            // p_cantidad: Cantidad en stock (número entero)
            var pCantidad = new OracleParameter("p_cantidad", OracleDbType.Int32) { Value = item.Cantidad };

            var pUnidadMedida = new OracleParameter("p_unidad_medida", OracleDbType.Varchar2) { Value = (Object)item.UnidadMedida ?? DBNull.Value};

            // === PARÁMETROS OPCIONALES (pueden ser nulos) ===
            // p_descripcion: Descripción del item (puede ser null → DBNull.Value)
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2)
            {
                Value = (Object)item.Descripcion ?? DBNull.Value  // Cast a Object permite usar ?? para null
            };

            // p_id_categoria: ID de categoría (puede ser null → DBNull.Value)
            var pIdCategoria = new OracleParameter("p_id_categoria", OracleDbType.Int32)
            {
                Value = (Object)item.IdCategoria ?? DBNull.Value  // Si es null, envía DBNull.Value
            };

            // p_ubicacion: Ubicación del item (puede ser null → DBNull.Value)
            var pUbicacion = new OracleParameter("p_ubicacion", OracleDbType.Varchar2)
            {
                Value = (Object)item.Ubicacion ?? DBNull.Value  // Si es null, envía DBNull.Value
            };

            // === PARÁMETRO DE SALIDA ===
            // p_item_out: Recibe el ID del item insertado desde Oracle (parámetro OUTPUT)
            var pItemOut = new OracleParameter("p_item_out", OracleDbType.Int32)
            {
                Direction = ParameterDirection.Output  // Es un parámetro de SALIDA
            };

            // Abre conexión a Oracle y ejecuta el procedimiento almacenado
            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                // Ejecuta el procedimiento almacenado pkg_items.sp_insertar en Oracle
                // Pasa todos los parámetros de entrada y salida
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_items.sp_insertar(:p_codigo, :p_nombre, :p_descripcion, :p_id_categoria, :p_cantidad, :p_unidad_medida,:p_ubicacion, :p_item_out); END;",
                    pCodigo, pNombre, pDescripcion, pIdCategoria, pCantidad, pUnidadMedida, pUbicacion, pItemOut);

                // Obtiene el valor del parámetro de salida (ID del nuevo item) y lo retorna
                return Convert.ToInt32(pItemOut.Value.ToString());
            }
        }

        public Item ObtenerPorId(int id)
        {
            var connectionString = ConfigurationManager.
                ConnectionStrings["OracleDbContext"].ConnectionString;

            var pItemId = new OracleParameter("p_item_id", OracleDbType.Int32) { Value = id };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))

            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Item>(
                    "BEGIN pkg_items.sp_obtener_por_id(:p_item_id, :p_cursor); END;", pItemId, pCursor).FirstOrDefault();
               
            }
        }

        public void Actualizar(Item item)
        {
            var connectionString = ConfigurationManager.
                ConnectionStrings["OracleDbContext"].ConnectionString;

            var pIdItem = new OracleParameter("p_item_id", OracleDbType.Int32) { Value = item.Id };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = item.Nombre };
            var pCantidad = new OracleParameter("p_cantidad", OracleDbType.Int32) { Value = item.Cantidad };
            var pUnidadMedida = new OracleParameter("p_unidad_medida", OracleDbType.Varchar2) { Value = (Object)item.UnidadMedida ?? DBNull.Value };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (Object)item.Descripcion ?? DBNull.Value };
            var pIdCategoria = new OracleParameter("p_id_categoria", OracleDbType.Int32) { Value = (Object)item.IdCategoria ?? DBNull.Value };
            var pUbicacion = new OracleParameter("p_ubicacion", OracleDbType.Varchar2) { Value = (Object)item.Ubicacion ?? DBNull.Value };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_items.sp_actualizar(:p_item_id, :p_nombre, :p_descripcion, :p_id_categoria, :p_cantidad, :p_unidad_medida,:p_ubicacion); END;",
                    pIdItem, pNombre, pDescripcion, pIdCategoria, pCantidad, pUnidadMedida, pUbicacion);

                
            }
        }


        public void Eliminar(int id)
        {
            var connectionString = ConfigurationManager.
                ConnectionStrings["OracleDbContext"].ConnectionString;
            var pItemId = new OracleParameter("p_item_id", OracleDbType.Int32) { Value = id };
            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_items.sp_eliminar(:p_item_id); END;", pItemId);
            }
        }
    }
}