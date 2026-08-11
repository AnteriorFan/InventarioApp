using System.Web;
using System.Web.Mvc;
using InventarioApp.Services;

namespace InventarioApp.Security
{
    public class AuthorizePermisoAttribute : AuthorizeAttribute
    {
        private readonly string _permisoRequerido;
        private readonly IPermisoService _permisoService;

        public AuthorizePermisoAttribute(string permiso)
        {
            _permisoRequerido = permiso;
            _permisoService = new PermisoService();
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
                return false;

            return _permisoService.UsuarioTienePermiso(httpContext.User.Identity.Name, _permisoRequerido);
        }
    }
}
