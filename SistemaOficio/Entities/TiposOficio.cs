namespace OfiGest.Entities
{
    public class TipoOficio
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; }

        public ICollection<Oficio> Oficios { get; set; }
    }
}
