using System.ComponentModel.DataAnnotations;

namespace OfiGest.Models
{
    public class UsuarioModel 
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        [Required(ErrorMessage = "El nombre del usuario es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Apellidos")]
        [Required(ErrorMessage = "El apellido es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido no puede exceder los 100 caracteres.")]
        public string Apellido { get; set; } = string.Empty;

        [Display(Name = "Correo Institucional")]
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(150, ErrorMessage = "El correo no puede exceder los 150 caracteres.")]
        public string Correo { get; set; } = string.Empty;

        [Display(Name = "Contraseña")]
        [StringLength(255, ErrorMessage = "La contraseña no puede exceder los 255 caracteres.")]
        public string? Contraseña { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Último acceso")]
        public DateTime? UltimoAcceso { get; set; }

        [Display(Name = "Departamento")]
        [Required(ErrorMessage = "El departamento es obligatorio.")]
        public int DepartamentoId { get; set; }
        public string? NombreDepartamento { get; set; }

        [Display(Name = "Rol")]
        [Required(ErrorMessage = "El rol es obligatorio.")]
        public int RolId { get; set; }
        public string? NombreRol { get; set; }

        [Display(Name = "División")]
   
        public int? DivisionId { get; set; }
        public string? NombreDivision { get; set; }

        [Display(Name = "Encargado Departamental")]
        public bool EsEncargadoDepartamental { get; set; }

        [Display(Name = "Requiere restablecer contraseña")]
        public bool RequiereRestablecer { get; set; }

        public bool EsEdicion { get; set; } = false;

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; }

     
        [Display(Name = "Imagen de Perfil")]
        public string? ImagenPerfil { get; set; }

        [Display(Name = "Subir Imagen")]
        [DataType(DataType.Upload)]
        public IFormFile? ArchivoImagen { get; set; }

    }
}