using System.Collections.Generic;

namespace InventarioApp.Models
{
    /// <summary>
    /// Pantalla de detalle de un activo: la ficha más su historial.
    /// </summary>
    public class DetalleActivoViewModel
    {
        //  Mismo criterio que DetalleItemViewModel: cuando una pantalla combina
        //  datos de dos fuentes, se arma un ViewModel en vez de forzar a Activo
        //  a cargar una lista de movimientos que no le corresponde.
        public Activo Activo { get; set; }
        public List<MovimientoActivo> Movimientos { get; set; }

        public DetalleActivoViewModel()
        {
            Activo = new Activo();
            Movimientos = new List<MovimientoActivo>();
        }
    }
}
