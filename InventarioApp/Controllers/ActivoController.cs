using System.Linq;
using System.Web.Mvc;
using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using InventarioApp.Services;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class ActivosController : Controller
    {
        private readonly IActivoService _activoService;
        private readonly ICategoriaService _categoriaService;
        private readonly IMarcaService _marcaService;
        private readonly IModeloService _modeloService;
        private readonly IEstadoService _estadoService;
        private readonly IEspacioService _espacioService;
        private readonly IUsuarioRepository _usuarioRepository;

        public ActivosController() : this(new ActivoService(), new CategoriaService(), new MarcaService(),
            new ModeloService(), new EstadoService(), new EspaciosService(), new UsuarioRepository())
        { }

        public ActivosController(IActivoService activoService, ICategoriaService categoriaService, IMarcaService marcaService,
            IModeloService modeloService, IEstadoService estadoService, IEspacioService espacioService, IUsuarioRepository usuarioRepository)
        {
            _activoService = activoService;
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _modeloService = modeloService;
            _estadoService = estadoService;
            _espacioService = espacioService;
            _usuarioRepository = usuarioRepository;
        }

        private int ObtenerIdUsuarioActual() => _usuarioRepository.ObtenerPorLogin(User.Identity.Name).Id;

        // Junto los 7 dropdowns en un solo método porque Create y Edit lo necesitan idéntico.
        private void CargarDropdowns(Activo activo = null)
        {
            ViewBag.Categorias = new SelectList(_categoriaService.ObtenerTodas(), "Id", "Nombre", activo?.IdCategoria);
            ViewBag.Marcas = new SelectList(_marcaService.ObtenerTodas(), "Id", "Nombre", activo?.IdMarca);
            ViewBag.Modelos = new SelectList(_modeloService.ObtenerTodos(), "Id", "Nombre", activo?.IdModelo);
            ViewBag.Estados = new SelectList(_estadoService.ObtenerTodos(), "Id", "Nombre", activo?.IdEstado);
            ViewBag.EspaciosOrigen = new SelectList(_espacioService.ObtenerTodos(), "Id", "Nombre", activo?.IdUbicacionOrigen);
            ViewBag.Espacios = new SelectList(_espacioService.ObtenerTodos(), "Id", "Nombre", activo?.IdUbicacionActual);
            ViewBag.Usuarios = new SelectList(
                _usuarioRepository.Listar().Select(u => new { u.Id, NombreCompleto = u.Nombre + " " + u.Apellido }),
                "Id", "NombreCompleto", activo?.IdResponsable);
        }

        [AuthorizePermiso("ACTIVOS_VER")]
        public ActionResult Index() => View(_activoService.ObtenerTodos());

        [AuthorizePermiso("ACTIVOS_ADMINISTRAR")]
        public ActionResult Create()
        {
            CargarDropdowns();
            return View();
        }

        [AuthorizePermiso("ACTIVOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Activo activo)
        {
            _activoService.Crear(activo, ObtenerIdUsuarioActual());

            if (Request.IsAjaxRequest())
                return Json(new { success = true });

            return RedirectToAction("Index");
        }

        [AuthorizePermiso("ACTIVOS_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var activo = _activoService.ObtenerPorId(id);
            CargarDropdowns(activo);
            return View(activo);
        }

        [AuthorizePermiso("ACTIVOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Activo activo)
        {
            _activoService.Actualizar(activo, ObtenerIdUsuarioActual());

            if (Request.IsAjaxRequest())
                return Json(new { success = true });

            return RedirectToAction("Index");
        }

        [AuthorizePermiso("ACTIVOS_VER")]
        public ActionResult Detalles(int? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index");

            var activo = _activoService.ObtenerPorId(id.Value);
            if (activo == null)
            {
                TempData["Error"] = "El activo que buscas no existe o fue eliminado.";
                return RedirectToAction("Index");
            }

            return View(activo);
        }

        [AuthorizePermiso("ACTIVOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _activoService.Eliminar(id);
            return RedirectToAction("Index");
        }
    }
}
