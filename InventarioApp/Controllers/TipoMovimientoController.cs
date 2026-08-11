using System.Linq;
using System.Web.Mvc;
using InventarioApp.Models;
using InventarioApp.Security;
using InventarioApp.Services;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class TiposMovimientoController : Controller
    {
        private readonly ITipoMovimientoService _tipoMovimientoService;
        public TiposMovimientoController() : this(new TipoMovimientoService()) { }
        public TiposMovimientoController(ITipoMovimientoService tipoMovimientoService) { _tipoMovimientoService = tipoMovimientoService; }

        [AuthorizePermiso("CATALOGOS_VER")]
        public ActionResult Index() => View(_tipoMovimientoService.ObtenerTodos());

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Create() => View();

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TipoMovimiento tipo)
        {
            _tipoMovimientoService.Crear(tipo);
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var tipo = _tipoMovimientoService.ObtenerTodos().FirstOrDefault(t => t.Id == id);
            return View(tipo);
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(TipoMovimiento tipo)
        {
            _tipoMovimientoService.Actualizar(tipo);
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _tipoMovimientoService.Eliminar(id);
            return RedirectToAction("Index");
        }
    }
}
