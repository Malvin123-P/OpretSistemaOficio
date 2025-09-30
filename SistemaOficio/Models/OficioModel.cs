using System.ComponentModel.DataAnnotations;

namespace OfiGest.Models
{
    public class OficioModel
    {
        [Key]
        [Display(Name = "ID del oficio")]
        public int Id { get; set; }

        [Required(ErrorMessage = "El código institucional es obligatorio.")]
        [StringLength(50)]
        [Display(Name = "Código institucional")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El contenido es obligatorio.")]
        [StringLength(5000)]
        [Display(Name = "Contenido del oficio")]
        public string Contenido { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; }

        [Required(ErrorMessage = "El tipo de oficio es obligatorio.")]
        [Display(Name = "Asunto")]
        public int TipoOficioId { get; set; }

        [Display(Name = "Tipo de oficio")]
        public string? NombreTipoOficio { get; set; }

        [Required(ErrorMessage = "El departamento remitente es obligatorio.")]
        [Display(Name = "Departamento remitente")]
        public int DepartamentoId { get; set; }

        [Display(Name = "Departamento remitente")]
        public string? NombreDepartamento { get; set; }

        public int UsuarioId { get; set; }
        [Required(ErrorMessage = "El usuario creador es obligatorio.")]
        [Display(Name = "Usuario creador")]
        public string? NombreUsuario { get; set; }

        [Display(Name = "Apellido del usuario")]
        public string? ApellidoUsuario { get; set; }

        [Display(Name = "Fecha de modificación")]
        public DateTime? ModificadoEn { get; set; }

        [StringLength(100)]
        [Display(Name = "Vía de comunicación")]
        public string? Via { get; set; }

        [StringLength(500)]
        [Display(Name = "Enlace de anexos")]
        public string? Anexos { get; set; }

        [Required]
        [Display(Name = "Estado activo")]
        public bool Estado { get; set; } = true;

        [Display(Name = "ID del modificador")]
        public int? ModificadoPorId { get; set; }

        [StringLength(500)]
        [Display(Name = "Motivo de modificación")]
        public string? MotivoModificacion { get; set; }

        [Required(ErrorMessage = "El campo 'Departamento dirigido' es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Departamento dirigido")]
        public string DirigidoDepartamento { get; set; } = string.Empty;

      
    }
}