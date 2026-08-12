using System.ComponentModel.DataAnnotations;

namespace InventarioApp.Models
{
    /// <summary>
    /// Formulario de alta de usuario.
    /// </summary>
    public class CrearUsuarioViewModel
    {
        //  Primer lugar del proyecto donde se usan DataAnnotations.
        //
        //  Hasta ahora la validación se hacía a mano dentro del Controller
        //  (if string.IsNullOrWhiteSpace...). Con atributos, la regla vive
        //  pegada al campo: ModelState.IsValid las aplica todas de una vez y
        //  @Html.ValidationMessageFor pinta el mensaje donde corresponde, sin
        //  que el Controller tenga que enumerarlas.
        //
        //  Es también donde más importa: un usuario mal dado de alta es alguien
        //  que no puede entrar, o peor, alguien con una contraseña débil.

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede pasar de 100 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido no puede pasar de 100 caracteres.")]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "El usuario para iniciar sesión es obligatorio.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Entre 3 y 50 caracteres.")]
        //  Sin espacios ni acentos: es lo que se teclea en el login, y un
        //  espacio invisible al final es de los errores más difíciles de ver.
        [RegularExpression("^[a-zA-Z0-9._-]+$",
            ErrorMessage = "Solo letras, números, punto, guion y guion bajo (sin espacios ni acentos).")]
        [Display(Name = "Usuario")]
        public string UsuarioLogin { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        //  Compare no comprueba la contraseña contra nada guardado: compara
        //  este campo contra la OTRA propiedad del mismo modelo. Sirve para
        //  atrapar el error de dedo al escribirla, que si no se descubre
        //  cuando la persona ya no puede entrar.
        [Required(ErrorMessage = "Confirma la contraseña.")]
        [Compare("Password", ErrorMessage = "Las dos contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarPassword { get; set; }

        //  Opcional a propósito: se puede dar de alta a alguien sin rol y
        //  decidirlo después. Sin rol simplemente no hereda ningún permiso.
        [Display(Name = "Rol")]
        public int? IdRol { get; set; }
    }
}
