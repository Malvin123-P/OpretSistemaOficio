using System.ComponentModel.DataAnnotations;

namespace OfiGest.Entities
{
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Correo { get; set; } 
        public string? Contraseña { get; set; }
        public bool Activo { get; set; }
        public bool EsEncargadoDepartamental { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public int DepartamentoId { get; set; }
        public Departamento Departamento { get; set; } 
        public int RolId { get; set; }
        public Rol Rol { get; set; }
        public ICollection<Oficio> Oficios { get; set; } 
        public bool RequiereRestablecer { get; set; }
        public string? Token { get; set; }
        public DateTime? TokenExpira { get; set; }

        public int? DivisionId { get; set; } 
        public Divisiones Division { get; set; }

        public DateTime FechaCreacion { get; set; }
        public string? ImagenPerfil { get; set; }
    }
}