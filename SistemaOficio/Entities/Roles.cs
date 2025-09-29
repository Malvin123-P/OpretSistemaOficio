namespace OfiGest.Entities
{
    public class Rol
    {
        public int Id { get; set; }
        public string Nombre { get; set; } 
        public string? Descripcion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public ICollection<Usuario> Usuarios { get; set; } 

    }
}
