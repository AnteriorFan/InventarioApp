using InventarioApp.Models;
using InventarioApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace InventarioApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController() : this(new AuthService()) { }
        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            var usuario = _authService.ValidarCredenciales(model.UsuarioLogin, model.Password);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos.");
                return View(model);
            }

            var ticket = new FormsAuthenticationTicket(
                1,
                usuario.UsuarioLogin,
                DateTime.Now,
                DateTime.Now.AddMinutes(60),
                false,
                string.Empty // ya no se guarda el rol aquí: AuthorizePermisoAttribute consulta permisos en vivo por usuario_login
            );

            string encryptedTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            Response.Cookies.Add(cookie);

            // Al dashboard, no al listado: lo primero que necesita ver alguien
            // que entra es qué hay que atender hoy, no la tabla completa.
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }
}