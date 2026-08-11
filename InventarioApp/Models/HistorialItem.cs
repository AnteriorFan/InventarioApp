using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Models
{
    public class HistorialItem
    {
        public int Id { get; set; }
        public int IdItem { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; }
        public string Accion { get; set; }
        public DateTime Fecha { get; set; }
        public string Detalle { get; set; }
    }
}