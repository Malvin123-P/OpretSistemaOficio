
namespace OfiGest.Models
{
    public class OficioPdfModel
    {
        public string? Codigo { get; set; }
        public string? TipoOficio { get; set; }
        public string? Contenido { get; set; }
        public string? Via { get; set; }
        public string? Anexos { get; set; }
        public string? DirigidoDepartamento { get; set; }
        public string? DepartamentoRemitente { get; set; }
        public string? UsuarioNombre { get; set; } 
        public string? EncargadoDepartamental { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}