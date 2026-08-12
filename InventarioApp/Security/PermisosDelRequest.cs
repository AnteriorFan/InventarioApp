using InventarioApp.Services;
using System.Collections.Generic;
using System.Web;

namespace InventarioApp.Security
{
    /// <summary>
    /// Resuelve los permisos del usuario una sola vez por petición.
    /// </summary>
    public static class PermisosDelRequest
    {
        //  Antes de esto, cada pregunta por un permiso costaba DOS consultas a
        //  Oracle (una para encontrar al usuario por su login, otra para sus
        //  permisos). Con [AuthorizePermiso] en la action MÁS los @Html.TienePermiso
        //  del navbar y de cada botón, una sola página llegaba a pedir lo mismo
        //  media docena de veces.
        //
        //  HttpContext.Items es exactamente el lugar para esto: es un diccionario
        //  que vive lo que dura UNA petición y se tira solo al terminar. No es
        //  Session (que dura toda la sesión y se quedaría con permisos viejos si
        //  alguien le cambia el rol al usuario mientras trabaja) ni Cache (que es
        //  global y habría que invalidar a mano).
        private const string Clave = "__permisos_del_request";

        public static List<string> Obtener(HttpContextBase contexto)
        {
            if (contexto == null || contexto.User == null || !contexto.User.Identity.IsAuthenticated)
                return new List<string>();

            var permisos = contexto.Items[Clave] as List<string>;

            if (permisos == null)
            {
                permisos = new PermisoService().ObtenerDeUsuario(contexto.User.Identity.Name);
                contexto.Items[Clave] = permisos;
            }

            return permisos;
        }

        public static bool Tiene(HttpContextBase contexto, string permiso)
        {
            return Obtener(contexto).Contains(permiso);
        }
    }
}
