
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OfiGest.Manegers;
using OfiGest.Models;
using System.Security.Claims;

namespace OfiGest.Context.Controllers
{
    public class LoginController : Controller
    {
        private readonly LoginUserManeger _loginUser;

        public LoginController(LoginUserManeger loginUser)
        {
            _loginUser = loginUser;
        }

        [HttpGet]
        public IActionResult Index()
        {

            return View(new LoginModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var resultado = _loginUser.AutenticarUsuario(model.Correo, model.Contraseña);

            if (resultado.Usuario == null || resultado.Usuario.Id == 0)
            {
                ModelState.AddModelError("Contraseña", "Credenciales inválidas.");
                return View(model);
            }

            var usuario = resultado.Usuario;

            if (usuario.RequiereRestablecer)
            {
                ModelState.AddModelError("Contraseña", "Tu cuenta requiere restablecimiento de contraseña. Revisa tu correo.");
                return View(model);
            }

            if (!usuario.Activo)
            {
                ModelState.AddModelError("Contraseña", "Tu cuenta está inactiva. Contacta al administrador.");
                return View(model);
            }

            await _loginUser.ActualizarUltimoAccesoAsync(usuario.Id);

            // Guardar datos en sesión
            HttpContext.Session.SetInt32("Id", usuario.Id);
            HttpContext.Session.SetString("UltimoAcceso", usuario.UltimoAcceso?.ToString() ?? "Sin registro");
            HttpContext.Session.SetString("NombreUsuario", $"{usuario.Nombre} {usuario.Apellido}");
            HttpContext.Session.SetString("RolUsuario", usuario.Rol.Nombre);
            HttpContext.Session.SetString("DepartamentoId", usuario.DepartamentoId.ToString());
            HttpContext.Session.SetString("NombreDepartamento", usuario.Departamento.Nombre.ToString());
            HttpContext.Session.SetString("ImagenPerfil", usuario.ImagenPerfil ?? "/images/usuario.png");
            HttpContext.Session.SetString("EsEncargadoDepartamental", usuario.EsEncargadoDepartamental.ToString());

            // Mensaje de bienvenida
            TempData["PrimerAcceso"] = resultado.EsPrimerAcceso;

            // Claims para autenticación
            var claims = new List<Claim>
            {
              new Claim(ClaimTypes.Name, usuario.Correo),
              new Claim(ClaimTypes.Role, usuario.Rol.Nombre),
              new Claim("Rol", usuario.Rol.Nombre),
              new Claim("Id", usuario.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToAction("Index", "Oficio");

        }

        [HttpGet]
        public IActionResult AccesoDenegado(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}


