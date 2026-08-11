using System.Linq;
using System.Web.Mvc;
using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using InventarioApp.Services;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class EspaciosController : Controller
    {
        private readonly IEspacioService _espacioService;
        private readonly IAreaService _areaService;
        private readonly IUsuarioRepository _usuarioRepository;

        public EspaciosController() : this(new EspaciosService(), new AreaService(), new UsuarioRepository()) { }
        public EspaciosController(IEspacioService espacioService, IAreaService areaService, IUsuarioRepository usuarioRepository)
        {
            _espacioService = espacioService;
            _areaService = areaService;
            _usuarioRepository = usuarioRepository;
        }

        private int ObtenerIdUsuarioActual() => _usuarioRepository.ObtenerPorLogin(User.Identity.Name).Id;

        [AuthorizePermiso("UBICACIONES_VER")]
        public ActionResult Index()
        {
            return View(_espacioService.ObtenerTodos());
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        public ActionResult Create()
        {
            ViewBag.Areas = new SelectList(_areaService.ObtenerTodos(), "Id", "Nombre");
            return View();
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Espacio espacio)
        {
            _espacioService.Crear(espacio, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var espacio = _espacioService.ObtenerTodos().FirstOrDefault(e => e.Id == id);
            ViewBag.Areas = new SelectList(_areaService.ObtenerTodos(), "Id", "Nombre", espacio?.IdArea);
            return View(espacio);
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Espacio espacio)
        {
            _espacioService.Actualizar(espacio, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _espacioService.Eliminar(id);
            return RedirectToAction("Index");
        }
    }
}
