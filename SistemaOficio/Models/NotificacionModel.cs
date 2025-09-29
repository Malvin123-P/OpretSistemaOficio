namespace OfiGest.Models
{
    public class NotificacionModel
    {
        public int Id { get; set; }
        public int OficioId { get; set; }
        public int UsuarioId { get; set; }
        public string TipoOficio { get; set; } = string.Empty;
        public string DepartamentoRemitente { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }

    }
}
