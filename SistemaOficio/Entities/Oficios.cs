using System;

namespace OfiGest.Entities
{
    public class Oficio
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }

        public int TipoOficioId { get; set; }
        public TipoOficio TipoOficio { get; set; }

        public int DepartamentoId { get; set; }
        public Departamento DepartamentoRemitente { get; set; }

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        public DateTime? ModificadoEn { get; set; }
        public string? Via { get; set; }
        public string? Anexos { get; set; }

        public bool Estado { get; set; } = true;
        public int? ModificadoPorId { get; set; }
        public string? MotivoModificacion { get; set; }

        public string DirigidoDepartamento { get; set; } = string.Empty;
     
    }
}