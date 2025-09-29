namespace OfiGest.Models
{
    public class TipoDatoResumen
    {
        public string Tipo { get; set; } = string.Empty;
        public string NombreTipo { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Porcentaje { get; set; }
    }
}