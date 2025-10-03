using Microsoft.EntityFrameworkCore;
using OfiGest.Context;
using OfiGest.Entities;
using OfiGest.Manegers;
using OfiGest.Models;
using OfiGest.Utilities;


namespace OfiGest.Managers
{
    public class UsuarioManager
    {
        private readonly ApplicationDbContext _context;
        private readonly CorreoManager _correoManager;
        private readonly IWebHostEnvironment _environment;

        public UsuarioManager(ApplicationDbContext context, CorreoManager correoManager,IWebHostEnvironment environment)
        {
            _context = context;
            _correoManager = correoManager;
            _environment = environment;
        }

        public async Task<bool> CrearAsync(UsuarioModel model)
        {
            if (!await ValidarReferenciasAsync(model))
                return false;

            var correoNormalizado = model.Correo.Trim().ToLower();

            if (await _context.Usuarios.AnyAsync(u => u.Correo == correoNormalizado))
                return false;

            var entity = new Usuario
            {
                Nombre = model.Nombre.Trim(),
                Apellido = model.Apellido.Trim(),
                Correo = correoNormalizado,
                Contraseña = null,
                DepartamentoId = model.DepartamentoId,
                DivisionId = model.EsEncargadoDepartamental ? null : model.DivisionId,
                RolId = model.RolId,
                Activo = model.Activo,
                EsEncargadoDepartamental = model.EsEncargadoDepartamental,
                FechaCreacion = DateTime.Now,
                ImagenPerfil = null
            };

            _context.Usuarios.Add(entity);
            var guardado = await _context.SaveChangesAsync() > 0;

            if (guardado && model.ArchivoImagen != null)
            {
                try
                {
                    entity.ImagenPerfil = await GuardarImagenAsync(model.ArchivoImagen, entity.Id);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }

            if (guardado)
                _correoManager.EnviarActivacionCuenta(entity.Correo);

            return guardado;
        }

        public async Task<bool> ActualizarAsync(UsuarioModel model)
        {
            var usuario = await _context.Usuarios.FindAsync(model.Id);
            if (usuario == null) return false;

            var correoDuplicado = await _context.Usuarios
                .AnyAsync(u => u.Correo.ToLower() == model.Correo.Trim().ToLower() && u.Id != model.Id);
            if (correoDuplicado) return false;

            var nuevoNombre = model.Nombre.Trim();
            var nuevoApellido = model.Apellido.Trim();
            var nuevoCorreo = model.Correo.Trim().ToLower();
            var nuevaDivisionId = model.EsEncargadoDepartamental ? null : model.DivisionId;
            var nuevaContraseña = string.IsNullOrWhiteSpace(model.Contraseña) ? null : model.Contraseña.HashPassword();

            bool hayCambios =
                !string.Equals(usuario.Nombre, nuevoNombre) ||
                !string.Equals(usuario.Apellido, nuevoApellido) ||
                !string.Equals(usuario.Correo, nuevoCorreo) ||
                usuario.DepartamentoId != model.DepartamentoId ||
                usuario.DivisionId != nuevaDivisionId ||
                usuario.RolId != model.RolId ||
                usuario.Activo != model.Activo ||
                usuario.EsEncargadoDepartamental != model.EsEncargadoDepartamental ||
                (nuevaContraseña != null && !string.Equals(usuario.Contraseña, nuevaContraseña)) ||
                model.ArchivoImagen != null;

            if (!hayCambios) return false;

            string? rutaImagenAnterior = null;
            if (model.ArchivoImagen != null && model.ArchivoImagen.Length > 0)
            {
                rutaImagenAnterior = usuario.ImagenPerfil;
                try
                {
                    usuario.ImagenPerfil = await GuardarImagenAsync(model.ArchivoImagen, usuario.Id);
                    if (!string.IsNullOrEmpty(rutaImagenAnterior))
                        EliminarImagenAnterior(rutaImagenAnterior);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error al actualizar imagen: {ex.Message}");
                }
            }

            usuario.Nombre = nuevoNombre;
            usuario.Apellido = nuevoApellido;
            usuario.Correo = nuevoCorreo;
            usuario.DepartamentoId = model.DepartamentoId;
            usuario.DivisionId = nuevaDivisionId;
            usuario.RolId = model.RolId;
            usuario.Activo = model.Activo;
            usuario.EsEncargadoDepartamental = model.EsEncargadoDepartamental;

            if (nuevaContraseña != null)
                usuario.Contraseña = nuevaContraseña;

            _context.Usuarios.Update(usuario);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<List<UsuarioModel>> ObtenerTodosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.Departamento)
                .Include(u => u.Rol)
                .Include(u => u.Division)
                .Select(u => new UsuarioModel
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Correo = u.Correo,
                    DepartamentoId = u.DepartamentoId,
                    DivisionId = u.DivisionId,
                    RolId = u.RolId,
                    NombreDepartamento = u.Departamento!.Nombre,
                    NombreDivision = u.Division != null ? u.Division.Nombre : "Encargado",
                    NombreRol = u.Rol!.Nombre,
                    EsEncargadoDepartamental = u.EsEncargadoDepartamental,
                    Activo = u.Activo,
                    UltimoAcceso = u.UltimoAcceso,
                    RequiereRestablecer = u.RequiereRestablecer,
                    ImagenPerfil = u.ImagenPerfil,
                    FechaCreacion = u.FechaCreacion
                })
                .OrderBy(u => u.Nombre)
                .ThenBy(u => u.Apellido)
                .ToListAsync();
        }

