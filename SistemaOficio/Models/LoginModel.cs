using System.ComponentModel.DataAnnotations;

namespace OfiGest.Models
{
    public class LoginModel : IValidatableObject
    {

        [Display(Name = "Correo Institucional")]
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(125, ErrorMessage = "El correo no puede exceder los 150 caracteres.")]
        public string Correo { get; set; }

        [Display(Name = "Contraseña")]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Contraseña { get; set; }
        public bool Recuerdame { get; set; }
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
}