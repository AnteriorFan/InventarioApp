using System.Web.Mvc;

namespace InventarioApp.Security
{
    /// <summary>
    /// Permite preguntar por un permiso desde cualquier vista:
    /// <c>@if (Html.TienePermiso("SEGURIDAD_ADMINISTRAR")) { ... }</c>
    /// </summary>
    public static class PermisoHtmlHelper
    {
        //  El navbar necesita saber qué secciones mostrar, y el navbar vive en
        //  _Layout: no tiene Controller propio ni ViewModel donde recibir
        //  banderas. Por eso acá sí se consulta el permiso desde la vista, a
        //  diferencia del resto del proyecto (donde se resuelven en el
        //  Controller y se pasan como bool en el ViewModel, como hace
        //  DashboardViewModel.PuedeVerBitacora).
        //
        //  IMPORTANTE: esto es solo para ESCONDER cosas de la interfaz. La
        //  seguridad real la sigue haciendo [AuthorizePermiso] en el Controller.
        //  Un menú oculto no protege nada: la URL se puede escribir a mano.
        public static bool TienePermiso(this HtmlHelper html, string permiso)
        {
            // El caché vive en PermisosDelRequest para que [AuthorizePermiso] y
            // las vistas compartan la MISMA lista: se resuelve una vez por
            // petición, sin importar cuántas veces se pregunte ni desde dónde.
            return PermisosDelRequest.Tiene(html.ViewContext.HttpContext, permiso);
        }
    }
}
