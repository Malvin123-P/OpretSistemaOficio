using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfiGest.Managers;
using OfiGest.Manegers;
using OfiGest.Models;
using OfiGest.Utilities;
using System.Text.RegularExpressions;

namespace OfiGest.Context.Controllers
{
    [Authorize]
    public class OficioController : Controller
    {
        private readonly OficioManager _managerOficio;
        private readonly ApplicationDbContext _context;

        public OficioController(OficioManager managerOficio, ApplicationDbContext context)
        {
            _managerOficio = managerOficio;
            _context = context;
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var oficio = await _context.Oficios
                    .Include(o => o.Usuario)
                    .Include(o => o.DepartamentoRemitente)
                    .Include(o => o.TipoOficio)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (oficio == null)
                {
                    TempData["Error"] = "Oficio no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (!oficio.Estado)
                {
                    TempData["Error"] = "No se puede descargar un oficio inactivo";
                    return RedirectToAction(nameof(Index));
                }

                var esCertificacion = EsCertificacion(oficio.TipoOficio?.Nombre);

                var encargadoDepartamental = await _context.Usuarios
                    .Where(u => u.DepartamentoId == oficio.DepartamentoId &&
                               u.EsEncargadoDepartamental &&
                               u.Activo)
                    .Select(u => $"{u.Nombre} {u.Apellido}".Trim())
                    .FirstOrDefaultAsync();

                if (esCertificacion)
                {
                  
                    var certificacionModel = new CertificacionPdfModel
                    {
                        Codigo = oficio.Codigo ?? "",
                        FechaCreacion = oficio.FechaCreacion,
                        DepartamentoRemitente = oficio.DepartamentoRemitente?.Nombre ?? "",
                        Contenido = oficio.Contenido ?? "",
                        Anexos = oficio.Anexos ?? "",
                        EncargadoDepartamental = encargadoDepartamental ?? "Encargado Departamental",
                        CargoFirmante = "Jefe de Departamento"
                    };

                    var certificacionManager = new PdfCertificacionManager();
                    var pdfBytes = certificacionManager.GenerarPdf(certificacionModel);
                    var nombreArchivo = certificacionManager.ObtenerNombreArchivo(certificacionModel);

                    return File(pdfBytes, "application/pdf", nombreArchivo);
                }
                else
                {
                   
                    var pdfModel = new OficioPdfModel
                    {
                        Codigo = oficio.Codigo ?? "",
                        FechaCreacion = oficio.FechaCreacion,
                        DepartamentoRemitente = oficio.DepartamentoRemitente?.Nombre ?? "",
                        DirigidoDepartamento = oficio.DirigidoDepartamento ?? "",
                        TipoOficio = oficio.TipoOficio?.Nombre ?? "",
                        Via = oficio.Via ?? "",
                        Contenido = oficio.Contenido ?? "",
                        Anexos = oficio.Anexos ?? "",
                        EncargadoDepartamental = encargadoDepartamental ?? "Encargado Departamental"
                    };

                    var pdfManager = new PdfOficioManager();
                    var pdfBytes = pdfManager.GenerarPdf(pdfModel);
                    var nombreArchivo = pdfManager.ObtenerNombreArchivo(pdfModel);

                    return File(pdfBytes, "application/pdf", nombreArchivo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar PDF: {ex.Message}");
                TempData["Error"] = "Error al generar el PDF del documento";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Index()
        {
            var rolUsuario = HttpContext.Session.GetString("RolUsuario");
            var departamentoIdStr = HttpContext.Session.GetString("DepartamentoId");

            List<OficioModel> oficios;

            if (rolUsuario == "Administrador")
            {
                oficios = await _managerOficio.ObtenerTodosAsync();
            }
            else if (int.TryParse(departamentoIdStr, out int departamentoId))
            {
                oficios = (await _managerOficio.ObtenerActivosAsync())
                    .Where(o => o.DepartamentoId == departamentoId)
                    .ToList();
            }
            else
            {
                TempData["Error"] = "No se pudo determinar el departamento del usuario.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.TiposOficio = oficios
                .Select(o => o.NombreTipoOficio)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n)
                .Select(n => new SelectListItem { Value = n, Text = n })
                .ToList();

            ViewBag.Departamentos = oficios
                .Select(o => o.DirigidoDepartamento)
                .Distinct()
                .OrderBy(n => n)
                .Select(n => new SelectListItem { Value = n, Text = n })
                .ToList();

            ViewBag.UsuariosFiltro = oficios
                .Select(o => $"{o.NombreUsuario} {o.ApellidoUsuario}".Trim())
                .Distinct()
                .OrderBy(n => n)
                .Select(n => new SelectListItem { Value = n, Text = n })
                .ToList();

            return View(oficios);
        }

        public async Task<IActionResult> Inactivos()
        {
            var rolUsuario = HttpContext.Session.GetString("RolUsuario");
            if (rolUsuario != "Administrador")
            {
                TempData["Error"] = "Acceso denegado.";
                return RedirectToAction("Index");
            }

            var oficios = await _managerOficio.ObtenerInactivosAsync();
            return View(oficios);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View(new OficioModel());
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var oficio = await _managerOficio.ObtenerPorIdAsync(id);
            if (oficio == null)
            {
                TempData["Error"] = "Oficio no encontrado.";
                return RedirectToAction("Index");
            }

            await CargarListasAsync(oficio.TipoOficioId, oficio.DirigidoDepartamento);
            return View(oficio);
        }

        public async Task<IActionResult> DescargarPdf(int id)
        {
            try
            {
                var oficio = await _context.Oficios
                    .Include(o => o.DepartamentoRemitente)
                    .Include(o => o.TipoOficio)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (oficio == null)
                {
                    TempData["Error"] = "Documento no encontrado";
                    return RedirectToAction("Index");
                }

           
                var esCertificacion = EsCertificacion(oficio.TipoOficio?.Nombre);

                var encargadoDepartamental = await _context.Usuarios
                    .Where(u => u.DepartamentoId == oficio.DepartamentoId &&
                               u.EsEncargadoDepartamental &&
                               u.Activo)
                    .Select(u => $"{u.Nombre} {u.Apellido}".Trim())
                    .FirstOrDefaultAsync();

                if (esCertificacion)
                {
                    var certificacionModel = new CertificacionPdfModel
                    {
                        Codigo = oficio.Codigo ?? "",
                        FechaCreacion = oficio.FechaCreacion,
                        DepartamentoRemitente = oficio.DepartamentoRemitente?.Nombre ?? "",
                        Contenido = oficio.Contenido ?? "",
                        Anexos = oficio.Anexos ?? "",
                        EncargadoDepartamental = encargadoDepartamental ?? "Encargado Departamental",
                        CargoFirmante = "Jefe de Departamento"
                    };

                    var certificacionManager = new PdfCertificacionManager();
                    var pdfBytes = certificacionManager.GenerarPdf(certificacionModel);
                    var fileName = certificacionManager.ObtenerNombreArchivo(certificacionModel);

                    return File(pdfBytes, "application/pdf", fileName);
                }
                else
                {
                    var pdfModel = new OficioPdfModel
                    {
                        Codigo = oficio.Codigo ?? "",
                        FechaCreacion = oficio.FechaCreacion,
                        DepartamentoRemitente = oficio.DepartamentoRemitente?.Nombre ?? "",
                        DirigidoDepartamento = oficio.DirigidoDepartamento ?? "",
                        TipoOficio = oficio.TipoOficio?.Nombre ?? "",
                        Via = oficio.Via ?? "",
                        Contenido = oficio.Contenido ?? "",
                        Anexos = oficio.Anexos ?? "",
                        EncargadoDepartamental = encargadoDepartamental ?? "Encargado Departamental"
                    };

                    var pdfManager = new PdfOficioManager();
                    var pdfBytes = pdfManager.GenerarPdf(pdfModel);
                    var fileName = pdfManager.ObtenerNombreArchivo(pdfModel);

                    return File(pdfBytes, "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al generar el PDF";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OficioModel oficio)
        {
            var departamentoUsuario = HttpContext.Session.GetString("NombreDepartamento");
            if (oficio.DirigidoDepartamento == departamentoUsuario)
            {
                ViewData["Error"] = "No puedes dirigir un documento a tu propio departamento.";
                await CargarListasAsync(oficio.TipoOficioId, oficio.DirigidoDepartamento);
                return View(oficio);
            }

            ModelStateHelper.LimpiarPropiedadesNoValidables(ModelState,
                nameof(oficio.Anexos),
                nameof(oficio.ApellidoUsuario),
                nameof(oficio.ModificadoEn),
                nameof(oficio.ModificadoPorId),
                nameof(oficio.MotivoModificacion),
                nameof(oficio.NombreDepartamento),
                nameof(oficio.NombreTipoOficio),
                nameof(oficio.NombreUsuario),
                nameof(oficio.Codigo)
            );

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(oficio.TipoOficioId, oficio.DirigidoDepartamento);
                return View(oficio);
            }

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null)
            {
                ViewData["Error"] = "No se pudo identificar al usuario logueado.";
                return RedirectToAction("Index");
            }

            try
            {
                var generarPdf = Request.Form["Descargar"].ToString() == "true";

            
                var resultado = await _managerOficio.CrearOficioAsync(oficio, usuarioId.Value, false);

                if (resultado.Success)
                {
                   
                    var documentoCreado = await _context.Oficios
                        .Where(o => o.UsuarioId == usuarioId.Value)
                        .OrderByDescending(o => o.FechaCreacion)
                        .FirstOrDefaultAsync();

                    if (documentoCreado != null)
                    {
                        if (generarPdf)
                        {
                        
                            TempData["OficioCreadoId"] = documentoCreado.Id;
                            TempData["Success"] = "Documento creado correctamente. El PDF se descargará automáticamente.";
                        }
                        else
                        {
                            TempData["Success"] = "Documento creado correctamente.";
                        }

                        return RedirectToAction("Index");
                    }
                }

                ViewData["Error"] = "Error al crear el documento. Verifica los datos o permisos.";
                await CargarListasAsync(oficio.TipoOficioId, oficio.DirigidoDepartamento);
                return View(oficio);
            }
            catch (Exception ex)
            {
                ViewData["Error"] = $"Ocurrió un error inesperado: {ex.Message}";
                await CargarListasAsync(oficio.TipoOficioId, oficio.DirigidoDepartamento);
                return View(oficio);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OficioModel modelo)
        {
            var departamentoUsuario = HttpContext.Session.GetString("NombreDepartamento");
            if (modelo.DirigidoDepartamento == departamentoUsuario)
            {
                TempData["Error"] = "Tu departamento actual coincide con el destino del documento. Modifica el destino para mantener la trazabilidad institucional.";
                await CargarListasAsync(modelo.TipoOficioId, modelo.DirigidoDepartamento);
                return View(modelo);
            }

            var original = await _managerOficio.ObtenerPorIdAsync(modelo.Id);
            if (original == null)
            {
                TempData["Error"] = "Documento no encontrado.";
                return RedirectToAction("Index");
            }

            bool sinCambios =
                original.Contenido?.Trim() == modelo.Contenido?.Trim() &&
                original.TipoOficioId == modelo.TipoOficioId &&
                original.DirigidoDepartamento?.Trim() == modelo.DirigidoDepartamento?.Trim() &&
                original.Via?.Trim() == modelo.Via?.Trim() &&
                original.Anexos?.Trim() == modelo.Anexos?.Trim() &&
                original.Estado == modelo.Estado;

            if (sinCambios)
            {
                TempData["Warning"] = "No se detectaron cambios en el formulario. Realice alguna modificación antes de actualizar.";
                await CargarListasAsync(modelo.TipoOficioId, modelo.DirigidoDepartamento);
                return View(modelo);
            }

            ModelStateHelper.LimpiarPropiedadesNoValidables(ModelState,
              nameof(modelo.Anexos),
              nameof(modelo.ApellidoUsuario),
              nameof(modelo.ModificadoEn),
              nameof(modelo.ModificadoPorId),
              nameof(modelo.MotivoModificacion),
              nameof(modelo.NombreDepartamento),
              nameof(modelo.NombreTipoOficio),
              nameof(modelo.NombreUsuario),
              nameof(modelo.Codigo)
            );

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(modelo.TipoOficioId, modelo.DirigidoDepartamento);
                return View(modelo);
            }

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null)
            {
                TempData["Error"] = "No se pudo identificar al usuario logueado.";
                return RedirectToAction("Index");
            }

            try
            {
                var actualizado = await _managerOficio.ActualizarOficioAsync(modelo, usuarioId.Value);
                TempData[actualizado ? "Success" : "Error"] = actualizado
                    ? "Documento modificado correctamente."
                    : "Error al modificar el documento. No se realizaron los cambios en la base de datos.";

                return RedirectToAction("Index");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await CargarListasAsync(modelo.TipoOficioId, modelo.DirigidoDepartamento);
                return View(modelo);
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocurrió un error inesperado al procesar la solicitud.";
                await CargarListasAsync(modelo.TipoOficioId, modelo.DirigidoDepartamento);
                return View(modelo);
            }
        }

        private async Task CargarListasAsync(int? tipoOficioId = null, string? DirigidoDepartamento = null)
        {
            var tiposOficio = await _managerOficio.ObtenerTipoOficioAsync();
            var departamentos = await _managerOficio.ObtenerDepartamentosAsync();

            var departamentoUsuario = HttpContext.Session.GetString("DepartamentoId");

            var departamentosFiltrados = departamentos
                .Where(d => d.Id.ToString() != departamentoUsuario)
                .ToList();

            ViewBag.TiposOficio = new SelectList(tiposOficio, "Id", "Nombre", tipoOficioId);
            ViewBag.Departamentos = new SelectList(departamentosFiltrados, "Nombre", "Nombre", DirigidoDepartamento);
        }

        private int? ObtenerUsuarioId()
        {
            var claimId = User.FindFirst("Id")?.Value;
            return int.TryParse(claimId, out int id) ? id : null;
        }

    
        private bool EsCertificacion(string? tipoDocumento)
        {
            if (string.IsNullOrWhiteSpace(tipoDocumento))
                return false;

            var tiposCertificacion = new[]
            {
                "certificacion",
                "certificación",
                "certificado",
                "constancia",
                "certify",
                "certificate"
            };

            return tiposCertificacion.Any(tipo =>
                tipoDocumento.Trim().ToLower().Contains(tipo));
        }
    }
}