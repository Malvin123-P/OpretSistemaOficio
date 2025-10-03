using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;
using OfiGest.Helpers;
using OfiGest.Manegers;
using OfiGest.Models;
using OfiGest.Utilities;

namespace OfiGest.Managers
{
    public class OficioManager
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificacionManager _notificacionManager;
        private readonly PdfOficioManager pdfOficioManager;

        public OficioManager(ApplicationDbContext context, NotificacionManager notificacionManager, PdfOficioManager pdfOficioManager)
        {
            _context = context;
            _notificacionManager = notificacionManager;
            this.pdfOficioManager = pdfOficioManager;
        }

        public async Task<(bool Success, byte[]? PdfBytes, string? FileName)> CrearOficioAsync(OficioModel modelo, int usuarioId, bool generarPdf = true)
        {
            if (!await _context.TiposOficio.AnyAsync(t => t.Id == modelo.TipoOficioId))
                return (false, null, null);

            var usuario = await ObtenerUsuarioPorIdAsync(usuarioId);
            if (usuario?.Departamento == null || (usuario.DivisionId == null && !usuario.EsEncargadoDepartamental))
                return (false, null, null);

            string area = "TICS";
            string division = usuario.Division?.Iniciales ?? "00";
            string departamento = usuario.Departamento.Iniciales;

            var generador = new CodigoOficioGenerator(_context);
            var (codigoGenerado, _) = await generador.GenerarYActualizarAsync(area, departamento, division);

            if (await _context.Oficios.AnyAsync(o => o.Codigo == codigoGenerado))
                return (false, null, null);

            var oficio = new Oficio
            {
                Codigo = codigoGenerado,
                Contenido = modelo.Contenido?.Trim(),
                FechaCreacion = DateTime.UtcNow,
                Estado = true,
                TipoOficioId = modelo.TipoOficioId,
                DepartamentoId = usuario.DepartamentoId,
                UsuarioId = usuario.Id,
                Via = modelo.Via?.Trim(),
                Anexos = modelo.Anexos?.Trim(),
                DirigidoDepartamento = modelo.DirigidoDepartamento?.Trim() ?? string.Empty
                
            };

            _context.Oficios.Add(oficio);
            bool guardado = await _context.SaveChangesAsync() > 0;

            if (guardado)
            {
                await _context.Entry(oficio).Reference(o => o.TipoOficio).LoadAsync();
                await _context.Entry(oficio).Reference(o => o.DepartamentoRemitente).LoadAsync();

                var logger = new LogOficioHelper(_context);
                await logger.RegistrarAsync(oficio, usuario.Id, "Creación");

                await _notificacionManager.CrearNotificacionParaEncargadosAsync(
                    oficio.Id,
                    oficio.TipoOficio?.Nombre ?? "Oficio",
                    oficio.DepartamentoRemitente?.Nombre ?? "Departamento"
                );

              
                byte[]? pdfBytes = null;
                string? fileName = null;

                if (generarPdf)
                {
             
                    var encargadoDepartamental = await _context.Usuarios
                        .Where(u => u.DepartamentoId == usuario.DepartamentoId &&
                                   u.EsEncargadoDepartamental &&
                                   u.Activo)
                        .Select(u => $"{u.Nombre} {u.Apellido}".Trim())
                        .FirstOrDefaultAsync();

                    var pdfModel = new OficioPdfModel
                    {
                        Codigo = oficio.Codigo,
                        TipoOficio = oficio.TipoOficio.Nombre ?? "Oficio",
                        Contenido = oficio.Contenido ?? string.Empty,
                        Via = oficio.Via ?? string.Empty,
                        Anexos = oficio.Anexos ?? string.Empty,
                        DirigidoDepartamento = oficio.DirigidoDepartamento,
                        DepartamentoRemitente = oficio.DepartamentoRemitente?.Nombre ?? string.Empty,
                        UsuarioNombre = $"{usuario.Nombre} {usuario.Apellido}".Trim(),
                        EncargadoDepartamental = encargadoDepartamental ?? "Encargado Departamental", 
                        FechaCreacion = oficio.FechaCreacion
                    };

                    pdfBytes = pdfOficioManager.GenerarPdf(pdfModel);
                    fileName = pdfOficioManager.ObtenerNombreArchivo(pdfModel);
                }

                return (true, pdfBytes, fileName);
            }

            return (false, null, null);
        }

        public async Task<TipoOficioModel?> ObtenerTipoOficioPorIdAsync(int id)
        {
            var tipoOficio = await _context.TiposOficio
                .FirstOrDefaultAsync(t => t.Id == id);

            return tipoOficio == null ? null : new TipoOficioModel
            {
                Id = tipoOficio.Id,
                Nombre = tipoOficio.Nombre,
                Descripcion = tipoOficio.Descripcion,
            
            };
        }

