using InventarioApp.Models;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;

namespace InventarioApp.Repositories
{
    public interface IDashboardRepository
    {
        KpiInventario ObtenerKpis();
        List<ItemReposicion> ListarReposicionUrgente(int dias);
        List<ItemMovido> ListarMasMovidos(int dias, int top);
        List<ItemAbc> ListarClasificacionAbc(int dias);
        List<MovimientoBitacora> ListarBitacoraReciente(int limite);
    }

    /// <summary>
    /// Único lugar del dashboard que habla con Oracle. Todo pasa por
    /// pkg_dashboard, que es de solo lectura: acá no hay ningún
    /// ExecuteSqlCommand, solo SqlQuery.
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString; }
        }

        //  Los cinco métodos repiten la misma coreografía: abrir conexión,
        //  pasar un REF CURSOR vacío como parámetro OUT, y leerlo como si
        //  fuera un result set normal. Este helper la escribe una sola vez.
        //
        //  Un procedure no puede "devolver una tabla" como una función: se le
        //  pasa el cursor por referencia, adentro hace OPEN cursor FOR SELECT,
        //  y del lado de C# se lee igual que un DataReader.
        private static List<T> Consultar<T>(string bloqueAnonimo, params OracleParameter[] parametrosEntrada)
        {
            var pCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor)
            {
                Direction = ParameterDirection.Output
            };

            // El cursor va SIEMPRE al final, igual que en la firma de todos los
            // procedures de pkg_dashboard: ODP.NET liga por posición.
            var parametros = new List<OracleParameter>(parametrosEntrada) { pCursor };

            using (var connection = new OracleConnection(ConnectionString))
            using (var db = new DbContext(connection, true))
            {
                return db.Database.SqlQuery<T>(bloqueAnonimo, parametros.ToArray()).ToList();
            }
        }

        public KpiInventario ObtenerKpis()
        {
            var fila = Consultar<KpiInventario>("BEGIN pkg_dashboard.sp_kpis(:p_cursor); END;")
                       .FirstOrDefault();

            // sp_kpis siempre devuelve una fila (hace SELECT ... FROM dual), pero
            // si algo falla es preferible un dashboard en ceros que una
            // NullReferenceException en la vista.
            return fila ?? new KpiInventario();
        }

        public List<ItemReposicion> ListarReposicionUrgente(int dias)
        {
            var pDias = new OracleParameter("p_dias", OracleDbType.Int32) { Value = dias };

            return Consultar<ItemReposicion>(
                "BEGIN pkg_dashboard.sp_reposicion_urgente(:p_dias, :p_cursor); END;", pDias);
        }

        public List<ItemMovido> ListarMasMovidos(int dias, int top)
        {
            var pDias = new OracleParameter("p_dias", OracleDbType.Int32) { Value = dias };
            var pTop = new OracleParameter("p_top", OracleDbType.Int32) { Value = top };

            return Consultar<ItemMovido>(
                "BEGIN pkg_dashboard.sp_mas_movidos(:p_dias, :p_top, :p_cursor); END;", pDias, pTop);
        }

        public List<ItemAbc> ListarClasificacionAbc(int dias)
        {
            var pDias = new OracleParameter("p_dias", OracleDbType.Int32) { Value = dias };

            return Consultar<ItemAbc>(
                "BEGIN pkg_dashboard.sp_clasificacion_abc(:p_dias, :p_cursor); END;", pDias);
        }

        public List<MovimientoBitacora> ListarBitacoraReciente(int limite)
        {
            var pLimite = new OracleParameter("p_limite", OracleDbType.Int32) { Value = limite };

            return Consultar<MovimientoBitacora>(
                "BEGIN pkg_dashboard.sp_bitacora_reciente(:p_limite, :p_cursor); END;", pLimite);
        }
    }
}
