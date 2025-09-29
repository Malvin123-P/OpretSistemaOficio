namespace OfiGest.Entities
{
    public class Divisiones
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Iniciales { get; set; }
        public string? Descripcion { get; set; }
        public int DepartamentoId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public Departamento Departamento { get; set; }
    }
}
