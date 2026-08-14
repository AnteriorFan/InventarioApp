using System;
using System.Linq;
using System.Web.Mvc;
using InventarioApp.Helpers;
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

        //  Alta rápida desde el formulario de Activos, igual que en Marcas.
        //  El id de la marca lo manda la pantalla: es la que ya está elegida en
        //  el dropdown de arriba, así que no hay que volver a preguntarla.
        [AuthorizePermiso("CATALOGOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult CrearRapido(int? idMarca, string nombre)
        {
            if (!idMarca.HasValue)
                return Json(new { success = false, mensaje = "Elige primero la marca: un modelo siempre pertenece a una." });

            if (string.IsNullOrWhiteSpace(nombre))
                return Json(new { success = false, mensaje = "El nombre del modelo es obligatorio." });

            try
            {
                var modelo = new Modelo { IdMarca = idMarca.Value, Nombre = nombre.Trim() };

                int id = _modeloService.Crear(modelo, ObtenerIdUsuarioActual());
                return Json(new { success = true, id, nombre = modelo.Nombre });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    mensaje = ErrorOracle.Traducir(ex,
                        "No se pudo crear el modelo.",
                        "Esa marca ya tiene un modelo con ese nombre.")
                });
            }
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
