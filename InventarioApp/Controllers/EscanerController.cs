using InventarioApp.Models;
using InventarioApp.Security;
using InventarioApp.Services;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    //  El escáner dejó de ser una pantalla de Items.
    //
    //  Vivía en /Items/Escanear porque cuando se hizo solo existían los items.
    //  Ahora los activos también traen su QR pegado, y una persona que escanea
    //  no sabe —ni tiene por qué saber— si lo que tiene enfrente está guardado
    //  como item o como activo: solo apunta y lee. Dejarlo bajo /Items obligaba
    //  a elegir un dueño arbitrario para algo que cruza los dos módulos.
    [Authorize]
    public class EscanerController : Controller
    {
        private readonly IItemService _itemService;
        private readonly IActivoService _activoService;

        public EscanerController() : this(new ItemService(), new ActivoService()) { }

        public EscanerController(IItemService itemService, IActivoService activoService)
        {
            _itemService = itemService;
            _activoService = activoService;
        }

        public ActionResult Index()
        {
            // La vista esconde el aviso sobre activos si el usuario no los ve.
            ViewBag.PuedeVerActivos = PermisosDelRequest.Tiene(HttpContext, "ACTIVOS_VER");
            return View();
        }

        [HttpGet]
        public JsonResult Buscar(string codigo)
        {
            codigo = (codigo ?? "").Trim();

            if (codigo.Length == 0)
                return Json(ResultadoEscaneo.NoEncontrado(codigo), JsonRequestBehavior.AllowGet);

            //  Se busca primero en items y luego en activos. Son tablas
            //  distintas con su propio UNIQUE, así que en teoría un mismo código
            //  podría existir en las dos; en la práctica no debería pasar porque
            //  los prefijos son distintos (TEC-…, ACT-…). Si algún día pasa,
            //  gana el item — y conviene saberlo antes que descubrirlo.
            var item = _itemService.ObtenerPorCodigo(codigo);
            if (item != null)
                return Json(DesdeItem(item), JsonRequestBehavior.AllowGet);

            //  El permiso se comprueba ANTES de consultar, no después de traer
            //  el activo: si no puede verlos, ese dato ni siquiera se lee de la
            //  base. Es el mismo criterio del dashboard con la bitácora.
            if (PermisosDelRequest.Tiene(HttpContext, "ACTIVOS_VER"))
            {
                var activo = _activoService.ObtenerPorCodigo(codigo);
                if (activo != null)
                    return Json(DesdeActivo(activo), JsonRequestBehavior.AllowGet);
            }

            return Json(ResultadoEscaneo.NoEncontrado(codigo), JsonRequestBehavior.AllowGet);
        }

        private ResultadoEscaneo DesdeItem(Item item)
        {
            string estadoTexto;
            string estadoClase;

            if (item.Cantidad == 0)
            {
                estadoTexto = "Agotado";
                estadoClase = "danger";
            }
            else if (item.Cantidad <= item.StockMinimo)
            {
                estadoTexto = item.Cantidad + " " + item.UnidadMedida + " · bajo mínimo";
                estadoClase = "warning";
            }
            else
            {
                estadoTexto = item.Cantidad + " " + item.UnidadMedida;
                estadoClase = "success";
            }

            var resultado = new ResultadoEscaneo
            {
                Tipo = ResultadoEscaneo.TipoItem,
                TipoTexto = "Item",
                Codigo = item.Codigo,
                Nombre = item.Nombre,
                Url = Url.Action("Detalles", "Items", new { id = item.Id }),
                EstadoTexto = estadoTexto,
                EstadoClase = estadoClase
            };

            resultado.Detalles.Add(new DetalleEscaneo("Ubicación", item.Ubicacion));
            resultado.Detalles.Add(new DetalleEscaneo("Categoría", item.NombreCategoria));
            resultado.Detalles.Add(new DetalleEscaneo("Descripción", item.Descripcion));

            return resultado;
        }

        private ResultadoEscaneo DesdeActivo(Activo activo)
        {
            var resultado = new ResultadoEscaneo
            {
                Tipo = ResultadoEscaneo.TipoActivo,
                TipoTexto = "Activo",
                Codigo = activo.Codigo,
                Nombre = activo.Nombre,
                Url = Url.Action("Detalles", "Activos", new { id = activo.Id }),
                EstadoTexto = string.IsNullOrWhiteSpace(activo.NombreEstado) ? "Sin estado" : activo.NombreEstado,
                EstadoClase = "secondary"
            };

            string marcaModelo = ((activo.NombreMarca ?? "") + " " + (activo.NombreModelo ?? "")).Trim();

            resultado.Detalles.Add(new DetalleEscaneo("Ubicación actual", activo.NombreUbicacionActual));
            resultado.Detalles.Add(new DetalleEscaneo("Marca / Modelo", marcaModelo));
            resultado.Detalles.Add(new DetalleEscaneo("Nº de serie", activo.NumeroSerie));
            resultado.Detalles.Add(new DetalleEscaneo("Responsable", activo.NombreResponsable));

            return resultado;
        }
    }
}
