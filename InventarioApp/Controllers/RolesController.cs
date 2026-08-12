using InventarioApp.Helpers;
using InventarioApp.Models;
using InventarioApp.Security;
using InventarioApp.Services;
using System;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    //  Administración de roles y de los permisos que trae cada uno.
    //
    //  Todo el controller pide SEGURIDAD_ADMINISTRAR: no tiene sentido separar
    //  "ver" de "administrar" acá, porque ver qué puede hacer cada rol ya es
    //  información sensible.
    [Authorize]
    [AuthorizePermiso("SEGURIDAD_ADMINISTRAR")]
    public class RolesController : Controller
    {
        private readonly IRolService _rolService;

        public RolesController() : this(new RolService()) { }

        public RolesController(IRolService rolService)
        {
            _rolService = rolService;
        }

        public ActionResult Index() => View(_rolService.ObtenerTodos());

        public ActionResult Create() => View(new Rol());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Rol rol)
        {
            if (string.IsNullOrWhiteSpace(rol.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre del rol es obligatorio.");
                return View(rol);
            }

            try
            {
                int nuevoId = _rolService.Crear(rol);

                // Un rol recién creado no sirve de nada sin permisos, así que en
                // vez de volver al listado se manda directo a asignárselos.
                TempData["Exito"] = "Rol creado. Ahora elige qué puede hacer.";
                return RedirectToAction("Permisos", new { id = nuevoId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", MensajeDeOracle(ex, "No se pudo crear el rol."));
                return View(rol);
            }
        }

        public ActionResult Edit(int id)
        {
            var rol = _rolService.ObtenerPorId(id);
            if (rol == null)
            {
                TempData["Error"] = "El rol que buscas no existe.";
                return RedirectToAction("Index");
            }

            return View(rol);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Rol rol)
        {
            if (string.IsNullOrWhiteSpace(rol.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre del rol es obligatorio.");
                return View(rol);
            }

            try
            {
                _rolService.Actualizar(rol);
                TempData["Exito"] = "Rol actualizado.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", MensajeDeOracle(ex, "No se pudo actualizar el rol."));
                return View(rol);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                _rolService.Eliminar(id);
                TempData["Exito"] = "Rol eliminado.";
            }
            catch (Exception ex)
            {
                // pkg_roles.sp_eliminar lanza ORA-20010 si el rol todavía tiene
                // usuarios. Ese mensaje está escrito para leerse, así que se
                // muestra tal cual en vez de un "ocurrió un error" genérico.
                TempData["Error"] = MensajeDeOracle(ex, "No se pudo eliminar el rol.");
            }

            return RedirectToAction("Index");
        }

        /// <summary>Matriz de permisos del rol.</summary>
        public ActionResult Permisos(int id)
        {
            var vm = _rolService.ObtenerConPermisos(id);
            if (vm == null)
            {
                TempData["Error"] = "El rol que buscas no existe.";
                return RedirectToAction("Index");
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Permisos(int id, int[] permisos)
        {
            //  permisos llega null cuando NO se palomeó ninguno. Es un caso
            //  válido (un rol sin permisos), no un error: hay que pasarlo tal
            //  cual para que el procedure borre los que tenía.
            _rolService.GuardarPermisos(id, permisos ?? new int[0]);

            TempData["Exito"] = "Permisos del rol actualizados.";
            return RedirectToAction("Index");
        }

        //  Los procedures usan RAISE_APPLICATION_ERROR con mensajes pensados
        //  para el usuario final, pero ODP.NET los entrega envueltos:
        //
        //    ORA-20010: No se puede eliminar el rol: ...
        //    ORA-06512: at "INVENTARIO.PKG_ROLES", line 42
        //
        //  ErrorOracle.Traducir se queda con la parte legible. Vive en Helpers
        //  porque UsuariosController necesita exactamente lo mismo.
        private static string MensajeDeOracle(Exception ex, string mensajePorDefecto)
        {
            return ErrorOracle.Traducir(ex, mensajePorDefecto, "Ya existe un rol con ese nombre.");
        }
    }
}
