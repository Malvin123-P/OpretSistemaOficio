using System;
using System.ComponentModel.DataAnnotations;

namespace OfiGest.Entities
{
    public class ContadorLocalOficio
    {
        public int Id { get; set; }
        public string Area { get; set; }
        public string Departamento { get; set; }
        public string Division { get; set; }
        public int UltimoNumero { get; set; }
        public DateTime UltimaActualizacion { get; set; }
    }
}