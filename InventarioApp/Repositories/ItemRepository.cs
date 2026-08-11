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

        Item ObtenerPorCodigo(string codigo);
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

            // p_stock_minimo: punto de reorden del item (número entero)
            var pStockMinimo = new OracleParameter("p_stock_minimo", OracleDbType.Int32) { Value = item.StockMinimo };

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

            // p_imagen_s3_key: Clave de la imagen en S3 (puede ser null → DBNull.Value)
            var pImagenS3Key = new OracleParameter("p_imagen_s3_key", OracleDbType.Varchar2) { Value = (object)item.ImagenS3Key ?? DBNull.Value };


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
                // OJO: ODP.NET liga los parámetros POR POSICIÓN, no por el nombre
                // que lleven. El orden de este array tiene que calzar exactamente
                // con el orden de los ":algo" del bloque anónimo — y ambos con la
                // firma del procedure en pkg_items.
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_items.sp_insertar(:p_codigo, :p_nombre, :p_descripcion, :p_id_categoria, :p_cantidad, :p_stock_minimo, :p_unidad_medida, :p_ubicacion, :p_imagen_s3_key, :p_item_out); END;",
                        pCodigo, pNombre, pDescripcion, pIdCategoria, pCantidad, pStockMinimo, pUnidadMedida, pUbicacion, pImagenS3Key, pItemOut);

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
            var pStockMinimo = new OracleParameter("p_stock_minimo", OracleDbType.Int32) { Value = item.StockMinimo };
            var pUnidadMedida = new OracleParameter("p_unidad_medida", OracleDbType.Varchar2) { Value = (Object)item.UnidadMedida ?? DBNull.Value };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (Object)item.Descripcion ?? DBNull.Value };
            var pIdCategoria = new OracleParameter("p_id_categoria", OracleDbType.Int32) { Value = (Object)item.IdCategoria ?? DBNull.Value };
            var pUbicacion = new OracleParameter("p_ubicacion", OracleDbType.Varchar2) { Value = (Object)item.Ubicacion ?? DBNull.Value };
            var pImagenS3Key = new OracleParameter("p_imagen_s3_key", OracleDbType.Varchar2) { Value = (object)item.ImagenS3Key ?? DBNull.Value };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_items.sp_actualizar(:p_item_id, :p_nombre, :p_descripcion, :p_id_categoria, :p_cantidad, :p_stock_minimo, :p_unidad_medida, :p_ubicacion, :p_imagen_s3_key); END;",
                    pIdItem, pNombre, pDescripcion, pIdCategoria, pCantidad, pStockMinimo, pUnidadMedida, pUbicacion, pImagenS3Key);
            }
        }
        public Item ObtenerPorCodigo(string codigo)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

            var pCodigo = new OracleParameter("p_codigo", OracleDbType.Varchar2) { Value = codigo };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(connectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Item>(
                    "BEGIN pkg_items.sp_buscar_por_codigo(:p_codigo, :p_cursor); END;",
                    pCodigo, pCursor).FirstOrDefault();
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