using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Models
{
    public class Categoria
    {

        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }

        /// <summary>
        /// Las 2-4 letras con las que esta categoría aparece en el código
        /// automático de los activos. Índice UNIQUE en la base.
        /// </summary>
        public string Abreviatura { get; set; }
    }
}