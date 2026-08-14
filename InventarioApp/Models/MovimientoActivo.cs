using System;
using System.Collections.Generic;

namespace InventarioApp.Models
{
    /// <summary>
    /// Un renglón del historial de un activo: qué le pasó, cuándo y quién lo hizo.
    /// </summary>
    public class MovimientoActivo
    {
        public int Id { get; set; }
        public int IdActivo { get; set; }
        public string TipoMovimiento { get; set; }

        //  El cursor devuelve los NOMBRES ya resueltos, no los ids: esta clase
        //  solo se usa para MOSTRAR el historial, nunca para escribirlo. Lo que
        //  se escribe va por RegistrarMovimientoViewModel, que sí lleva ids.
        public string UbicacionOrigen { get; set; }
        public string UbicacionDestino { get; set; }
        public string ResponsableAnterior { get; set; }
        public string ResponsableNuevo { get; set; }
        public string EstadoAnterior { get; set; }
        public string EstadoNuevo { get; set; }

        public string Motivo { get; set; }
        public string Observaciones { get; set; }
        public string ImagenKey { get; set; }
        public string RealizadoPor { get; set; }
        public DateTime Fecha { get; set; }

        // Solo lo llena sp_listar_recientes (la bitácora global).
        public string CodigoActivo { get; set; }
        public string NombreActivo { get; set; }

        public bool TieneImagen
        {
            get { return !string.IsNullOrEmpty(ImagenKey); }
        }

        /// <summary>
        /// Resumen en una línea de lo que cambió, para la columna principal
        /// de la tabla. Se arma aquí y no en la vista porque es lógica de
        /// presentación con varias ramas, y en Razor quedaría ilegible.
        /// </summary>
        public string Resumen
        {
            get
            {
                var partes = new List<string>();

                if (UbicacionOrigen != UbicacionDestino)
                    partes.Add(Texto(UbicacionOrigen) + " → " + Texto(UbicacionDestino));

                if (ResponsableAnterior != ResponsableNuevo)
                    partes.Add(Texto(ResponsableAnterior) + " → " + Texto(ResponsableNuevo));

                if (EstadoAnterior != EstadoNuevo)
                    partes.Add(Texto(EstadoAnterior) + " → " + Texto(EstadoNuevo));

                // Un movimiento puede no cambiar nada (por ejemplo una nota).
                return partes.Count == 0 ? "Sin cambios de ubicación, responsable ni estado"
                                         : string.Join(" · ", partes);
            }
        }

        private static string Texto(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "sin asignar" : valor.Trim();
        }
    }
}
