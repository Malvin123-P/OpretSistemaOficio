using OfiGest.Context;
using OfiGest.Entities;

namespace OfiGest.Helpers
{
    public class LogOficioHelper
    {
        private readonly ApplicationDbContext _context;

        public LogOficioHelper(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAsync(Oficio oficio, int usuarioId, string tipoAccion)
        {
            var log = new LogOficio
            {
                OficioId = oficio.Id,
                Codigo = oficio.Codigo,
                Asunto = oficio.TipoOficio.Nombre,
                Contenido = oficio.Contenido,
                FechaRegistro = DateTime.UtcNow,
                UsuarioAccionId = usuarioId,
                TipoAccion = tipoAccion
            };

            _context.LogOficios.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}