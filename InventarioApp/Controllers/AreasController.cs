using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using InventarioApp.Services;
using System.Linq;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class AreasController : Controller
    {
        private readonly IEdificioService _edificioService;
        private readonly IAreaService _areaService;
        private readonly IUsuarioRepository _usuarioRepository;

        public AreasController() : this(new AreaService(), new EdificioService(), new UsuarioRepository()) { }
        public AreasController(IAreaService areaService, IEdificioService edificioService, IUsuarioRepository usuarioRepository)
        {
            _areaService = areaService;
            _edificioService = edificioService;
            _usuarioRepository = usuarioRepository;
        }

        private int ObtenerIdUsuarioActual() => _usuarioRepository.ObtenerPorLogin(User.Identity.Name).Id;

        [AuthorizePermiso("UBICACIONES_VER")]
        public ActionResult Index()
        {
            return View(_areaService.ObtenerTodos());
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        public ActionResult Create()
        {
            ViewBag.Edificios = new SelectList(_edificioService.ObtenerTodos(), "Id", "Nombre");
            return View();
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Area edificio)
        {
            _areaService.Crear(edificio, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var area = _areaService.ObtenerTodos().FirstOrDefault(e => e.Id == id);
            ViewBag.Edificios = new SelectList(_edificioService.ObtenerTodos(), "Id", "Nombre", area?.IdEdificio);
            return View(area);
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Area edificio)
        {
            _areaService.Actualizar(edificio, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _areaService.Eliminar(id);
            return RedirectToAction("Index");
        }
    }
}
