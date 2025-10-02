using System.ComponentModel.DataAnnotations;

namespace OfiGest.Models
{
    public class SolicitudRestablecimientoViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(150, ErrorMessage = "El correo no puede exceder los 150 caracteres.")]
        public string Correo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var emailValido = new EmailAddressAttribute().IsValid(Correo);
            if (emailValido)
            {

                var dominiosPermitidos = Environment.GetEnvironmentVariable("SeguridadCorreo_DominiosPermitidos").Split(',');
                if (dominiosPermitidos != null &&
                    !dominiosPermitidos.Any(d => Correo.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return new ValidationResult(
                        "El correo debe pertenecer a un dominio institucional válido.",
                        new[] { nameof(Correo) });
                }
            }
        }
    }

    public class RestablecerClaveViewModel
    {
        public string Correo { get; set; }
        public string Token { get; set; }

        [Required(ErrorMessage = "La Contraseña es obligatorio.")]
        [DataType(DataType.Password)]
        public string NuevaContraseña { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria.")]
        [Compare("NuevaContraseña", ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        public string ConfirmarContraseña { get; set; }
    }
  
}
