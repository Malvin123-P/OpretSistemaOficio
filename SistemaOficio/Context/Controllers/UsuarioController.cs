using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfiGest.Entities;
using OfiGest.Managers;
using OfiGest.Models;

namespace OfiGest.Controllers
{
    [Authorize(Policy = "Administrador")]
    public class UsuarioController : Controller
    {
        private readonly UsuarioManager _usuarioManager;
        private readonly IWebHostEnvironment _environment;

        public UsuarioController(UsuarioManager usuarioManager, IWebHostEnvironment environment)
        {
            _usuarioManager = usuarioManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var rolUsuario = HttpContext.Session.GetString("RolUsuario");
                var departamentoIdStr = HttpContext.Session.GetString("DepartamentoId");

                List<UsuarioModel> usuarios;

                if (rolUsuario == "Administrador")
                {
                    usuarios = await _usuarioManager.ObtenerTodosAsync();
                }
                else if (int.TryParse(departamentoIdStr, out int departamentoId))
                {
                    usuarios = (await _usuarioManager.ObtenerTodosAsync())
                        .Where(u => u.DepartamentoId == departamentoId && u.Activo)
                        .ToList();
                }
                else
                {
                    TempData["Error"] = "No se pudo determinar el departamento del usuario.";
                    return RedirectToAction("Index", "Home");
                }

                return View(usuarios);
            }
            catch
            {
                TempData["Error"] = "Error al cargar los usuarios.";
                return RedirectToAction("Index", "Home");
            }
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View(new UsuarioModel());
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var usuario = await _usuarioManager.ObtenerPorIdAsync(id);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            usuario.EsEdicion = true;
            await CargarListasAsync(usuario.DepartamentoId, usuario.RolId, usuario.DivisionId, usuario.EsEncargadoDepartamental);
            return View(usuario);
        }
     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioModel model)
        {
            await CargarListasAsync(model.DepartamentoId, model.RolId, model.DivisionId, model.EsEncargadoDepartamental);

            if (model.EsEncargadoDepartamental)
                ModelState.Remove(nameof(model.DivisionId));

            if (model.ArchivoImagen != null)
            {
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(model.ArchivoImagen.FileName).ToLower();
                if (!extensionesPermitidas.Contains(extension))
                {
                         TempData["Validacion"] ="Formato de archivo no permitido. Use JPG, JPEG, PNG o GIF.";
                }

                if (model.ArchivoImagen.Length > 2 * 1024 * 1024)
                {
                    TempData["Validacion"] = "La imagen no puede ser mayor a 2MB.";
              
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            if (model.EsEncargadoDepartamental &&
                await _usuarioManager.ExisteEncargadoEnDepartamentoAsync(model.DepartamentoId))
            {
                TempData["Validacion"] = "Ya existe un encargado departamental en este departamento.";
                return View(model);
            }

            var dominiosPermitidos = Environment.GetEnvironmentVariable("SeguridadCorreo_DominiosPermitidos").Split(',');
            bool correoValido = dominiosPermitidos.Any(d => model.Correo.EndsWith(d, StringComparison.OrdinalIgnoreCase));

            if (!correoValido)
            {
                TempData["Validacion"] = $"El correo debe terminar en uno de los siguientes dominios permitidos: {string.Join(", ", dominiosPermitidos)}.";
                return View(model);
            }

            if (await _usuarioManager.ExisteCorreoAsync(model.Correo))
            {
                TempData["Validacion"] = "Ya existe un usuario con este correo institucional.";
                return View(model);
            }

            if (await _usuarioManager.ExisteNombreApellidoEnDepartamentoAsync(model.Nombre, model.Apellido, model.DepartamentoId))
            {
                TempData["Validacion"] = "Ya existe un usuario con el mismo nombre y apellido en este departamento.";
                return View(model);
            }


            var creado = await _usuarioManager.CrearAsync(model);

            if (creado)
            {
                TempData["Success"] = "Usuario creado correctamente.";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError(string.Empty, "No se pudo crear el usuario. Verifique referencias o correo duplicado.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UsuarioModel model)
        {
            var original = await _usuarioManager.ObtenerPorIdAsync(model.Id);

            model.EsEdicion = true;
            await CargarListasAsync(model.DepartamentoId, model.RolId, model.DivisionId, model.EsEncargadoDepartamental);

            if (original == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction("Index");
            }

            bool sinCambios =
              original.Nombre == model.Nombre &&
              original.Apellido == model.Apellido &&
              original.Correo == model.Correo &&
              original.DepartamentoId == model.DepartamentoId &&
              original.DivisionId == model.DivisionId &&
              original.RolId == model.RolId &&
              original.Activo == model.Activo &&
              original.EsEncargadoDepartamental == model.EsEncargadoDepartamental &&
              model.ArchivoImagen == null;

            if (sinCambios)
            {
                TempData["Warning"] = "No se detectaron cambios en el formulario. Realice alguna modificación antes de actualizar.";
                return RedirectToAction("Edit", new { id = model.Id });
            }

            if (model.ArchivoImagen != null && model.ArchivoImagen.Length > 0)
            {
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(model.ArchivoImagen.FileName).ToLower();
                if (!extensionesPermitidas.Contains(extension))
                {
                    TempData["Validacion"] = "La imagen debe ser JPG, JPEG, PNG o GIF.";
                    return View(model);
                }

                if (model.ArchivoImagen.Length > 2 * 1024 * 1024)
                {
                    TempData["Validacion"] = "La imagen no puede ser mayor a 2MB.";
                    return View(model);
                }
            }

            var dominiosPermitidos = Environment.GetEnvironmentVariable("SeguridadCorreo_DominiosPermitidos").Split(',');
            bool correoValido = dominiosPermitidos.Any(d => model.Correo.EndsWith(d, StringComparison.OrdinalIgnoreCase));

            if (!correoValido)
            {
                TempData["Validacion"] = $"El correo debe terminar en uno de los siguientes dominios permitidos: {string.Join(", ", dominiosPermitidos)}.";
                return View(model);
            }

            if (model.EsEncargadoDepartamental &&
                await _usuarioManager.ExisteEncargadoEnDepartamentoAsync(model.DepartamentoId, model.Id))
            {
                TempData["Validacion"] = "Ya existe otro encargado departamental en este departamento.";
                return View(model);
            }

            bool puedeCambiarDepartamento = await _usuarioManager.PuedeCambiarDepartamentoAsync(model.Id, model.DepartamentoId);
            if (!puedeCambiarDepartamento)
            {
                TempData["Validacion"] = "No puedes cambiar de departamento si ya tienes oficios registrados.";
                return View(model);
            }

            ModelState.Remove(nameof(model.DivisionId));
            ModelState.Remove(nameof(model.Contraseña));
            ModelState.Remove(nameof(model.NombreDepartamento));
            ModelState.Remove(nameof(model.NombreDivision));
            ModelState.Remove(nameof(model.NombreRol));

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var actualizado = await _usuarioManager.ActualizarAsync(model);
                TempData[actualizado ? "Success" : "Error"] = actualizado
                    ? "Usuario modificado correctamente."
                    : "No se detectaron cambios en el formulario. Realice alguna modificación antes de actualizar.";

                return RedirectToAction("Index");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocurrió un error inesperado al procesar la solicitud.";
                return View(model);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "ID inválido.";
                return RedirectToAction("Index");
            }

            var (eliminado, tieneOficios, tieneLogs, tieneNotificaciones) = await _usuarioManager.EliminarAsync(id);

            if (tieneOficios)
            {
                TempData["Error"] = "No se puede eliminar el usuario. Está asociado a oficios existentes.";
            }
            else if (tieneLogs)
            {
                TempData["Error"] = "No se puede eliminar el usuario. Está registrado en el historial de acciones.";
            }
            else if (tieneNotificaciones)
            {
                TempData["Error"] = "No se puede eliminar el usuario. Está asociado a notificaciones en el sistema.";
            }
            else
            {
                TempData[eliminado ? "Success" : "Error"] = eliminado
                    ? "Usuario eliminado correctamente."
                    : "No se pudo eliminar el usuario.";
            }

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<JsonResult> ObtenerDivisionesPorDepartamento(int departamentoId)
        {
            var divisiones = await _usuarioManager.ObtenerDivisionesPorDepartamentoAsync(departamentoId);
            return Json(divisiones.Select(d => new { d.Id, d.Nombre }));
        }

        private async Task CargarListasAsync(int? departamentoId = null, int? rolId = null, int? divisionId = null, bool esEncargadoDepartamental = false)
        {
            var departamentos = await _usuarioManager.ObtenerDepartamentosAsync();
            var roles = await _usuarioManager.ObtenerRolesAsync();

            var divisiones = new List<Divisiones>();
            if (departamentoId.HasValue)
            {
                divisiones = await _usuarioManager.ObtenerDivisionesPorDepartamentoAsync(departamentoId.Value);
            }

            ViewBag.Departamentos = new SelectList(departamentos, "Id", "Nombre", departamentoId);
            ViewBag.Roles = new SelectList(roles, "Id", "Nombre", rolId);
            ViewBag.Divisiones = new SelectList(divisiones, "Id", "Nombre", divisionId);
        }

        private int? ObtenerDepartamentoIdDeSesion()
        {
            var departamentoIdStr = HttpContext.Session.GetString("DepartamentoId");
            return int.TryParse(departamentoIdStr, out var departamentoId) ? departamentoId : null;
        }

        [HttpGet]
        public IActionResult DescargarImagen(string ruta)
        {
            if (string.IsNullOrEmpty(ruta))
                return NotFound();

            try
            {
                var rutaCompleta = Path.Combine(_environment.WebRootPath, ruta.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                var nombreArchivo = Path.GetFileName(rutaCompleta);

                if (!System.IO.File.Exists(rutaCompleta))
                    return NotFound();

             
                var extension = Path.GetExtension(rutaCompleta).ToLower();
                var tipoMime = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                return PhysicalFile(rutaCompleta, tipoMime, nombreArchivo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al descargar imagen: {ex.Message}");
                return StatusCode(500, "Error al descargar la imagen");
            }
        }
    }
}