        public async Task<UsuarioModel?> ObtenerPorIdAsync(int id)
        {
            return await _context.Usuarios
                .Include(u => u.Departamento)
                .Include(u => u.Rol)
                .Include(u => u.Division)
                .Where(u => u.Id == id)
                .Select(u => new UsuarioModel
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Correo = u.Correo,
                    DepartamentoId = u.DepartamentoId,
                    DivisionId = u.DivisionId,
                    RolId = u.RolId,
                    NombreDepartamento = u.Departamento!.Nombre,
                    NombreDivision = u.Division != null ? u.Division.Nombre : "Encargado",
                    NombreRol = u.Rol!.Nombre,
                    EsEncargadoDepartamental = u.EsEncargadoDepartamental,
                    Activo = u.Activo,
                    UltimoAcceso = u.UltimoAcceso,
                    RequiereRestablecer = u.RequiereRestablecer,
                    ImagenPerfil = u.ImagenPerfil,
                    FechaCreacion = u.FechaCreacion
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool eliminado, bool tieneOficios, bool tieneLogs, bool tieneNotificaciones)> EliminarAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return (false, false, false, false);

            // Verificar si tiene oficios asociados
            bool tieneOficios = await _context.Oficios.AnyAsync(o => o.UsuarioId == id);
            if (tieneOficios)
                return (false, true, false, false);

            // Verificar si tiene logs asociados
            bool tieneLogs = await _context.LogOficios.AnyAsync(l => l.UsuarioAccionId == id);
            if (tieneLogs)
                return (false, false, true, false);

            // Verificar si tiene notificaciones asociadas
            bool tieneNotificaciones = await _context.Notificaciones.AnyAsync(n => n.UsuarioId == id);
            if (tieneNotificaciones)
                return (false, false, false, true);

            // Eliminar imagen de perfil si existe
            if (!string.IsNullOrEmpty(usuario.ImagenPerfil))
            {
                EliminarImagenAnterior(usuario.ImagenPerfil);
            }

            // Eliminar el usuario
            _context.Usuarios.Remove(usuario);
            var guardado = await _context.SaveChangesAsync() > 0;
            return (guardado, false, false, false);
        }

        public async Task<List<Departamento>> ObtenerDepartamentosAsync()
        {
            return await _context.Departamentos
                .OrderBy(d => d.Nombre)
                .ToListAsync();
        }

        public async Task<List<Rol>> ObtenerRolesAsync()
        {
            return await _context.Roles
                .OrderBy(r => r.Nombre)
                .ToListAsync();
        }

        public async Task<List<Divisiones>> ObtenerDivisionesPorDepartamentoAsync(int departamentoId)
        {
            return await _context.Divisiones
                .Where(d => d.DepartamentoId == departamentoId)
                .OrderBy(d => d.Nombre)
                .ToListAsync();
        }

