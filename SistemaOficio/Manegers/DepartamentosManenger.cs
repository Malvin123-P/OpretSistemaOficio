using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;
using OfiGest.Models;
using OfiGest.Utilities.GenerarIniciales;

namespace OfiGest.Manegers
{
    public class DepartamentosManenger
    {
        private readonly ApplicationDbContext dbContext;

        public DepartamentosManenger(ApplicationDbContext context)
        {
            dbContext = context;
        }

        public async Task<bool> CrearAsync(DepartamentoModel model)
        {
            var inicialesGeneradas = InicialesDepartamentos.GenerarIniciales(model.Nombre);

            var inicialesFinal = string.IsNullOrWhiteSpace(model.Iniciales)
                ? inicialesGeneradas
                : model.Iniciales.Trim().ToUpper();

            var entity = new Departamento
            {
                Nombre = model.Nombre,
                Iniciales = inicialesFinal,
                Descripcion = model.Descripcion ?? "",
                FechaCreacion = DateTime.Now
            };

            dbContext.Departamentos.Add(entity);
            return await dbContext.SaveChangesAsync() > 0;
        }

        public async Task<List<DepartamentoModel>> ObtenerTodosAsync()
        {
            var encargados = await dbContext.Usuarios
                .Where(u => u.EsEncargadoDepartamental)
                .Select(u => new { u.Nombre, u.Apellido, u.DepartamentoId })
                .ToListAsync();

            var departamentos = await dbContext.Departamentos
                .Include(d => d.Divisiones)
                .ToListAsync();

            var resultado = departamentos.Select(d =>
            {
                var encargado = encargados.FirstOrDefault(e => e.DepartamentoId == d.Id);

                return new DepartamentoModel
                {
                    Id = d.Id,
                    Nombre = d.Nombre,
                    Iniciales = d.Iniciales,
                    Descripcion = d.Descripcion,
                    NombreEncargado = encargado != null
                        ? $"{encargado.Nombre} {encargado.Apellido}"
                        : "Sin encargado",
                    Divisiones = d.Divisiones.Select(div => new DivisionesModel
                    {
                        Id = div.Id,
                        Nombre = div.Nombre,
                        Descripcion = div.Descripcion
                    }).ToList()
                };
            }).ToList();

            return resultado;
        }
        public async Task<DepartamentoModel?> ObtenerPorIdAsync(int id)
        {
            var d = await dbContext.Departamentos
                .Include(dep => dep.Divisiones)
                .FirstOrDefaultAsync(dep => dep.Id == id);

            if (d == null) return null;

            var encargado = await dbContext.Usuarios
                .Where(u => u.EsEncargadoDepartamental && u.DepartamentoId == id)
                .Select(u => new { u.Nombre, u.Apellido })
                .FirstOrDefaultAsync();

            return new DepartamentoModel
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Iniciales = d.Iniciales,
                Descripcion = d.Descripcion,
                NombreEncargado = encargado != null
                    ? $"{encargado.Nombre} {encargado.Apellido}"
                    : "Sin encargado",
                Divisiones = d.Divisiones.Select(div => new DivisionesModel
                {
                    Id = div.Id,
                    Nombre = div.Nombre,
                    Descripcion = div.Descripcion
                }).ToList()
            };
        }

        public async Task<DepartamentoModel?> ObtenerPorNombreAsync(string nombre)
        {
            var d = await dbContext.Departamentos
                .FirstOrDefaultAsync(dep => dep.Nombre.ToLower() == nombre.ToLower());

            if (d == null) return null;

            var encargado = await dbContext.Usuarios
                .Where(u => u.EsEncargadoDepartamental && u.DepartamentoId == d.Id)
                .Select(u => u.Nombre)
                .FirstOrDefaultAsync();

            return new DepartamentoModel
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Iniciales = d.Iniciales,
                Descripcion = d.Descripcion,
                NombreEncargado = encargado ?? "Sin encargado"
            };
        }

        public async Task<DepartamentoModel?> ObtenerPorInicialesAsync(string iniciales)
        {
            var d = await dbContext.Departamentos
                .FirstOrDefaultAsync(dep => dep.Iniciales.ToLower() == iniciales.ToLower());

            if (d == null) return null;

            var encargado = await dbContext.Usuarios
                .Where(u => u.EsEncargadoDepartamental && u.DepartamentoId == d.Id)
                .Select(u => u.Nombre)
                .FirstOrDefaultAsync();

            return new DepartamentoModel
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Iniciales = d.Iniciales,
                Descripcion = d.Descripcion,
                NombreEncargado = encargado ?? "Sin encargado"
            };
        }

        public async Task<bool> ActualizarAsync(DepartamentoModel model)
        {
            var departamento = await dbContext.Departamentos.FindAsync(model.Id);
            if (departamento == null) return false;

            var nombreNuevo = model.Nombre.Trim();
            var inicialesNuevas = string.IsNullOrWhiteSpace(model.Iniciales)
                ? InicialesDepartamentos.GenerarIniciales(nombreNuevo)
                : model.Iniciales.Trim().ToUpper();
            var descripcionNueva = model.Descripcion?.Trim() ?? "";

            bool hayCambios =
                departamento.Nombre != nombreNuevo ||
                departamento.Iniciales != inicialesNuevas ||
                (departamento.Descripcion ?? "") != descripcionNueva;

            if (!hayCambios)
                return false;

            departamento.Nombre = nombreNuevo;
            departamento.Iniciales = inicialesNuevas;
            departamento.Descripcion = descripcionNueva;

            dbContext.Departamentos.Update(departamento);
            return await dbContext.SaveChangesAsync() > 0;
        }

        public async Task<(bool eliminado, bool tieneUsuarios, bool tieneDivisiones)> EliminarAsync(int id)
        {
            var tieneUsuarios = await dbContext.Usuarios.AnyAsync(u => u.DepartamentoId == id);
            var tieneDivisiones = await dbContext.Divisiones.AnyAsync(d => d.DepartamentoId == id);

            if (tieneUsuarios || tieneDivisiones)
                return (false, tieneUsuarios, tieneDivisiones);

            var departamento = await dbContext.Departamentos.FindAsync(id);
            if (departamento == null)
                return (false, false, false);

            dbContext.Departamentos.Remove(departamento);
            await dbContext.SaveChangesAsync();

            return (true, false, false);
        }

        public async Task<bool> ExisteInicialesAsync(string iniciales, int excluirId)
        {
            return await dbContext.Departamentos
                .AnyAsync(d => d.Iniciales.ToLower() == iniciales.Trim().ToLower() &&
                               d.Id != excluirId);
        }
    }
}