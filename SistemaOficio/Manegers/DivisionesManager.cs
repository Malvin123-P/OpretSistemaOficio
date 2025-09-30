using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;
using OfiGest.Models;
using OfiGest.Utilities.GenerarIniciales;

namespace OfiGest.Manegers
{
    public class DivisionesManager
    {
        private readonly ApplicationDbContext _context;

        public DivisionesManager(ApplicationDbContext context)
        {
            _context = context;
        }

    
        public async Task<bool> CrearAsync(DivisionesModel model)
        {
            var inicialesGeneradas = InicialesDepartamentos.GenerarIniciales(model.Nombre);
            var inicialesFinal = string.IsNullOrWhiteSpace(model.Iniciales)
                ? inicialesGeneradas
                : model.Iniciales.Trim().ToUpper();

            var deptoExiste = await _context.Departamentos.AnyAsync(d => d.Id == model.DepartamentoId);
            if (!deptoExiste) return false;

            var entity = new Divisiones
            {
                Nombre = model.Nombre,
                DepartamentoId = model.DepartamentoId,
                Iniciales = inicialesFinal,
                Descripcion = model.Descripcion ?? "",
                FechaCreacion = DateTime.Now
            };

            _context.Divisiones.Add(entity);
            return await _context.SaveChangesAsync() > 0;
        }


        public async Task<bool> ActualizarAsync(DivisionesModel model)
        {
            var division = await _context.Divisiones.FindAsync(model.Id);
            if (division == null) return false;

            var nombreNuevo = model.Nombre.Trim();
            var inicialesNuevas = string.IsNullOrWhiteSpace(model.Iniciales)
                ? InicialesDepartamentos.GenerarIniciales(nombreNuevo)
                : model.Iniciales.Trim().ToUpper();
            var descripcionNueva = model.Descripcion?.Trim() ?? "";

            bool hayCambios =
                division.Nombre != nombreNuevo ||
                division.Iniciales != inicialesNuevas ||
                division.DepartamentoId != model.DepartamentoId ||
                (division.Descripcion ?? "") != descripcionNueva;

            if (!hayCambios)
                return false;

            division.Nombre = nombreNuevo;
            division.Iniciales = inicialesNuevas;
            division.DepartamentoId = model.DepartamentoId;
            division.Descripcion = descripcionNueva;

            _context.Divisiones.Update(division);
            return await _context.SaveChangesAsync() > 0;
        }
        public async Task<(bool eliminado, bool tieneUsuarios)> EliminarAsync(int id)
        {
            var division = await _context.Divisiones.FindAsync(id);
            if (division == null) return (false, false);

            bool tieneUsuarios = await _context.Usuarios.AnyAsync(u => u.DivisionId == id);
            if (tieneUsuarios) return (false, true);

            _context.Divisiones.Remove(division);
            await _context.SaveChangesAsync();
            return (true, false);
        }

  
        public async Task<DivisionesModel?> ObtenerPorIdAsync(int id)
        {
            var u = await _context.Divisiones
                .Include(x => x.Departamento)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (u == null) return null;

            return new DivisionesModel
            {
                Id = u.Id,
                Nombre = u.Nombre,
                DepartamentoId = u.DepartamentoId,
                Iniciales = u.Iniciales,
                NombreDepartamento = u.Departamento.Nombre,
                Descripcion = u.Descripcion
            };
        }

     
        public async Task<DivisionesModel?> ObtenerPorNombreAsync(string nombre)
        {
            var d = await _context.Divisiones
                .FirstOrDefaultAsync(dep => dep.Nombre.ToLower() == nombre.Trim().ToLower());

            if (d == null) return null;

            return new DivisionesModel
            {
                Id = d.Id,
                Nombre = d.Nombre,
                DepartamentoId = d.DepartamentoId,
                Iniciales = d.Iniciales,
                Descripcion = d.Descripcion
            };
        }

    
        public async Task<DivisionesModel?> ObtenerPorNombreYDepartamentosAsync(string nombre, int idDepartamento)
        {
            var nombreNormalizado = nombre.Trim().ToLower();

            var entidad = await _context.Divisiones
                .Include(u => u.Departamento)
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Nombre.ToLower() == nombreNormalizado &&
                    u.DepartamentoId == idDepartamento);

            if (entidad == null) return null;

            return new DivisionesModel
            {
                Id = entidad.Id,
                Nombre = entidad.Nombre,
                DepartamentoId = entidad.DepartamentoId,
                Iniciales = entidad.Iniciales,
                Descripcion = entidad.Descripcion
            };
        }


        public async Task<DivisionesModel?> ObtenerPorInicialesYDepartamentoAsync(string iniciales, int departamentoId)
        {
            var d = await _context.Divisiones
                .FirstOrDefaultAsync(d => d.Iniciales.ToLower() == iniciales.Trim().ToLower() &&
                                          d.DepartamentoId == departamentoId);

            if (d == null) return null;

            return new DivisionesModel
            {
                Id = d.Id,
                Nombre = d.Nombre,
                DepartamentoId = d.DepartamentoId,
                Iniciales = d.Iniciales,
                Descripcion = d.Descripcion
            };
        }

   
        public async Task<List<DivisionesModel>> ObtenerTodosAsync()
        {
            return await _context.Divisiones
                .Include(u => u.Departamento)
                .Select(d => new DivisionesModel
                {
                    Id = d.Id,
                    Nombre = d.Nombre,
                    DepartamentoId = d.DepartamentoId,
                    Iniciales = d.Iniciales,
                    NombreDepartamento = d.Departamento.Nombre,
                    Descripcion = d.Descripcion
                }).ToListAsync();
        }


        public async Task<List<Departamento>> ObtenerDepartamentosComboBoxAsync()
        {
            return await _context.Departamentos
                .OrderBy(d => d.Nombre)
                .ToListAsync();
        }

   
        public async Task<string?> ObtenerNombreDepartamentoAsync(int id)
        {
            return await _context.Departamentos
                .Where(d => d.Id == id)
                .Select(d => d.Nombre)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExisteInicialesEnDepartamentoAsync(string iniciales, int departamentoId, int excluirId)
        {
            return await _context.Divisiones
                .AnyAsync(d => d.Iniciales.ToLower() == iniciales.Trim().ToLower() &&
                               d.DepartamentoId == departamentoId &&
                               d.Id != excluirId);
        }
    }
}