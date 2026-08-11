using System;
using System.Web.Mvc;
using InventarioApp.Models;
using InventarioApp.Services;
using InventarioApp.Repositories;
using InventarioApp.Security;

namespace InventarioApp.Controllers
{


    public class MovimientosController : Controller
    {
        private readonly IMovimientoService _movimientoService;

        public MovimientosController() : this(new MovimientoService()) { }

        public MovimientosController(IMovimientoService movimientoService)
        {
            _movimientoService = movimientoService;
        }

        [AuthorizePermiso("MOVIMIENTOS_REGISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(int idItem, string tipoMovimiento, int cantidad, string observaciones)
        {
            try
            {
                _movimientoService.Registrar(idItem, tipoMovimiento, cantidad, observaciones);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al registrar el movimiento: " + ex.Message;
            }

            return RedirectToAction("Detalles", "Items", new { id = idItem });
        }
    }
}