using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;
using OfiGest.Models;

namespace OfiGest.Managers
{
    public class EstadisticaManager
    {
        private readonly ApplicationDbContext _context;

        public EstadisticaManager(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<TipoDatoResumen>> ObtenerResumenPorTipoAsync(int departamentoId)
        {
            var oficios = await _context.Oficios
                .Include(o => o.TipoOficio)
                .Where(o => o.DepartamentoId == departamentoId && o.Estado == true) 
                .ToListAsync();

            return ProcesarResumen(oficios);
        }

        public async Task<List<TipoDatoResumen>> ObtenerResumenPorTipoAsync()
        {
            var oficios = await _context.Oficios
                .Include(o => o.TipoOficio)
                .Where(o => o.Estado == true) 
                .ToListAsync();

            return ProcesarResumen(oficios);
        }

        private List<TipoDatoResumen> ProcesarResumen(List<Oficio> oficios)
        {
            var total = oficios.Count;

            return oficios
                .GroupBy(o => o.TipoOficio)
                .Select(g => new TipoDatoResumen
                {
                    Tipo = g.Key?.Id.ToString() ?? "0",
                    NombreTipo = g.Key?.Nombre ?? "Sin tipo",
                    Cantidad = g.Count(),
                    Porcentaje = total > 0 ? Math.Round((g.Count() / (decimal)total) * 100, 2) : 0
                })
                .OrderByDescending(r => r.Cantidad)
                .ToList();
        }
    }
}