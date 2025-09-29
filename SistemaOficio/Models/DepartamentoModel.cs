using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OfiGest.Models
{
    public class DepartamentoModel
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre del departamento es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; }

        [Display(Name = "Iniciales")]
        [Required(ErrorMessage = "Las iniciales son obligatorias.")]
        [StringLength(10, ErrorMessage = "Las iniciales no pueden exceder los 10 caracteres.")]
        public string Iniciales { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
        public string? Descripcion { get; set; }

        [Display(Name = "Encargado Departamental")]
        public string? NombreEncargado { get; set; }

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; }

        public List<DivisionesModel> Divisiones { get; set; } = new();
    }
}