        public async Task<bool> ActualizarOficioAsync(OficioModel modelo, int usuarioModificadorId)
        {
            var oficio = await _context.Oficios
                .Include(o => o.TipoOficio)
                .FirstOrDefaultAsync(o => o.Id == modelo.Id);

            if (oficio == null || !await _context.TiposOficio.AnyAsync(t => t.Id == modelo.TipoOficioId))
                return false;

            var contenidoNuevo = modelo.Contenido?.Trim();
            var viaNueva = modelo.Via?.Trim();
            var anexosNuevos = modelo.Anexos?.Trim();
            var motivoNuevo = modelo.MotivoModificacion?.Trim();
            var tipoNuevo = modelo.TipoOficioId;
            var estadoNuevo = modelo.Estado;
            var dirigidoDepartamentoNuevo = modelo.DirigidoDepartamento?.Trim();

            bool hayCambios =
                oficio.Contenido != contenidoNuevo ||
                oficio.Via != viaNueva ||
                oficio.Anexos != anexosNuevos ||
                oficio.TipoOficioId != tipoNuevo ||
                oficio.Estado != estadoNuevo ||
                oficio.MotivoModificacion != motivoNuevo ||
                oficio.DirigidoDepartamento != dirigidoDepartamentoNuevo;

            if (!hayCambios)
                return false;

            oficio.Contenido = contenidoNuevo;
            oficio.Via = viaNueva;
            oficio.Anexos = anexosNuevos;
            oficio.TipoOficioId = tipoNuevo;
            oficio.Estado = estadoNuevo;
            oficio.MotivoModificacion = motivoNuevo;
            oficio.DirigidoDepartamento = dirigidoDepartamentoNuevo;
            oficio.ModificadoEn = DateTime.UtcNow;
            oficio.ModificadoPorId = usuarioModificadorId;

            _context.Oficios.Update(oficio);
            bool actualizado = await _context.SaveChangesAsync() > 0;

            if (actualizado)
            {
            
                await _context.Entry(oficio).Reference(o => o.TipoOficio).LoadAsync();

                var logger = new LogOficioHelper(_context);
                await logger.RegistrarAsync(oficio, usuarioModificadorId, "Modificación");
            }

            return actualizado;
        }

        public async Task<OficioModel?> ObtenerPorIdAsync(int id)
        {
            var entidad = await _context.Oficios
                .Include(o => o.TipoOficio)
                .Include(o => o.DepartamentoRemitente)
                .Include(o => o.Usuario)
                .FirstOrDefaultAsync(o => o.Id == id);

            return entidad == null ? null : Proyectar(entidad);
        }

        public async Task<List<OficioModel>> ObtenerTodosAsync()
        {
            var oficios = await _context.Oficios
                .Include(o => o.TipoOficio)
                .Include(o => o.DepartamentoRemitente)
                .Include(o => o.Usuario)
                .OrderByDescending(o => o.FechaCreacion)
                .ToListAsync();

            return oficios.Select(Proyectar).ToList();
        }

        public async Task<List<OficioModel>> ObtenerActivosAsync()
        {
            var oficios = await _context.Oficios
                .Include(o => o.TipoOficio)
                .Include(o => o.DepartamentoRemitente)
                .Include(o => o.Usuario)
                .Where(o => o.Estado)
                .OrderByDescending(o => o.FechaCreacion)
                .ToListAsync();

            return oficios.Select(Proyectar).ToList();
        }

        public async Task<List<OficioModel>> ObtenerInactivosAsync()
        {
            var oficios = await _context.Oficios
                .Include(o => o.TipoOficio)
                .Include(o => o.DepartamentoRemitente)
                .Include(o => o.Usuario)
                .Where(o => !o.Estado)
                .OrderByDescending(o => o.FechaCreacion)
                .ToListAsync();

            return oficios.Select(Proyectar).ToList();
        }

        public async Task<Usuario?> ObtenerUsuarioPorIdAsync(int id)
        {
            return await _context.Usuarios
                .Include(u => u.Departamento)
                .Include(u => u.Division)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<TipoOficio>> ObtenerTipoOficioAsync()
        {
            return await _context.TiposOficio
                .OrderBy(t => t.Nombre)
                .ToListAsync();
        }

        public async Task<List<Departamento>> ObtenerDepartamentosAsync()
        {
            return await _context.Departamentos
                .OrderBy(d => d.Nombre)
                .ToListAsync();
        }

        private static OficioModel Proyectar(Oficio o) => new OficioModel
        {
            Id = o.Id,
            Codigo = o.Codigo,
            Contenido = o.Contenido,
            FechaCreacion = o.FechaCreacion,
            Estado = o.Estado,
            DepartamentoId = o.DepartamentoId,
            TipoOficioId = o.TipoOficioId,
            NombreTipoOficio = o.TipoOficio?.Nombre,
            NombreUsuario = o.Usuario?.Nombre,
            ApellidoUsuario = o.Usuario?.Apellido,
            DirigidoDepartamento = o.DirigidoDepartamento,
            Via = o.Via,
            Anexos = o.Anexos,
            ModificadoEn = o.ModificadoEn,
            ModificadoPorId = o.ModificadoPorId,
            MotivoModificacion = o.MotivoModificacion
        };
    }
}