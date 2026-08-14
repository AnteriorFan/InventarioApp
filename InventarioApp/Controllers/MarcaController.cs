using InventarioApp.Helpers;
using InventarioApp.Models;
using InventarioApp.Repositories;
using InventarioApp.Security;
using InventarioApp.Services;
using System;
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

        //  Alta rápida desde el formulario de Activos.
        //
        //  Sin esto, toparse con una marca que no existe obliga a abandonar el
        //  alta del activo, ir a Catálogos, crearla, volver y capturar todo otra
        //  vez. Devuelve JSON para que la pantalla agregue la opción al
        //  dropdown sin recargar ni perder lo escrito.
        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CrearRapido(string nombre, string abreviatura)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return Json(new { success = false, mensaje = "El nombre de la marca es obligatorio." });

            //  La abreviatura NO es opcional aquí, aunque la columna la admita
            //  vacía: una marca sin abreviatura hace fallar la generación
            //  automática del código justo cuando se guarde el activo, y para
            //  entonces ya no se ve de dónde vino el problema.
            if (string.IsNullOrWhiteSpace(abreviatura))
                return Json(new { success = false, mensaje = "Pon la abreviatura: sin ella no se puede generar el código del activo." });

            try
            {
                var marca = new Marca
                {
                    Nombre = nombre.Trim(),
                    Abreviatura = abreviatura.Trim().ToUpperInvariant()
                };

                int id = _marcaService.Crear(marca, ObtenerIdUsuarioActual());
                return Json(new { success = true, id, nombre = marca.Nombre });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    mensaje = ErrorOracle.Traducir(ex,
                        "No se pudo crear la marca.",
                        "Ya existe una marca con ese nombre o esa abreviatura.")
                });
            }
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
