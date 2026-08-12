using System.Web;
using System.Web.Optimization;

namespace InventarioApp
{
    public class BundleConfig
    {
        // Para obtener más información sobre las uniones, visite https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Utilice la versión de desarrollo de Modernizr para desarrollar y obtener información sobre los formularios.  De esta manera estará
            // para la producción, use la herramienta de compilación disponible en https://modernizr.com para seleccionar solo las pruebas que necesite.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            //  bootstrap.BUNDLE.js, no bootstrap.js.
            //
            //  Bootstrap 5 se distribuye en dos versiones de JavaScript:
            //
            //    bootstrap.js         -> solo el código de Bootstrap.
            //    bootstrap.bundle.js  -> lo mismo MÁS Popper incrustado.
            //
            //  Popper es la librería que calcula dónde colocar un elemento
            //  flotante para que no se salga de la pantalla. Los componentes
            //  que se despliegan sobre el resto de la página la necesitan:
            //  dropdown, tooltip y popover. Los demás (modal, alert, collapse,
            //  tabs) funcionan sin ella, y por eso el error no aparecía hasta
            //  hacer clic en un menú desplegable:
            //
            //      Uncaught TypeError: Popper__namespace.createPopper is not a function
            //
            //  Bootstrap está cargado y "casi todo" funciona, así que el
            //  instinto es buscar el bug en el HTML del dropdown. No está ahí:
            //  falta una dependencia que este archivo nunca incluyó.
            bundles.Add(new Bundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.bundle.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));
        }
    }
}
