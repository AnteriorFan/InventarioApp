using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using InventarioApp.Services;
using System.Linq;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class MarcasController : Controller
    {
        private readonly IMarcaService _marcaService;
        private readonly IUsuarioRepository _usuarioRepository;

        public MarcasController() : this(new MarcaService(), new UsuarioRepository()) { }
        public MarcasController(IMarcaService marcaService, IUsuarioRepository usuarioRepository)
        {
            _marcaService = marcaService;
            _usuarioRepository = usuarioRepository;
        }

        private int ObtenerIdUsuarioActual() => _usuarioRepository.ObtenerPorLogin(User.Identity.Name).Id;

        [AuthorizePermiso("CATALOGOS_VER")]
        public ActionResult Index() => View(_marcaService.ObtenerTodas());

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Create() => View();

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Marca marca)
        {
            _marcaService.Crear(marca, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var marca = _marcaService.ObtenerTodas().FirstOrDefault(m => m.Id == id);
            return View(marca);
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Marca marca)
        {
            _marcaService.Actualizar(marca, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _marcaService.Eliminar(id);
            return RedirectToAction("Index");
        }
    }
}