        public async Task<bool> ExisteEncargadoEnDepartamentoAsync(int departamentoId, int? usuarioIdExcluir = null)
        {
            var query = _context.Usuarios
                .Where(u => u.DepartamentoId == departamentoId && u.EsEncargadoDepartamental);

            if (usuarioIdExcluir.HasValue)
                query = query.Where(u => u.Id != usuarioIdExcluir.Value);

            return await query.AnyAsync();
        }

        private async Task<bool> ValidarReferenciasAsync(UsuarioModel model)
        {
            if (!await _context.Roles.AnyAsync(r => r.Id == model.RolId))
                return false;
            if (!await _context.Departamentos.AnyAsync(d => d.Id == model.DepartamentoId))
                return false;

            if (!model.EsEncargadoDepartamental && model.DivisionId.HasValue)
            {
                if (!await _context.Divisiones.AnyAsync(d => d.Id == model.DivisionId.Value))
                    return false;
            }

            return true;
        }

        public async Task<bool> ExisteNombreApellidoEnDepartamentoAsync(string nombre, string apellido, int departamentoId, int? usuarioIdExcluir = null)
        {
            var query = _context.Usuarios
                .Where(u =>
                    u.Nombre.Trim().ToLower() == nombre.Trim().ToLower() &&
                    u.Apellido.Trim().ToLower() == apellido.Trim().ToLower() &&
                    u.DepartamentoId == departamentoId);

            if (usuarioIdExcluir.HasValue)
                query = query.Where(u => u.Id != usuarioIdExcluir.Value);

            return await query.AnyAsync();
        }

        public async Task<bool> ExisteCorreoAsync(string correo, int? usuarioIdExcluir = null)
        {
            var query = _context.Usuarios
                .Where(u => u.Correo.ToLower() == correo.Trim().ToLower());

            if (usuarioIdExcluir.HasValue)
                query = query.Where(u => u.Id != usuarioIdExcluir.Value);

            return await query.AnyAsync();
        }

        private async Task<string?> GuardarImagenAsync(IFormFile? archivoImagen, int usuarioId)
        {
            if (archivoImagen == null || archivoImagen.Length == 0)
                return null;

            // Validar tipo de archivo
            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(archivoImagen.FileName).ToLower();
            if (!extensionesPermitidas.Contains(extension))
                throw new ArgumentException("Formato de archivo no permitido. Use JPG, JPEG, PNG o GIF.");

            // Validar tamaño (máximo 2MB)
            if (archivoImagen.Length > 2 * 1024 * 1024)
                throw new ArgumentException("La imagen no puede ser mayor a 2MB.");

            // Crear directorio si no existe
            var directorioImagenes = Path.Combine(_environment.WebRootPath, "images", "usuarios");
            if (!Directory.Exists(directorioImagenes))
                Directory.CreateDirectory(directorioImagenes);

            // Generar nombre único
            var nombreArchivo = $"usuario_{usuarioId}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var rutaCompleta = Path.Combine(directorioImagenes, nombreArchivo);

            // Guardar archivo
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivoImagen.CopyToAsync(stream);
            }

            return $"/images/usuarios/{nombreArchivo}";
        }

        private void EliminarImagenAnterior(string? rutaImagenAnterior)
        {
            if (string.IsNullOrEmpty(rutaImagenAnterior))
                return;

            try
            {
                var rutaFisicaAnterior = Path.Combine(_environment.WebRootPath,
                    rutaImagenAnterior.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (System.IO.File.Exists(rutaFisicaAnterior))
                {
                    System.IO.File.Delete(rutaFisicaAnterior);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar imagen anterior: {ex.Message}");
            }
        }

        public async Task<string?> ObtenerRutaImagenPorIdAsync(int usuarioId)
        {
            return await _context.Usuarios
                .Where(u => u.Id == usuarioId)
                .Select(u => u.ImagenPerfil)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> PuedeCambiarDepartamentoAsync(int usuarioId, int nuevoDepartamentoId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return false;

            // Si no hay cambio de departamento, se permite
            if (usuario.DepartamentoId == nuevoDepartamentoId)
                return true;

            // Si tiene oficios registrados, no puede cambiar
            bool tieneOficios = await _context.Oficios.AnyAsync(o => o.UsuarioId == usuarioId);
            return !tieneOficios;
        }
    }
}

