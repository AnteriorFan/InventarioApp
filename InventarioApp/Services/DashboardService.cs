using InventarioApp.Models;
using InventarioApp.Repositories;

namespace InventarioApp.Services
{
    public interface IDashboardService
    {
        /// <summary>
        /// Arma de una sola vez todo lo que necesita la pantalla de inicio.
        /// </summary>
        /// <param name="dias">Ventana de análisis, en días, para las métricas de movimiento.</param>
        /// <param name="incluirBitacora">
        /// false cuando el usuario no tiene el permiso HISTORIAL_VER: en ese caso
        /// la consulta ni siquiera se ejecuta. Ocultar el widget en la vista sería
        /// suficiente para que no se vea, pero no para que el dato no se lea.
        /// </param>
        DashboardViewModel ObtenerResumen(int dias, bool incluirBitacora);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService() : this(new DashboardRepository()) { }

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public DashboardViewModel ObtenerResumen(int dias, bool incluirBitacora)
        {
            var vm = new DashboardViewModel
            {
                DiasVentana = dias,
                Kpis = _dashboardRepository.ObtenerKpis(),
                Reposicion = _dashboardRepository.ListarReposicionUrgente(dias),
                MasMovidos = _dashboardRepository.ListarMasMovidos(dias, 8),
                Abc = _dashboardRepository.ListarClasificacionAbc(dias)
            };

            if (incluirBitacora)
            {
                vm.Bitacora = _dashboardRepository.ListarBitacoraReciente(12);
            }

            return vm;
        }
    }
}
