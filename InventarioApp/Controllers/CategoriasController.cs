using InventarioApp.Helpers;
using InventarioApp.Models;
using InventarioApp.Security;
using InventarioApp.Services;
using System;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    //  El último catálogo que le faltaba pantalla. Hasta ahora las categorías
    //  solo se podían dar de alta con un INSERT a mano, y desde que el código de
    //  los activos se genera solo, además había que acordarse de ponerle la
    //  abreviatura con un UPDATE aparte.
    [Authorize]
    public class CategoriasController : Controller
    {
        private readonly ICategoriaService _categoriaService;

        public CategoriasController() : this(new CategoriaService()) { }
        public CategoriasController(ICategoriaService categoriaService)
        {
            _categoriaService = categoriaService;
        }

        [AuthorizePermiso("CATALOGOS_VER")]
        public ActionResult Index() => View(_categoriaService.ObtenerTodas());

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Create() => View(new Categoria());

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Categoria categoria)
        {
            string error = Validar(categoria);
            if (error != null)
            {
                ModelState.AddModelError("", error);
                return View(categoria);
            }

            try
            {
                _categoriaService.Crear(categoria);
                TempData["Exito"] = "Categoría creada.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", MensajeDeOracle(ex, "No se pudo crear la categoría."));
                return View(categoria);
            }
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        public ActionResult Edit(int id)
        {
            var categoria = _categoriaService.ObtenerPorId(id);
            if (categoria == null)
            {
                TempData["Error"] = "La categoría que buscas no existe.";
                return RedirectToAction("Index");
            }

            return View(categoria);
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Categoria categoria)
        {
            string error = Validar(categoria);
            if (error != null)
            {
                ModelState.AddModelError("", error);
                return View(categoria);
            }

            try
            {
                _categoriaService.Actualizar(categoria);
                TempData["Exito"] = "Categoría actualizada.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", MensajeDeOracle(ex, "No se pudo actualizar la categoría."));
                return View(categoria);
            }
        }

        //  Alta rápida desde el formulario de Activos, igual que Marcas y
        //  Modelos. Ahora que existe sp_insertar ya se puede: antes esta era la
        //  única del trío que obligaba a salirse a SQL Developer.
        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CrearRapido(string nombre, string abreviatura)
        {
            var categoria = new Categoria { Nombre = (nombre ?? "").Trim(), Abreviatura = (abreviatura ?? "").Trim() };

            string error = Validar(categoria);
            if (error != null)
                return Json(new { success = false, mensaje = error });

            try
            {
                int id = _categoriaService.Crear(categoria);
                return Json(new { success = true, id, nombre = categoria.Nombre });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = MensajeDeOracle(ex, "No se pudo crear la categoría.") });
            }
        }

        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                _categoriaService.Eliminar(id);
                TempData["Exito"] = "Categoría eliminada.";
            }
            catch (Exception ex)
            {
                // pkg_categorias.sp_eliminar lanza ORA-20060 si todavía la usan
                // items o activos, con el conteo de cada uno.
                TempData["Error"] = MensajeDeOracle(ex, "No se pudo eliminar la categoría.");
            }

            return RedirectToAction("Index");
        }

        private static string Validar(Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.Nombre))
                return "El nombre de la categoría es obligatorio.";

            //  La abreviatura se exige aquí aunque la columna la admita vacía.
            //  Una categoría sin abreviatura no truena al guardarse: truena
            //  después, al dar de alta un activo con ella, y ahí ya no se ve de
            //  dónde vino el problema.
            if (string.IsNullOrWhiteSpace(categoria.Abreviatura))
                return "Pon la abreviatura: sin ella no se puede generar el código de los activos de esta categoría.";

            if (categoria.Abreviatura.Trim().Length < 2)
                return "La abreviatura necesita al menos 2 letras.";

            return null;
        }

        private static string MensajeDeOracle(Exception ex, string mensajePorDefecto)
        {
            return ErrorOracle.Traducir(ex, mensajePorDefecto,
                "Ya existe una categoría con ese nombre o esa abreviatura.");
        }
    }
}
