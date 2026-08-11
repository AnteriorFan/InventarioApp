using System.Linq;
using System.Web.Mvc;
using InventarioApp.Models;
using InventarioApp.Security;
using InventarioApp.Services;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class EstadosController : Controller
    {
        private readonly IEstadoService _estadoService;
        public EstadosController() : this(new EstadoService()) { }
        public EstadosController(IEstadoService estadoService) { _estadoService = estadoService; }

        [AuthorizePermiso("CATALOGOS_VER")]
        public ActionResult Index() => View(_estadoService.ObtenerTodos());

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Create() => View();

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Estado estado)
        {
            _estadoService.Crear(estado);
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var estado = _estadoService.ObtenerTodos().FirstOrDefault(e => e.Id == id);
            return View(estado);
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Estado estado)
        {
            _estadoService.Actualizar(estado);
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _estadoService.Eliminar(id);
            return RedirectToAction("Index");
        }
    }
}
