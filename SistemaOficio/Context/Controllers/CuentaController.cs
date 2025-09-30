using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OfiGest.Manegers;
using OfiGest.Models;
using OfiGest.Utilities;
using System.Net;
using System.Text.RegularExpressions;

namespace OfiGest.Context.Controllers
{
    public class CuentaController : Controller
    {
        private readonly CorreoManager _correoManager;
        private readonly ApplicationDbContext _context;
      

        public CuentaController(CorreoManager correoManager, ApplicationDbContext context)
        {
            _correoManager = correoManager;
            _context = context;
         
        }

        [HttpGet]
        public IActionResult SolicitarRestablecimiento()
        {
            if (TempData["Error"] != null)
                ModelState.AddModelError("Correo", "El enlace ha expirado o no es válido. Por favor, solicita uno nuevo desde la opción de recuperación.");

            return View(new SolicitudRestablecimientoViewModel());
        }

        [HttpPost]
        public IActionResult SolicitarRestablecimiento(SolicitudRestablecimientoViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var dominiosPermitidos = Environment.GetEnvironmentVariable("SeguridadCorreo_DominiosPermitidos").Split(',');

            var correoNormalizado = model.Correo.Trim().ToLower();
        
            if (!dominiosPermitidos.Any(d => correoNormalizado.EndsWith(d)))
            {
                ModelState.AddModelError("Correo", "El dominio del correo no está permitido.");
                return View(model);
            }

            if (!_correoManager.UsuarioExiste(correoNormalizado))
            {
                ModelState.AddModelError("Correo", "El correo no está registrado.");
                return View(model);
            }

            var enviado = _correoManager.EnviarRestablecimientoClave(correoNormalizado);

            if (!enviado)
            {
                var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correoNormalizado);
                var tokenActivo = usuario?.RequiereRestablecer == false && usuario.TokenExpira >= DateTime.UtcNow;

                if (tokenActivo)
                {
                    ModelState.AddModelError("Correo", "Ya existe una solicitud activa. Revisa tu correo y usa el enlace más reciente.");
                }
                else
                {
                    ModelState.AddModelError("Correo", "No se pudo enviar el correo. Intenta más tarde.");
                }

                return View(model);
            }

            TempData["RestablecimientoExitoso"] = "Enlace de recuperación enviado. Revisa tu correo.";
            return RedirectToAction("Index", "Login");

        }

        [HttpGet]
        public IActionResult Restablecer(string correo, string token)
        {
            try
            {
                var tokenDecodificado = WebUtility.UrlDecode(token);
                var correoDecodificado = WebUtility.UrlDecode(correo);

                if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "El enlace de restablecimiento es inválido.";
                    return RedirectToAction("SolicitarRestablecimiento");
                }

                if (!_correoManager.ValidarTokenRestablecimiento(correo, token))
                {
                    TempData["Error"] = "El enlace ha expirado o no es válido. Por favor, solicita uno nuevo desde la opción de recuperación.";
                    return RedirectToAction("SolicitarRestablecimiento");
                }

                return View(new RestablecerClaveViewModel { Correo = correo, Token = token });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al procesar su solicitud. Intente nuevamente.";
                return RedirectToAction("SolicitarRestablecimiento");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Restablecer(RestablecerClaveViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var regex = new Regex(@"^(?=.*[A-Z])(?=.*\d).{8,}$");
            if (!regex.IsMatch(model.NuevaContraseña ?? ""))
            {
                ModelState.AddModelError("NuevaContraseña", "La contraseña debe tener al menos 8 caracteres, una mayúscula y un número.");
                return View(model);
            }

            var correoNormalizado = model.Correo?.Trim().ToLower();
            var tokenRecibido = model.Token?.Trim();

            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo.ToLower() == correoNormalizado);

            if (usuario == null)
            {
                ModelState.AddModelError("Correo", "Usuario no encontrado.");
                return View(model);
            }

            if (!_correoManager.ValidarTokenRestablecimiento(correoNormalizado, tokenRecibido))
            {
                ModelState.AddModelError("Correo", "El enlace ha expirado o no es válido. Solicita uno nuevo.");
                return View(model);
            }

            usuario.Contraseña = model.NuevaContraseña.HashPassword();
            usuario.Token = null;
            usuario.TokenExpira = null;
            usuario.RequiereRestablecer = false;

            try
            {
                _context.SaveChanges();

                // Limpiar cualquier sesión existente para este usuario
                HttpContext.Session.Remove("Id");
                HttpContext.Session.Remove("UltimoAcceso");
                HttpContext.Session.Remove("NombreUsuario");
                HttpContext.Session.Remove("RolUsuario");
                HttpContext.Session.Remove("DepartamentoId");
                HttpContext.Session.Remove("NombreDepartamento");

                // Cerrar sesión si está autenticado
                if (User.Identity.IsAuthenticated)
                {
                     HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
                }

                TempData["RestablecimientoExitoso"] = "Contraseña actualizada correctamente. Ya puedes iniciar sesión.";
                return RedirectToAction("Index", "Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Ocurrió un error al actualizar la contraseña. Intenta nuevamente.");
                return View(model);
            }
        }

    }
}