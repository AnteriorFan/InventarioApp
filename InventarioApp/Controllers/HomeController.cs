using InventarioApp.Services;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        // Ventana de análisis de las métricas de movimiento. 30 días es lo
        // suficientemente largo para que el promedio no se dispare por un pico
        // de un solo día, y lo suficientemente corto para reflejar el ritmo
        // actual y no el del trimestre pasado.
        private const int DiasVentana = 30;

        private readonly IDashboardService _dashboardService;
        private readonly IPermisoService _permisoService;

        public HomeController() : this(new DashboardService(), new PermisoService()) { }

        public HomeController(IDashboardService dashboardService, IPermisoService permisoService)
        {
            _dashboardService = dashboardService;
            _permisoService = permisoService;
        }

        public ActionResult Index()
        {
            //  Los permisos se resuelven acá, no en la vista.
            //
            //  El de bitácora además decide si la consulta se ejecuta: no basta
            //  con esconder el widget con un @if, porque el dato ya habría
            //  viajado desde la base hasta el servidor. Un EMPLEADO ve el estado
            //  del inventario; quién hizo cada cosa es otra conversación.
            bool puedeVerBitacora = _permisoService.UsuarioTienePermiso(User.Identity.Name, "HISTORIAL_VER");

            var modelo = _dashboardService.ObtenerResumen(DiasVentana, puedeVerBitacora);

            modelo.PuedeVerBitacora = puedeVerBitacora;
            modelo.PuedeRegistrarMovimiento =
                _permisoService.UsuarioTienePermiso(User.Identity.Name, "MOVIMIENTOS_REGISTRAR");

            return View(modelo);
        }
    }
}
