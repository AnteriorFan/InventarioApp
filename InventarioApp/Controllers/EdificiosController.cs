using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using InventarioApp.Services;
using System.Linq;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class EdificiosController : Controller
    {
        private readonly IEdificioService _edificioService;
        private readonly IUsuarioRepository _usuarioRepository;

        public EdificiosController() : this(new EdificioService(), new UsuarioRepository()) { }
        public EdificiosController(IEdificioService edificioService, IUsuarioRepository usuarioRepository)
        {
            _edificioService = edificioService;
            _usuarioRepository = usuarioRepository;
        }

        private int ObtenerIdUsuarioActual() => _usuarioRepository.ObtenerPorLogin(User.Identity.Name).Id;

        [AuthorizePermiso("UBICACIONES_VER")]
        public ActionResult Index()
        {
            return View(_edificioService.ObtenerTodos());
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        public ActionResult Create() => View();

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Edificio edificio)
        {
            _edificioService.Crear(edificio, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var edificio = _edificioService.ObtenerTodos().FirstOrDefault(e => e.Id == id);
            return View(edificio);
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Edificio edificio)
        {
            _edificioService.Actualizar(edificio, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("UBICACIONES_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _edificioService.Eliminar(id);
            return RedirectToAction("Index");
        }
    }
}
