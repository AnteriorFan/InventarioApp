using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Models
{

        public class Marca
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }

        /// <summary>
        /// Las 2-4 letras con las que esta marca aparece en el código
        /// automático de los activos. Tiene un índice UNIQUE en la base:
        /// dos marcas no pueden compartirla.
        /// </summary>
        public string Abreviatura { get; set; }
        }
}

