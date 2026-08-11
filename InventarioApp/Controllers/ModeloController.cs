using System.Linq;
using System.Web.Mvc;
using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using InventarioApp.Services;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class ModelosController : Controller
    {
        private readonly IModeloService _modeloService;
        private readonly IMarcaService _marcaService;
        private readonly IUsuarioRepository _usuarioRepository;

        public ModelosController() : this(new ModeloService(), new MarcaService(), new UsuarioRepository()) { }
        public ModelosController(IModeloService modeloService, IMarcaService marcaService, IUsuarioRepository usuarioRepository)
        {
            _modeloService = modeloService;
            _marcaService = marcaService;
            _usuarioRepository = usuarioRepository;
        }

        private int ObtenerIdUsuarioActual() => _usuarioRepository.ObtenerPorLogin(User.Identity.Name).Id;

        [AuthorizePermiso("CATALOGOS_VER")]
        public ActionResult Index() => View(_modeloService.ObtenerTodos());

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Create()
        {
            ViewBag.Marcas = new SelectList(_marcaService.ObtenerTodas(), "Id", "Nombre");
            return View();
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Modelo modelo)
        {
            _modeloService.Crear(modelo, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var modelo = _modeloService.ObtenerTodos().FirstOrDefault(m => m.Id == id);
            ViewBag.Marcas = new SelectList(_marcaService.ObtenerTodas(), "Id", "Nombre", modelo?.IdMarca);
            return View(modelo);
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Modelo modelo)
        {
            _modeloService.Actualizar(modelo, ObtenerIdUsuarioActual());
            return RedirectToAction("Index");
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _modeloService.Eliminar(id);
            return RedirectToAction("Index");
        }
    }
}
