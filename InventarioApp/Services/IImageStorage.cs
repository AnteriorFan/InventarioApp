using System;
using System.IO;
using System.Web;

namespace InventarioApp.Services
{
    public interface IImageStorage
    {
        string Guardar(HttpPostedFileBase archivo);
        string ObtenerUrl(string key);
    }

    public class LocalImageStorage : IImageStorage
    {
        private const string CarpetaRelativa = "~/Content/uploads/";

        public string Guardar(HttpPostedFileBase archivo)
        {
            if (archivo == null || archivo.ContentLength == 0)
                return null;

            var carpetaFisica = HttpContext.Current.Server.MapPath(CarpetaRelativa);
            if (!Directory.Exists(carpetaFisica))
                Directory.CreateDirectory(carpetaFisica);

            var extension = Path.GetExtension(archivo.FileName);
            var nombreUnico = Guid.NewGuid().ToString() + extension;

            archivo.SaveAs(Path.Combine(carpetaFisica, nombreUnico));

            return nombreUnico; // esto es lo que se guarda en items.imagen_s3_key
        }

        public string ObtenerUrl(string key)
        {
            return string.IsNullOrEmpty(key) ? null : VirtualPathUtility.ToAbsolute(CarpetaRelativa + key);
        }
    }
}
