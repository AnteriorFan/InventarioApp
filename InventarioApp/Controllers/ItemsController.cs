using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Services;
using InventarioApp.Security;
using System;
using System.Web;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    [Authorize]
    public class ItemsController : Controller
    {
        private readonly IItemService _itemService;
        private readonly IHistorialService _historialService;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly ICategoriaService _categoriaService;
        private readonly IMovimientoService _movimientoService;
        private readonly IImageStorage _imageStorage;

        public ItemsController() : this(new ItemService(), new HistorialService(), new UsuarioRepository(), new CategoriaService(), new MovimientoService(), new LocalImageStorage()) { }

        public ItemsController(IItemService itemService, IHistorialService historialService, IUsuarioRepository usuarioRepository, ICategoriaService categoriaService, IMovimientoService movimientoService, IImageStorage imageStorage)
        {
            _itemService = itemService;
            _historialService = historialService;
            _usuarioRepository = usuarioRepository;
            _categoriaService = categoriaService;
            _movimientoService = movimientoService;
            _imageStorage = imageStorage;
        }

        private int ObtenerIdUsuarioActual()
        {
            var usuario = _usuarioRepository.ObtenerPorLogin(User.Identity.Name);
            return usuario.Id;
        }

        public ActionResult Index()
        {
            var items = _itemService.ObtenerTodos();
            return View(items);
        }

        [AuthorizePermiso("ITEMS_CREAR")]
        public ActionResult Create()
        {
            ViewBag.Categorias = new SelectList(_categoriaService.ObtenerTodas(), "Id", "Nombre");

            // Se manda un Item vacío en vez de View() pelado para que el form
            // arranque con un stock mínimo sugerido en vez de un 0, que en la
            // práctica significa "no me avises nunca".
            return View(new Item { StockMinimo = 5 });
        }

        [AuthorizePermiso("ITEMS_CREAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Item nuevoItem, HttpPostedFileBase imagen)
        {
            nuevoItem.ImagenS3Key = _imageStorage.Guardar(imagen);

            int nuevoId = _itemService.Crear(nuevoItem);
            _historialService.Registrar(nuevoId, ObtenerIdUsuarioActual(), "ALTA", $"Item creado: {nuevoItem.Nombre} (código {nuevoItem.Codigo})");

            if (Request.IsAjaxRequest())
                return Json(new { success = true });

            return RedirectToAction("Index");
        }

        [AuthorizePermiso("ITEMS_EDITAR")]
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index");

            var item = _itemService.ObtenerPorId(id.Value);
            if (item == null)
            {
                TempData["Error"] = "El item que buscas no existe o fue eliminado.";
                return RedirectToAction("Index");
            }

            ViewBag.Categorias = new SelectList(_categoriaService.ObtenerTodas(), "Id", "Nombre", item.IdCategoria);
            return View(item);
        }

        [AuthorizePermiso("ITEMS_EDITAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Item item, HttpPostedFileBase imagen)
        {
            if (imagen != null && imagen.ContentLength > 0)
            {
                item.ImagenS3Key = _imageStorage.Guardar(imagen);
            }
            else
            {
                item.ImagenS3Key = _itemService.ObtenerPorId(item.Id).ImagenS3Key; // conserva la que ya tenía
            }

            _itemService.Actualizar(item);
            _historialService.Registrar(item.Id, ObtenerIdUsuarioActual(), "MODIFICACION", $"Item modificado: {item.Nombre}");

            if (Request.IsAjaxRequest())
                return Json(new { success = true });

            return RedirectToAction("Index");
        }


        [AuthorizePermiso("ITEMS_ELIMINAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete()
        {
            int id = Convert.ToInt32(Request.Form["id"]);
            _itemService.Eliminar(id);
            _historialService.Registrar(id, ObtenerIdUsuarioActual(), "BAJA", "Item eliminado (borrado lógico)");

            // TempData, no ModelState: solo TempData sobrevive un redirect.
            TempData["Exito"] = "Item eliminado correctamente.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public JsonResult BuscarPorCodigo(string codigo)
        {
            var item = _itemService.ObtenerPorCodigo(codigo);
            return Json(item, JsonRequestBehavior.AllowGet);
        }

        //  El escáner se mudó a su propio controller cuando dejó de ser solo de
        //  items (ahora también lee activos). Esta action se queda como
        //  redirección para no romper enlaces guardados ni marcadores.
        //
        //  302 y no 301: un redirect permanente lo cachea el navegador y ya no
        //  vuelve a preguntar, así que si mañana se decide otra cosa habría que
        //  ir a limpiar la caché de cada equipo para poder probarlo.
        public ActionResult Escanear()
        {
            return RedirectToAction("Index", "Escaner");
        }


        public ActionResult Detalles(int? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index");

            var item = _itemService.ObtenerPorId(id.Value);
            if (item == null)
            {
                TempData["Error"] = "El item que buscas no existe o fue eliminado.";
                return RedirectToAction("Index");
            }

            var viewModel = new DetalleItemViewModel
            {
                Item = item,
                Movimientos = _movimientoService.ObtenerPorItem(id.Value)
            };
            return View(viewModel);
        }

        [AuthorizePermiso("HISTORIAL_VER")]
        public ActionResult Historial(int? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index");

            var historial = _historialService.ObtenerPorItem(id.Value);
            return View(historial);
        }
    }
}