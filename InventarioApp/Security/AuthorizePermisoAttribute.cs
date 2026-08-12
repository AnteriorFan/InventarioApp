using System.Web;
using System.Web.Mvc;

namespace InventarioApp.Security
{
    public class AuthorizePermisoAttribute : AuthorizeAttribute
    {
        private readonly string _permisoRequerido;

        public AuthorizePermisoAttribute(string permiso)
        {
            _permisoRequerido = permiso;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
                return false;

            // Pasa por el caché por petición: si el navbar ya preguntó, esto ya
            // no vuelve a ir a Oracle.
            return PermisosDelRequest.Tiene(httpContext, _permisoRequerido);
        }

        //  Sin esto, un usuario YA AUTENTICADO al que solo le falta el permiso
        //  terminaba en la pantalla de iniciar sesión, y parecía que se le había
        //  caído la sesión.
        //
        //  El motivo: AuthorizeAttribute contesta 401 (no sé quién eres) para
        //  los dos casos, y el módulo de Forms Authentication convierte
        //  cualquier 401 en una redirección al loginUrl. Pero "no sé quién eres"
        //  y "sé quién eres y no puedes" son cosas distintas: la segunda es 403,
        //  y con 403 nadie redirige a ningún lado.
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var contexto = filterContext.HttpContext;

            if (!contexto.User.Identity.IsAuthenticated)
            {
                // No ha iniciado sesión: el comportamiento de siempre (401 -> login) sí es el correcto.
                base.HandleUnauthorizedRequest(filterContext);
                return;
            }

            var vista = new ViewResult { ViewName = "NoAutorizado" };
            vista.ViewBag.PermisoRequerido = _permisoRequerido;

            filterContext.Result = vista;

            contexto.Response.StatusCode = 403;

            // Sin esto IIS se queda con la respuesta y sirve SU pantalla de error
            // genérica en vez de la vista de arriba.
            contexto.Response.TrySkipIisCustomErrors = true;
        }
    }
}
