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
    public interface IMovimientoActivoRepository
    {
        int Registrar(RegistrarMovimientoViewModel datos, string imagenKey, int idUsuario);
        List<MovimientoActivo> ListarPorActivo(int idActivo);
        List<MovimientoActivo> ListarRecientes(int limite);
    }

    public class MovimientoActivoRepository : IMovimientoActivoRepository
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString; }
        }

        //  RECORDATORIO: ODP.NET liga los parámetros POR POSICIÓN, no por nombre.
        //  El orden de este arreglo tiene que calzar exactamente con la firma de
        //  pkg_movimientos_activos.sp_registrar. Si se agrega un parámetro al
        //  procedure, hay que agregarlo aquí en el mismo lugar.
        public int Registrar(RegistrarMovimientoViewModel datos, string imagenKey, int idUsuario)
        {
            var pIdActivo = new OracleParameter("p_id_activo", OracleDbType.Int32) { Value = datos.IdActivo };
            var pIdTipo = new OracleParameter("p_id_tipo_movimiento", OracleDbType.Int32) { Value = datos.IdTipoMovimiento };
            var pUbicacion = new OracleParameter("p_id_ubicacion_destino", OracleDbType.Int32) { Value = Opcional(datos.IdUbicacionDestino) };
            var pResponsable = new OracleParameter("p_id_responsable_nuevo", OracleDbType.Int32) { Value = Opcional(datos.IdResponsableNuevo) };
            var pEstado = new OracleParameter("p_id_estado_nuevo", OracleDbType.Int32) { Value = Opcional(datos.IdEstadoNuevo) };
            var pMotivo = new OracleParameter("p_motivo", OracleDbType.Varchar2) { Value = Opcional(datos.Motivo) };
            var pObservaciones = new OracleParameter("p_observaciones", OracleDbType.Varchar2) { Value = Opcional(datos.Observaciones) };
            var pImagen = new OracleParameter("p_imagen_key", OracleDbType.Varchar2) { Value = Opcional(imagenKey) };
            var pRealizadoPor = new OracleParameter("p_realizado_por", OracleDbType.Int32) { Value = idUsuario };
            var pIdOut = new OracleParameter("p_id_movimiento_out", OracleDbType.Int32) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_movimientos_activos.sp_registrar(" +
                    ":p_id_activo, :p_id_tipo_movimiento, :p_id_ubicacion_destino, " +
                    ":p_id_responsable_nuevo, :p_id_estado_nuevo, :p_motivo, " +
                    ":p_observaciones, :p_imagen_key, :p_realizado_por, :p_id_movimiento_out); END;",
                    pIdActivo, pIdTipo, pUbicacion, pResponsable, pEstado,
                    pMotivo, pObservaciones, pImagen, pRealizadoPor, pIdOut);

                return Convert.ToInt32(pIdOut.Value.ToString());
            }
        }

        public List<MovimientoActivo> ListarPorActivo(int idActivo)
        {
            var pIdActivo = new OracleParameter("p_id_activo", OracleDbType.Int32) { Value = idActivo };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<MovimientoActivo>(
                    "BEGIN pkg_movimientos_activos.sp_listar_por_activo(:p_id_activo, :p_cursor); END;",
                    pIdActivo, pCursor).ToList();
            }
        }

        public List<MovimientoActivo> ListarRecientes(int limite)
        {
            var pLimite = new OracleParameter("p_limite", OracleDbType.Int32) { Value = limite };
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<MovimientoActivo>(
                    "BEGIN pkg_movimientos_activos.sp_listar_recientes(:p_limite, :p_cursor); END;",
                    pLimite, pCursor).ToList();
            }
        }

        // Un null de C# tiene que llegar a Oracle como DBNull, no como null.
        private static object Opcional(object valor)
        {
            if (valor == null) return DBNull.Value;

            var texto = valor as string;
            if (texto != null && string.IsNullOrWhiteSpace(texto)) return DBNull.Value;

            return valor;
        }
    }
}
