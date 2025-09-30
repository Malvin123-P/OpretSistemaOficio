using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;
using OfiGest.Models;

namespace OfiGest.Manegers
{
    public class TipoOficioManenger
    {
        private readonly ApplicationDbContext _context;

        public TipoOficioManenger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CrearAsync(TipoOficioModel model, ModelStateDictionary modelState)
        {
          
            var entity = new TipoOficio
            {
                Nombre = model.Nombre,
                Iniciales = model.Iniciales,
                Descripcion = model.Descripcion,
                FechaCreacion = DateTime.Now
            };

            _context.TiposOficio.Add(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<TipoOficioModel>> ObtenerTodosAsync()
        {
            return await _context.TiposOficio
                .Select(t => new TipoOficioModel
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    Iniciales = t.Iniciales,
                    Descripcion = t.Descripcion
                  
                })
                .ToListAsync();
        }

        public async Task<TipoOficioModel?> ObtenerPorIdAsync(int id)
        {
            var t = await _context.TiposOficio.FindAsync(id);
            if (t == null) return null;

            return new TipoOficioModel
            {
                Id = t.Id,
                Nombre = t.Nombre,
                Iniciales = t.Iniciales,
                Descripcion = t.Descripcion,
 
            };
        }

        public async Task<bool> ActualizarAsync(TipoOficioModel model)
        {
            var entity = await _context.TiposOficio.FindAsync(model.Id);
            if (entity == null) return false;

            var nombreNuevo = model.Nombre.Trim();
            var inicialesNuevas = model.Iniciales.Trim().ToUpper();
            var descripcionNueva = model.Descripcion?.Trim() ?? string.Empty;

            bool hayCambios =
                !string.Equals(entity.Nombre, nombreNuevo) ||
                !string.Equals(entity.Iniciales, inicialesNuevas) ||
                !string.Equals(entity.Descripcion ?? string.Empty, descripcionNueva);
           

            if (!hayCambios) return false;

            entity.Nombre = nombreNuevo;
            entity.Iniciales = inicialesNuevas;
            entity.Descripcion = descripcionNueva;

            _context.TiposOficio.Update(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<(bool eliminado, bool tieneOficios)> EliminarAsync(int id)
        {
            var entity = await _context.TiposOficio.FindAsync(id);
            if (entity == null) return (false, false);

            bool tieneOficios = await _context.Oficios.AnyAsync(o => o.TipoOficioId == id);
            if (tieneOficios)
                return (false, true);

            _context.TiposOficio.Remove(entity);
            await _context.SaveChangesAsync();
            return (true, false);
        }

        public async Task<TipoOficioModel?> ObtenerPorNombreAsync(string nombre)
        {
            var d = await _context.TiposOficio
                .FirstOrDefaultAsync(dep => dep.Nombre.ToLower() == nombre.ToLower());

            if (d == null) return null;

            return new TipoOficioModel
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Iniciales = d.Iniciales,
                Descripcion = d.Descripcion
          
            };
        }

        public async Task<TipoOficioModel?> ObtenerPorCodigoAsync(string codigo)
        {
            var d = await _context.TiposOficio
                .FirstOrDefaultAsync(dep => dep.Iniciales.ToLower() == codigo.ToLower());

            if (d == null) return null;

            return new TipoOficioModel
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Iniciales = d.Iniciales,
                Descripcion = d.Descripcion
                
            };
        }


        public async Task<bool> ExisteTipoOficioCompuestoAsync(string nombre, string iniciales)
        {
            return await _context.TiposOficio
                .AnyAsync(t => t.Nombre.ToLower() == nombre.Trim().ToLower() &&
                               t.Iniciales.ToLower() == iniciales.Trim().ToLower());
        }

        public async Task<bool> ExisteInicialesAsync(string iniciales, int excluirId)
        {
            return await _context.Departamentos
                .AnyAsync(d => d.Iniciales.ToLower() == iniciales.Trim().ToLower() &&
                               d.Id != excluirId);
        }
    }
}
