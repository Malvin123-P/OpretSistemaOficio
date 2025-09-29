using System.ComponentModel.DataAnnotations;
namespace OfiGest.Models
{

    public class RolModel
    {
        [Key]
        public int Id { get; set; }
        [Display(Name = " Nombre del Rol")]
        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios.")]
        public string Nombre { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(350)]
        public string? Descripcion { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; }
    }
}
