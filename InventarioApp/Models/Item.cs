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

        // Punto de reorden: cuando Cantidad cae hasta aca, el item aparece en
        // "Reposicion urgente" del dashboard. Reemplaza al "< 10" que antes
        // estaba hardcodeado en la vista Items/Index.
        public int StockMinimo { get; set; }

        public string UnidadMedida { get; set; }
        public string Ubicacion { get; set; }
        public string ImagenS3Key { get; set; }
    }
}