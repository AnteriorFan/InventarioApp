using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int? IdCategoria { get; set; }
        public string NombreCategoria { get; set; }
        public int Cantidad { get; set; }

        public string UnidadMedida { get; set; }
        public string Ubicacion { get; set; }
        public string ImageS3Key { get; set; }
    }
}