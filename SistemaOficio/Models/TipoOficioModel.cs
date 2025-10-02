using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OfiGest.Models
{
    public class TipoOficioModel
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        [RegularExpression(@"^[A-Za-zÁÉÍÓÚáéíóúÑñ0-9\s\-]+$", ErrorMessage = "El nombre solo puede contener letras, números, espacios y guiones.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Las iniciales son requeridas")]
        [StringLength(20, ErrorMessage = "Las iniciales no pueden exceder 20 caracteres")]
        [RegularExpression(@"^(OFI-|CERT-)[A-Z]+", ErrorMessage = "Las iniciales deben comenzar con OFI- o CERT- seguido de letras mayúsculas")]
        public string Iniciales { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
        public string? Descripcion { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; }
    }
}
