using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Models;

namespace OfiGest.Manegers
{
    public class RolManeger
    {
        private readonly ApplicationDbContext _context;

        public RolManeger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RolModel>> ObtenerTodosAsync()
        {
            return await _context.Roles
                .Select(r => new RolModel
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion!
                })
                .ToListAsync();
        }

        public async Task<RolModel?> ObtenerPorIdAsync(int id)
        {
            var r = await _context.Roles.FindAsync(id);
            if (r == null) return null;

            return new RolModel
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion!
            };
        }

        public async Task<RolModel?> ObtenerPorNombreAsync(string nombre)
        {
            var r = await _context.Roles
                .FirstOrDefaultAsync(rol => rol.Nombre.ToLower() == nombre.ToLower());

            if (r == null) return null;

            return new RolModel
            {
                Id = r.Id,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion!
            };
        }
    }
}