using InventarioApp.Helpers;
using InventarioApp.Models;
using InventarioApp.Security;
using InventarioApp.Services;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace InventarioApp.Controllers
{
    //  Administración de usuarios: qué rol tiene cada quien y qué excepciones
    //  individuales se le pusieron encima.
    //
    //  Este controller NO da de alta usuarios ni cambia contraseñas. El alta
    //  vive en AccountController porque necesita el hasheo de PBKDF2, y
    //  mezclarlo acá obligaría a este controller a conocer credenciales para
    //  hacer un trabajo que es solo de autorización.
    [Authorize]
    [AuthorizePermiso("SEGURIDAD_ADMINISTRAR")]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IRolService _rolService;

        public UsuariosController() : this(new UsuarioService(), new RolService()) { }

        public UsuariosController(IUsuarioService usuarioService, IRolService rolService)
        {
            _usuarioService = usuarioService;
            _rolService = rolService;
        }

        public ActionResult Index()
        {
            //  Se manda la lista cruda, no un SelectList ya armado: cada fila
            //  necesita su propio SelectList con SU rol preseleccionado, y eso
            //  se construye en la vista dentro del foreach.
            ViewBag.Roles = _rolService.ObtenerTodos();
            return View(_usuarioService.ObtenerParaAdmin());
        }

        // El formulario de alta sí usa un SelectList normal: es uno solo.
        private void CargarRoles(int? seleccionado = null)
        {
            ViewBag.ListaRoles = new SelectList(_rolService.ObtenerTodos(), "Id", "Nombre", seleccionado);
        }

        public ActionResult Create()
        {
            CargarRoles();
            return View(new CrearUsuarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CrearUsuarioViewModel modelo)
        {
            //  ModelState.IsValid aplica de un golpe todas las reglas que están
            //  como atributos en CrearUsuarioViewModel ([Required], [Compare],
            //  la expresión regular del login...). Si algo falla, se vuelve a
            //  mostrar la misma vista y @Html.ValidationMessageFor pinta cada
            //  mensaje junto a su campo.
            if (!ModelState.IsValid)
            {
                CargarRoles(modelo.IdRol);
                return View(modelo);
            }

            try
            {
                int nuevoId = _usuarioService.Crear(modelo);

                TempData["Exito"] = "Usuario creado. Ya puede iniciar sesión.";

                // Igual que al crear un rol: se manda directo a revisar qué
                // puede hacer, que es el paso que de verdad falta.
                return RedirectToAction("Permisos", new { id = nuevoId });
            }
            catch (Exception ex)
            {
                //  El UNIQUE de usuario_login es la falla realista aquí, y no se
                //  puede comprobar antes sin una condición de carrera: entre el
                //  "¿existe?" y el INSERT alguien más pudo tomar ese login. Se
                //  deja que la base sea la que decide y se traduce el error.
                ModelState.AddModelError("",
                    ErrorOracle.Traducir(ex,
                        "No se pudo crear el usuario.",
                        "Ese nombre de usuario ya está ocupado. Elige otro."));

                CargarRoles(modelo.IdRol);
                return View(modelo);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarRol(int id, int? idRol)
        {
            _usuarioService.CambiarRol(id, idRol);

            TempData["Exito"] = "Rol del usuario actualizado.";
            return RedirectToAction("Index");
        }

        /// <summary>Matriz de excepciones individuales del usuario.</summary>
        public ActionResult Permisos(int id)
        {
            var vm = _usuarioService.ObtenerConPermisos(id);
            if (vm == null)
            {
                TempData["Error"] = "El usuario que buscas no existe.";
                return RedirectToAction("Index");
            }

            return View(vm);
        }

        //  El nombre del método es distinto al del GET a propósito: C# no permite
        //  dos métodos con la MISMA firma en la misma clase, y los atributos de
        //  MVC ([HttpPost]) no cuentan como parte de la firma. [ActionName] es
        //  lo que hace que la URL siga siendo /Usuarios/Permisos en los dos.
        //
        //  En RolesController no hizo falta porque ahí el POST recibe un
        //  parámetro extra (int[] permisos) y las firmas ya son diferentes.
        [HttpPost]
        [ActionName("Permisos")]
        [ValidateAntiForgeryToken]
        public ActionResult GuardarPermisos(int id)
        {
            //  La vista manda un radio por permiso, llamado "permiso_<id>", con
            //  valor HEREDA / CONCEDER / NEGAR.
            //
            //  No se puede usar model binding a una lista acá porque el nombre
            //  del campo lleva el id adentro, así que se recorre el form a mano.
            //  A cambio, agregar un permiso nuevo al catálogo no obliga a tocar
            //  nada de este código.
            var conceder = new List<int>();
            var negar = new List<int>();

            const string prefijo = "permiso_";

            foreach (string campo in Request.Form.AllKeys)
            {
                if (campo == null || !campo.StartsWith(prefijo)) continue;

                int idPermiso;
                if (!int.TryParse(campo.Substring(prefijo.Length), out idPermiso)) continue;

                switch (Request.Form[campo])
                {
                    case PermisoDeUsuario.EstadoConceder:
                        conceder.Add(idPermiso);
                        break;

                    case PermisoDeUsuario.EstadoNegar:
                        negar.Add(idPermiso);
                        break;

                    //  HEREDA no se guarda en ningún lado, y eso es exactamente
                    //  lo correcto: la AUSENCIA de fila en usuario_permisos es
                    //  lo que significa "sin excepción, haz lo que diga el rol".
                }
            }

            _usuarioService.GuardarOverrides(id, conceder, negar);

            TempData["Exito"] = "Permisos del usuario actualizados.";
            return RedirectToAction("Permisos", new { id });
        }
    }
}
