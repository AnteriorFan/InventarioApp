using System.ComponentModel.DataAnnotations;

namespace InventarioApp.Models
{
    /// <summary>
    /// Formulario de "registrar movimiento" de un activo.
    /// </summary>
    public class RegistrarMovimientoViewModel
    {
        public int IdActivo { get; set; }

        [Required(ErrorMessage = "Elige qué tipo de movimiento es.")]
        [Display(Name = "Tipo de movimiento")]
        public int? IdTipoMovimiento { get; set; }

        //  Los tres son opcionales: cada movimiento cambia lo que le toca. Un
        //  traslado manda ubicación y deja los otros dos vacíos, y el procedure
        //  entiende "vacío = no cambia" (NVL contra el valor actual).
        [Display(Name = "Nueva ubicación")]
        public int? IdUbicacionDestino { get; set; }

        [Display(Name = "Nuevo responsable")]
        public int? IdResponsableNuevo { get; set; }

        [Display(Name = "Nuevo estado")]
        public int? IdEstadoNuevo { get; set; }

        //  Motivo e imagen NO llevan [Required] aquí, aunque para una Baja sean
        //  obligatorios: si fueran obligatorios siempre, no se podría registrar
        //  un traslado normal.
        //
        //  La regla depende del TIPO elegido y vive en tipos_movimiento
        //  (requiere_motivo / requiere_imagen). El Controller la comprueba
        //  leyendo esas banderas, y el procedure la vuelve a comprobar por si
        //  alguien llama la base por fuera.
        [StringLength(200, ErrorMessage = "El motivo no puede pasar de 200 caracteres.")]
        public string Motivo { get; set; }

        [StringLength(1000, ErrorMessage = "Las observaciones no pueden pasar de 1000 caracteres.")]
        public string Observaciones { get; set; }
    }
}
