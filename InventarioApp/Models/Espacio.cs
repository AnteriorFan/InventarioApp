using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Models
{
    public class Espacio
    {
        public int Id { get; set; }
        public int IdEdificio { get; set; }
        public string NombreEdificio { get; set; }
        public int IdArea { get; set; }
        public string NombreArea { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
    }
}