using System;
using System.Linq;

namespace InventarioApp.Helpers
{
    /// <summary>
    /// Traduce una excepción de Oracle a un mensaje que se le pueda enseñar
    /// a quien está usando la aplicación.
    /// </summary>
    public static class ErrorOracle
    {
        /// <param name="ex">La excepción tal cual la lanzó la capa de datos.</param>
        /// <param name="mensajePorDefecto">Qué decir cuando el error no es de negocio.</param>
        /// <param name="mensajeDuplicado">
        /// Qué decir ante un ORA-00001 (violación de UNIQUE). Cada pantalla sabe
        /// cuál es su campo único, así que el texto lo pone quien llama.
        /// </param>
        public static string Traducir(Exception ex, string mensajePorDefecto, string mensajeDuplicado = null)
        {
            //  Se recorre la cadena de InnerException porque Entity Framework
            //  envuelve la OracleException: el mensaje de más arriba suele ser
            //  un genérico de "error al ejecutar el comando".
            for (var actual = ex; actual != null; actual = actual.InnerException)
            {
                // ORA-00001 = se repitió un valor en una columna UNIQUE.
                if (actual.Message.Contains("ORA-00001"))
                    return mensajeDuplicado ?? "Ya existe un registro con ese valor.";

                //  ORA-20000 a ORA-20999 es el rango de RAISE_APPLICATION_ERROR:
                //  son los mensajes que escribimos nosotros en los packages, ya
                //  redactados para el usuario final.
                if (actual.Message.Contains("ORA-2"))
                {
                    string primeraLinea = actual.Message
                        .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(primeraLinea)) break;

                    // Quitar el prefijo "ORA-20010: " y quedarse con el texto.
                    int dosPuntos = primeraLinea.IndexOf(':');
                    return dosPuntos > 0 && primeraLinea.TrimStart().StartsWith("ORA-")
                        ? primeraLinea.Substring(dosPuntos + 1).Trim()
                        : primeraLinea;
                }
            }

            //  Cualquier otra cosa (red caída, tablespace lleno) NO se muestra:
            //  al usuario no le sirve un stack de Oracle, y a un atacante sí.
            return mensajePorDefecto;
        }
    }
}
