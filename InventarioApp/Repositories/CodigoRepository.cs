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
    public interface ICodigoRepository
    {
        string Regenerar(int idActivo, string motivo, int idUsuario);
        void MarcarEtiquetaImpresa(int idActivo);
        List<Activo> ListarEtiquetasPendientes();
    }

    /// <summary>
    /// Habla con pkg_codigos: regenerar el código de un activo y llevar la
    /// cuenta de qué etiquetas faltan por imprimir.
    /// </summary>
    public class CodigoRepository : ICodigoRepository
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString; }
        }

        //  No hay un método "Generar" suelto a propósito. El código se genera
        //  DENTRO de pkg_activos.sp_insertar, cuando el código llega vacío, para
        //  que tomar el consecutivo y usarlo ocurran en la misma transacción.
        //  Si se generara aquí y se insertara después, dos altas simultáneas
        //  podrían llevarse el mismo número.
        public string Regenerar(int idActivo, string motivo, int idUsuario)
        {
            var pIdActivo = new OracleParameter("p_id_activo", OracleDbType.Int32) { Value = idActivo };
            var pMotivo = new OracleParameter("p_motivo", OracleDbType.Varchar2) { Value = (object)motivo ?? DBNull.Value };
            var pRealizadoPor = new OracleParameter("p_realizado_por", OracleDbType.Int32) { Value = idUsuario };

            // Size obligatorio en un OUT de texto: sin él ODP.NET no reserva
            // buffer y el valor vuelve vacío o truncado.
            var pCodigoOut = new OracleParameter("p_codigo_out", OracleDbType.Varchar2, 100)
            {
                Direction = ParameterDirection.Output
            };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_codigos.sp_regenerar_codigo(:p_id_activo, :p_motivo, :p_realizado_por, :p_codigo_out); END;",
                    pIdActivo, pMotivo, pRealizadoPor, pCodigoOut);

                return pCodigoOut.Value == null ? null : pCodigoOut.Value.ToString();
            }
        }

        public void MarcarEtiquetaImpresa(int idActivo)
        {
            var pIdActivo = new OracleParameter("p_id_activo", OracleDbType.Int32) { Value = idActivo };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                db.Database.ExecuteSqlCommand(
                    "BEGIN pkg_codigos.sp_marcar_etiqueta_impresa(:p_id_activo); END;", pIdActivo);
            }
        }

        public List<Activo> ListarEtiquetasPendientes()
        {
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<Activo>(
                    "BEGIN pkg_codigos.sp_listar_etiquetas_pendientes(:p_cursor); END;", pCursor).ToList();
            }
        }
    }
}
