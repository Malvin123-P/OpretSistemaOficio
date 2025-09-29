namespace OfiGest.Entities
{
    public class Notificaciones
    {
        public int Id { get; set; }
        public int OficioId { get; set; }
        public int UsuarioId { get; set; }
        public string TipoOficio { get; set; } = string.Empty;
        public string DepartamentoRemitente { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public bool EsLeida { get; set; }
        public virtual Oficio Oficio { get; set; } = null!;
        public virtual Usuario Usuario { get; set; } = null!;
    }
}
