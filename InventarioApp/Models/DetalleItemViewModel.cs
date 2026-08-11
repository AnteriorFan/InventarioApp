using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace InventarioApp.Models
{
    public class DetalleItemViewModel
    {
        public Item Item { get; set; }
        public List<MovimIentosinventario> Movimientos { get; set; }
    }
}