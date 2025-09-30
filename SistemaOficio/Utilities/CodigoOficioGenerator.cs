using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;

namespace OfiGest.Utilities
{
    public class CodigoOficioGenerator
    {
        private readonly ApplicationDbContext _context;

        public CodigoOficioGenerator(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(string Codigo, int Numero)> GenerarYActualizarAsync(string area, string departamento, string division)
        {
            area = area.Trim().ToUpper();
            departamento = departamento.Trim().ToUpper();
            division = division.Trim().ToUpper();

            var contador = await _context.ContadorLocalOficio
                .FirstOrDefaultAsync(c => c.Area == area && c.Departamento == departamento);

            int numeroActual = (contador?.UltimoNumero ?? 0) + 1;
            string secuencia = numeroActual.ToString("D4");
            string formatoFijo = "00";
            string fechaActual = DateTime.Now.ToString("yyyyMMdd");
            string codigo = $"{area}{departamento}{division}{formatoFijo}{fechaActual}{secuencia}";

            if (contador == null)
            {
                contador = new ContadorLocalOficio
                {
                    Area = area,
                    Departamento = departamento,
                    Division = division,
                    UltimoNumero = numeroActual,
                    UltimaActualizacion = DateTime.UtcNow
                };
                _context.ContadorLocalOficio.Add(contador);
            }
            else
            {
                contador.UltimoNumero = numeroActual;
                contador.UltimaActualizacion = DateTime.UtcNow;
                _context.ContadorLocalOficio.Update(contador);
            }

            await _context.SaveChangesAsync();
            return (codigo, numeroActual);
        }
    }
}