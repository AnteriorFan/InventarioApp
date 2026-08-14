using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using InventarioApp.Helpers;
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
        private readonly IMovimientoActivoService _movimientoService;
        private readonly ITipoMovimientoService _tipoMovimientoService;
        private readonly IImageStorage _imageStorage;
        private readonly ICodigoService _codigoService;

        public ActivosController() : this(new ActivoService(), new CategoriaService(), new MarcaService(),
            new ModeloService(), new EstadoService(), new EspaciosService(), new UsuarioRepository(),
            new MovimientoActivoService(), new TipoMovimientoService(), new LocalImageStorage(), new CodigoService())
        { }

        public ActivosController(IActivoService activoService, ICategoriaService categoriaService, IMarcaService marcaService,
            IModeloService modeloService, IEstadoService estadoService, IEspacioService espacioService, IUsuarioRepository usuarioRepository,
            IMovimientoActivoService movimientoService, ITipoMovimientoService tipoMovimientoService, IImageStorage imageStorage,
            ICodigoService codigoService)
        {
            _codigoService = codigoService;
            _activoService = activoService;
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _modeloService = modeloService;
            _estadoService = estadoService;
            _espacioService = espacioService;
            _usuarioRepository = usuarioRepository;
            _movimientoService = movimientoService;
            _tipoMovimientoService = tipoMovimientoService;
            _imageStorage = imageStorage;
        }

        private int ObtenerIdUsuarioActual() => _usuarioRepository.ObtenerPorLogin(User.Identity.Name).Id;

        // Junto los 7 dropdowns en un solo método porque Create y Edit lo necesitan idéntico.
        private void CargarDropdowns(Activo activo = null)
        {
            var categorias = _categoriaService.ObtenerTodas();
            var marcas = _marcaService.ObtenerTodas();

            //  Las abreviaturas viajan a la vista para poder armar la vista
            //  previa del código en el navegador conforme se eligen los
            //  dropdowns. El código DEFINITIVO no sale de aquí: el consecutivo
            //  lo reparte Oracle al insertar, porque dos personas dando de alta
            //  a la vez tendrían el mismo número si lo calculara el navegador.
            //
            //  La clave es string y NO int: Json.Encode usa JavaScriptSerializer,
            //  que solo serializa diccionarios con clave de texto y revienta con
            //  un Dictionary<int,...>. Da igual para el JavaScript, porque las
            //  claves de un objeto de JS son cadenas de todos modos, y .val() de
            //  un <select> también devuelve string.
            ViewBag.AbrevCategorias = categorias.ToDictionary(c => c.Id.ToString(), c => c.Abreviatura ?? "");
            ViewBag.AbrevMarcas = marcas.ToDictionary(m => m.Id.ToString(), m => m.Abreviatura ?? "");

            ViewBag.Categorias = new SelectList(categorias, "Id", "Nombre", activo?.IdCategoria);
            ViewBag.Marcas = new SelectList(marcas, "Id", "Nombre", activo?.IdMarca);
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
            //  nombre es NOT NULL en la tabla. Sin esta comprobación el vacío
            //  llega hasta Oracle y sale un ORA-01400 en pantalla amarilla, que
            //  no le dice nada a quien está capturando.
            if (string.IsNullOrWhiteSpace(activo.Nombre))
                return ErrorDeFormulario("Ponle un nombre al activo.", activo);

            try
            {
                _activoService.Crear(activo, ObtenerIdUsuarioActual());
            }
            catch (Exception ex)
            {
                //  Aquí caen las dos fallas realistas: el código duplicado, y
                //  los ORA-2005x de pkg_codigos cuando falta la abreviatura de
                //  la categoría o de la marca.
                return ErrorDeFormulario(
                    ErrorOracle.Traducir(ex, "No se pudo crear el activo.", "Ya existe un activo con ese código."),
                    activo);
            }

            if (Request.IsAjaxRequest())
                return Json(new { success = true });

            return RedirectToAction("Index");
        }

        //  El formulario se usa por AJAX (dentro del modal) y como página
        //  completa. Cada camino necesita una respuesta distinta: JSON con el
        //  mensaje para que el modal lo pinte sin cerrarse, o la vista de vuelta
        //  con los datos que ya se habían capturado.
        private ActionResult ErrorDeFormulario(string mensaje, Activo activo)
        {
            if (Request.IsAjaxRequest())
                return Json(new { success = false, mensaje });

            TempData["Error"] = mensaje;
            CargarDropdowns(activo);
            return View(activo);
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

            CargarDatosMovimiento(activo);
            return View(new DetalleActivoViewModel
            {
                Activo = activo,
                Movimientos = _movimientoService.ObtenerPorActivo(activo.Id)
            });
        }

        //  Lo que necesita el formulario de "registrar movimiento" del panel
        //  derecho de Detalles.
        private void CargarDatosMovimiento(Activo activo)
        {
            var tipos = _tipoMovimientoService.ObtenerTodos();

            ViewBag.TiposMovimiento = new SelectList(tipos, "Id", "Nombre");
            ViewBag.EspaciosMovimiento = new SelectList(_espacioService.ObtenerTodos(), "Id", "Nombre");
            ViewBag.EstadosMovimiento = new SelectList(_estadoService.ObtenerTodos(), "Id", "Nombre");
            ViewBag.UsuariosMovimiento = new SelectList(
                _usuarioRepository.Listar().Select(u => new { u.Id, NombreCompleto = u.Nombre + " " + u.Apellido }),
                "Id", "NombreCompleto");

            //  Las reglas de cada tipo viajan a la vista como un diccionario para
            //  que el JavaScript pueda marcar el motivo y la foto como
            //  obligatorios EN CUANTO se elige el tipo, sin ir al servidor.
            //
            //  Clave string por lo mismo que arriba: JavaScriptSerializer no
            //  serializa diccionarios con clave numérica.
            ViewBag.ReglasTipos = tipos.ToDictionary(
                t => t.Id.ToString(),
                t => new { requiereMotivo = t.RequiereMotivo, requiereImagen = t.RequiereImagen });
        }

        [AuthorizePermiso("ACTIVOS_MOVER")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarMovimiento(RegistrarMovimientoViewModel modelo, HttpPostedFileBase evidencia)
        {
            var tipo = modelo.IdTipoMovimiento.HasValue
                ? _movimientoService.ObtenerTipo(modelo.IdTipoMovimiento.Value)
                : null;

            if (tipo == null)
            {
                TempData["Error"] = "Elige un tipo de movimiento válido.";
                return RedirectToAction("Detalles", new { id = modelo.IdActivo });
            }

            //  Las reglas se comprueban aquí para poder dar un mensaje claro,
            //  PERO el procedure las vuelve a comprobar. No es redundancia
            //  inútil: esta capa se puede saltar (llamando la base por fuera, o
            //  con un POST armado a mano), la de Oracle no.
            if (tipo.RequiereMotivo && string.IsNullOrWhiteSpace(modelo.Motivo))
            {
                TempData["Error"] = "El movimiento \"" + tipo.Nombre + "\" exige un motivo. Explica por qué.";
                return RedirectToAction("Detalles", new { id = modelo.IdActivo });
            }

            if (tipo.RequiereImagen && (evidencia == null || evidencia.ContentLength == 0))
            {
                TempData["Error"] = "El movimiento \"" + tipo.Nombre + "\" exige una foto como evidencia.";
                return RedirectToAction("Detalles", new { id = modelo.IdActivo });
            }

            try
            {
                string imagenKey = _imageStorage.Guardar(evidencia);
                _movimientoService.Registrar(modelo, imagenKey, ObtenerIdUsuarioActual());

                TempData["Exito"] = "Movimiento registrado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ErrorOracle.Traducir(ex, "No se pudo registrar el movimiento.");
            }

            return RedirectToAction("Detalles", new { id = modelo.IdActivo });
        }

        //  Regenerar el código es el "procedimiento aparte y explícito" del que
        //  habla el comentario de pkg_activos.sp_actualizar: el código NO se
        //  cambia con el update de todos los días, solo por aquí, con motivo, y
        //  dejando el movimiento en el historial.
        [AuthorizePermiso("ACTIVOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegenerarCodigo(int id, string motivo)
        {
            try
            {
                string nuevoCodigo = _codigoService.Regenerar(id, motivo, ObtenerIdUsuarioActual());

                //  El aviso dice explícitamente que hay que reimprimir: no
                //  guardamos el código anterior, así que cualquier etiqueta ya
                //  pegada con el código viejo deja de encontrar este activo.
                TempData["Exito"] = "Código regenerado: " + nuevoCodigo +
                                    ". Imprime la etiqueta nueva y reemplaza la anterior.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ErrorOracle.Traducir(ex, "No se pudo regenerar el código.");
            }

            return RedirectToAction("Detalles", new { id });
        }

        [AuthorizePermiso("ACTIVOS_ADMINISTRAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarEtiquetaImpresa(int id)
        {
            _codigoService.MarcarEtiquetaImpresa(id);
            TempData["Exito"] = "Etiqueta marcada como impresa.";
            return RedirectToAction("Detalles", new { id });
        }

        /// <summary>Lista de activos cuya etiqueta todavía no se ha impreso.</summary>
        [AuthorizePermiso("ACTIVOS_VER")]
        public ActionResult Etiquetas()
        {
            return View(_codigoService.ObtenerEtiquetasPendientes());
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
