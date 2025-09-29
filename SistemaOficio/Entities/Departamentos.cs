namespace OfiGest.Entities
{
    public class Departamento
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public ICollection<Oficio> Oficios { get; set; } = new List<Oficio>();
        public ICollection<Divisiones> Divisiones { get; set; } = new List<Divisiones>();
    }
}
