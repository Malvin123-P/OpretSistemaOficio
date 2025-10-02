using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;
public class NotificacionManager
{
    private readonly ApplicationDbContext _context;

    public NotificacionManager(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notificaciones>> ObtenerNotificacionesAsync(int usuarioId, int top = 5)
    {
        try
        {
            return await _context.Notificaciones
                .Include(n => n.Oficio)
                    .ThenInclude(o => o.DepartamentoRemitente)
                .Where(n => n.UsuarioId == usuarioId && !n.EsLeida)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(top)
                .ToListAsync();
        }
        catch
        {
            return new List<Notificaciones>();
        }
    }

    public async Task<bool> MarcarComoLeidaAsync(int notificacionId, int usuarioId)
    {
        try
        {
            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == notificacionId && n.UsuarioId == usuarioId);

            if (notificacion != null && !notificacion.EsLeida)
            {
                notificacion.EsLeida = true;
                return await _context.SaveChangesAsync() > 0;
            }
            return notificacion != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> ObtenerCantidadNotificacionesPendientesAsync(int usuarioId)
    {
        try
        {
            return await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId && !n.EsLeida)
                .CountAsync();
        }
        catch
        {
            return 0;
        }
    }

  
    public async Task CrearNotificacionParaEncargadosAsync(int oficioId, string tipoOficio, string departamentoRemitente)
    {
        var oficio = await _context.Oficios.FindAsync(oficioId);
        if (oficio == null) return;

        var encargadosIds = await _context.Usuarios
            .Where(u => u.EsEncargadoDepartamental && u.Activo && u.Departamento.Nombre == oficio.DirigidoDepartamento)
            .Select(u => u.Id)
            .ToListAsync();

        if (!encargadosIds.Any()) return;

        var notificaciones = encargadosIds.Select(usuarioId => new Notificaciones
        {
            OficioId = oficioId,
            UsuarioId = usuarioId,
            TipoOficio = tipoOficio,
            FechaCreacion = DateTime.Now,
            EsLeida = false,
            DepartamentoRemitente = departamentoRemitente
        }).ToList();

        await _context.Notificaciones.AddRangeAsync(notificaciones);
        await _context.SaveChangesAsync();
    }
}