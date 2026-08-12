using System.Collections.Generic;

namespace InventarioApp.Models
{
    /// <summary>
    /// Lo que devuelve el escáner cuando lee un código, ya sea de un item o de
    /// un activo.
    /// </summary>
    public class ResultadoEscaneo
    {
        //  Esta clase existe para que el JavaScript del escáner NO tenga que
        //  saber la diferencia entre un item y un activo.
        //
        //  La alternativa era mandar el Item o el Activo crudos y que el JS
        //  tuviera dos rutinas distintas para pintar cada uno. Eso significa
        //  duplicar el HTML en el cliente y, cada vez que se agregue un campo,
        //  acordarse de tocar el JS. Aquí el servidor decide qué mostrar y el
        //  JS solo recorre una lista de etiqueta/valor.
        //
        //  De paso es más seguro: solo viaja al navegador lo que se va a
        //  enseñar, no el objeto completo.

        public const string TipoItem = "ITEM";
        public const string TipoActivo = "ACTIVO";
        public const string TipoNinguno = "NINGUNO";

        public string Tipo { get; set; }

        /// <summary>Etiqueta legible del tipo: "Item" / "Activo".</summary>
        public string TipoTexto { get; set; }

        public string Codigo { get; set; }
        public string Nombre { get; set; }

        /// <summary>A dónde lleva el botón "Abrir".</summary>
        public string Url { get; set; }

        /// <summary>Texto del badge de la esquina (existencia, o estado del activo).</summary>
        public string EstadoTexto { get; set; }

        /// <summary>Sufijo de la clase de Bootstrap: success, warning, danger, secondary.</summary>
        public string EstadoClase { get; set; }

        public List<DetalleEscaneo> Detalles { get; set; }

        public ResultadoEscaneo()
        {
            Detalles = new List<DetalleEscaneo>();
        }

        public static ResultadoEscaneo NoEncontrado(string codigo)
        {
            return new ResultadoEscaneo
            {
                Tipo = TipoNinguno,
                Codigo = codigo
            };
        }
    }

    public class DetalleEscaneo
    {
        public string Etiqueta { get; set; }
        public string Valor { get; set; }

        public DetalleEscaneo() { }

        public DetalleEscaneo(string etiqueta, string valor)
        {
            Etiqueta = etiqueta;
            // Un guion es más legible que un hueco en blanco en la ficha.
            Valor = string.IsNullOrWhiteSpace(valor) ? "—" : valor;
        }
    }
}
