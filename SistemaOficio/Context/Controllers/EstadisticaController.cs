using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfiGest.Managers;
using OfiGest.Models;

namespace OfiGest.Context.Controllers
{
    [Authorize]
    public class EstadisticaController : Controller
    {
        private readonly EstadisticaManager _estadisticaManager;

        public EstadisticaController(EstadisticaManager estadisticaManager)
        {
            _estadisticaManager = estadisticaManager;
        }

        [HttpGet]
        public async Task<IActionResult> PorTipo()
        {
            try
            {
                var rolUsuario = HttpContext.Session.GetString("RolUsuario");
                List<TipoDatoResumen> resumen;

                if (rolUsuario == "Administrador")
                {
                    resumen = await _estadisticaManager.ObtenerResumenPorTipoAsync();
                }
                else
                {
                    var departamentoIdStr = HttpContext.Session.GetString("DepartamentoId");
                    if (!int.TryParse(departamentoIdStr, out var departamentoId))
                    {
                        TempData["Error"] = "Departamento inválido en sesión.";
                        return RedirectToAction("Index", "Home");
                    }
                    resumen = await _estadisticaManager.ObtenerResumenPorTipoAsync(departamentoId);
                }

                ViewBag.TotalOficios = resumen.Sum(r => r.Cantidad);
                return View(resumen);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al cargar las estadísticas.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}