using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Models
{
    public class Area
    {
        public int Id { get; set; }
        public int IdEdificio { get; set; }
        public string NombreEdificio { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
    }
}
