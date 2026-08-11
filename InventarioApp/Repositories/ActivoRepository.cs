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
    public interface IActivoRepository
    {
        List<Activo> Listar();
        Activo ObtenerPorId(int id);
        Activo ObtenerPorCodigo(string codigo);
        int Insertar(Activo activo, int idUsuario);
        void Actualizar(Activo activo, int idUsuario);
        void Eliminar(int id);
    }

    public class ActivoRepository : IActivoRepository
    {
        // Pequeño atajo: en vez de repetir esta línea en cada método (son varios aquí), la centralizo en una propiedad.
        private string ConnectionString => ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;

        public List<Activo> Listar()
        {
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Activo>("BEGIN pkg_activos.sp_listar(:p_cursor); END;", pCursor).ToList();
            }
        }

        public Activo ObtenerPorId(int id)
        {
            var pId = new OracleParameter("p_id_activo", OracleDbType.Int32) { Value = id };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Activo>(
                    "BEGIN pkg_activos.sp_obtener_por_id(:p_id_activo, :p_cursor); END;", pId, pCursor).FirstOrDefault();
            }
        }

        public Activo ObtenerPorCodigo(string codigo)
        {
            var pCodigo = new OracleParameter("p_codigo", OracleDbType.Varchar2) { Value = codigo };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Activo>(
                    "BEGIN pkg_activos.sp_buscar_por_codigo(:p_codigo, :p_cursor); END;", pCodigo, pCursor).FirstOrDefault();
            }
        }

        public int Insertar(Activo activo, int idUsuario)
        {
            var pCodigo = new OracleParameter("p_codigo", OracleDbType.Varchar2) { Value = activo.Codigo };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = activo.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)activo.Descripcion ?? DBNull.Value };
            var pIdCategoria = new OracleParameter("p_id_categoria", OracleDbType.Int32) { Value = (object)activo.IdCategoria ?? DBNull.Value };
            var pIdMarca = new OracleParameter("p_id_marca", OracleDbType.Int32) { Value = (object)activo.IdMarca ?? DBNull.Value };
            var pIdModelo = new OracleParameter("p_id_modelo", OracleDbType.Int32) { Value = (object)activo.IdModelo ?? DBNull.Value };
            var pNumeroSerie = new OracleParameter("p_numero_serie", OracleDbType.Varchar2) { Value = (object)activo.NumeroSerie ?? DBNull.Value };
            var pIdEstado = new OracleParameter("p_id_estado", OracleDbType.Int32) { Value = (object)activo.IdEstado ?? DBNull.Value };
            var pIdUbicacionOrigen = new OracleParameter("p_id_ubicacion_origen", OracleDbType.Int32) { Value = (object)activo.IdUbicacionOrigen ?? DBNull.Value };
            var pIdUbicacionActual = new OracleParameter("p_id_ubicacion_actual", OracleDbType.Int32) { Value = (object)activo.IdUbicacionActual ?? DBNull.Value };
            var pResponsable = new OracleParameter("p_responsable", OracleDbType.Int32) { Value = (object)activo.IdResponsable ?? DBNull.Value };
            var pFechaCompra = new OracleParameter("p_fecha_compra", OracleDbType.Date) { Value = (object)activo.FechaCompra ?? DBNull.Value };
            var pCosto = new OracleParameter("p_costo", OracleDbType.Decimal) { Value = (object)activo.Costo ?? DBNull.Value };
            var pGarantiaHasta = new OracleParameter("p_garantia_hasta", OracleDbType.Date) { Value = (object)activo.GarantiaHasta ?? DBNull.Value };
            var pObservaciones = new OracleParameter("p_observaciones", OracleDbType.Varchar2) { Value = (object)activo.Observaciones ?? DBNull.Value };
            var pCreadoPor = new OracleParameter("p_creado_por", OracleDbType.Int32) { Value = idUsuario };
            var pIdOut = new OracleParameter("p_id_activo_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_activos.sp_insertar(:p_codigo, :p_nombre, :p_descripcion, :p_id_categoria, :p_id_marca, :p_id_modelo, :p_numero_serie, :p_id_estado, :p_id_ubicacion_origen, :p_id_ubicacion_actual, :p_responsable, :p_fecha_compra, :p_costo, :p_garantia_hasta, :p_observaciones, :p_creado_por, :p_id_activo_out); END;",
                    pCodigo, pNombre, pDescripcion, pIdCategoria, pIdMarca, pIdModelo, pNumeroSerie, pIdEstado,
                    pIdUbicacionOrigen, pIdUbicacionActual, pResponsable, pFechaCompra, pCosto, pGarantiaHasta,
                    pObservaciones, pCreadoPor, pIdOut);

                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public void Actualizar(Activo activo, int idUsuario)
        {
            var pId = new OracleParameter("p_id_activo", OracleDbType.Int32) { Value = activo.Id };
            var pNombre = new OracleParameter("p_nombre", OracleDbType.Varchar2) { Value = activo.Nombre };
            var pDescripcion = new OracleParameter("p_descripcion", OracleDbType.Varchar2) { Value = (object)activo.Descripcion ?? DBNull.Value };
            var pIdCategoria = new OracleParameter("p_id_categoria", OracleDbType.Int32) { Value = (object)activo.IdCategoria ?? DBNull.Value };
            var pIdMarca = new OracleParameter("p_id_marca", OracleDbType.Int32) { Value = (object)activo.IdMarca ?? DBNull.Value };
            var pIdModelo = new OracleParameter("p_id_modelo", OracleDbType.Int32) { Value = (object)activo.IdModelo ?? DBNull.Value };
            var pNumeroSerie = new OracleParameter("p_numero_serie", OracleDbType.Varchar2) { Value = (object)activo.NumeroSerie ?? DBNull.Value };
            var pIdEstado = new OracleParameter("p_id_estado", OracleDbType.Int32) { Value = (object)activo.IdEstado ?? DBNull.Value };
            var pIdUbicacionOrigen = new OracleParameter("p_id_ubicacion_origen", OracleDbType.Int32) { Value = (object)activo.IdUbicacionOrigen ?? DBNull.Value };
            var pIdUbicacionActual = new OracleParameter("p_id_ubicacion_actual", OracleDbType.Int32) { Value = (object)activo.IdUbicacionActual ?? DBNull.Value };
            var pResponsable = new OracleParameter("p_responsable", OracleDbType.Int32) { Value = (object)activo.IdResponsable ?? DBNull.Value };
            var pFechaCompra = new OracleParameter("p_fecha_compra", OracleDbType.Date) { Value = (object)activo.FechaCompra ?? DBNull.Value };
            var pCosto = new OracleParameter("p_costo", OracleDbType.Decimal) { Value = (object)activo.Costo ?? DBNull.Value };
            var pGarantiaHasta = new OracleParameter("p_garantia_hasta", OracleDbType.Date) { Value = (object)activo.GarantiaHasta ?? DBNull.Value };
            var pObservaciones = new OracleParameter("p_observaciones", OracleDbType.Varchar2) { Value = (object)activo.Observaciones ?? DBNull.Value };
            var pActualizadoPor = new OracleParameter("p_actualizado_por", OracleDbType.Int32) { Value = idUsuario };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_activos.sp_actualizar(:p_id_activo, :p_nombre, :p_descripcion, :p_id_categoria, :p_id_marca, :p_id_modelo, :p_numero_serie, :p_id_estado, :p_id_ubicacion_origen, :p_id_ubicacion_actual, :p_responsable, :p_fecha_compra, :p_costo, :p_garantia_hasta, :p_observaciones, :p_actualizado_por); END;",
                    pId, pNombre, pDescripcion, pIdCategoria, pIdMarca, pIdModelo, pNumeroSerie, pIdEstado,
                    pIdUbicacionOrigen, pIdUbicacionActual, pResponsable, pFechaCompra, pCosto, pGarantiaHasta,
                    pObservaciones, pActualizadoPor);
            }
        }

        public void Eliminar(int id)
        {
            var pId = new OracleParameter("p_id_activo", OracleDbType.Int32) { Value = id };
            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand("BEGIN pkg_activos.sp_eliminar(:p_id_activo); END;", pId);
            }
        }
    }
}
