namespace OfiGest.Entities
{
    public class LogOficio
    {
        public int Id { get; set; }
        public int OficioId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public int UsuarioAccionId { get; set; }
        public string TipoAccion { get; set; } = string.Empty;

        public Oficio? Oficio { get; set; }
        public Usuario? UsuarioAccion { get; set; }
    }
}