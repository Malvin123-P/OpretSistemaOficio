using System.ComponentModel.DataAnnotations;

namespace OfiGest.Models
{
    public class DivisionesModel
    {
        [Key]
        public int Id { get; set; }
        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre de la division es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no debe exceder los 100 caracteres.")]
        public string Nombre { get; set; }

        [Display(Name = "Iniciales")]
        [Required(ErrorMessage = "Las iniciales son obligatorias.")]
        [StringLength(10, ErrorMessage = "Las iniciales no pueden exceder los 10 caracteres.")]
        public string Iniciales { get; set; } = string.Empty;

        [Display(Name = "Pertenece al Departamento")]
        [Required(ErrorMessage = "Debe seleccionar un departamento.")]
        public int DepartamentoId { get; set; }
        public string? NombreDepartamento { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La Descripción no puede exceder los 500 caracteres.")]
        public string? Descripcion { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; }

    }
}
