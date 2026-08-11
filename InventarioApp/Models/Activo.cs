using System;

namespace InventarioApp.Models
{
    public class Activo
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int? IdCategoria { get; set; }
        public string NombreCategoria { get; set; }
        public int? IdMarca { get; set; }
        public string NombreMarca { get; set; }
        public int? IdModelo { get; set; }
        public string NombreModelo { get; set; }
        public string NumeroSerie { get; set; }
        public int? IdEstado { get; set; }
        public string NombreEstado { get; set; }
        public int? IdUbicacionOrigen { get; set; }
        public string NombreUbicacionOrigen { get; set; }
        public int? IdUbicacionActual { get; set; }
        public string NombreUbicacionActual { get; set; }
        public int? IdResponsable { get; set; }
        public string NombreResponsable { get; set; }
        public DateTime? FechaCompra { get; set; }
        public decimal? Costo { get; set; }
        public DateTime? GarantiaHasta { get; set; }
        public string Observaciones { get; set; }
    }
}
