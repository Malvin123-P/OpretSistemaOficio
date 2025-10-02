using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OfiGest.Context.Controllers
{
    [Authorize]
    public class NotificacionController : Controller
    {
        private readonly NotificacionManager _notificacionManager;

        public NotificacionController(NotificacionManager notificacionManager)
        {
            _notificacionManager = notificacionManager;
        }

     
        [HttpGet]
        public async Task<JsonResult> ObtenerCantidad()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null)
                return Json(new { cantidad = 0 });

            var cantidad = await _notificacionManager.ObtenerCantidadNotificacionesPendientesAsync(usuarioId.Value);
            return Json(new { cantidad });
        }

     
        [HttpGet]
        public async Task<JsonResult> ObtenerLista()
        {
            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null)
                return Json(new { notificaciones = new List<object>() });

            var notificaciones = await _notificacionManager.ObtenerNotificacionesAsync(usuarioId.Value);

            var resultado = notificaciones.Select(n => new
            {
                n.Id,
                OficioId = n.OficioId,
                n.TipoOficio,
                Fecha = n.FechaCreacion.ToString("dd/MM/yyyy HH:mm"),
                DepartamentoRemitente = n.DepartamentoRemitente ?? "No Especificado"
            });

            return Json(new { notificaciones = resultado });
        }

        [HttpPost]
        public async Task<JsonResult> MarcarComoLeida([FromBody] int id)
        {
            Console.WriteLine($" [MarcarComoLeida] ID recibido: {id}");

            if (id <= 0) return Json(new { success = false, message = "ID inválido" });

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId == null) return Json(new { success = false, message = "Usuario no autenticado" });

            var resultado = await _notificacionManager.MarcarComoLeidaAsync(id, usuarioId.Value);
            return Json(new { success = resultado, message = resultado ? "Marcado como leído" : "No se pudo marcar" });
        }


     
        private int? ObtenerUsuarioId()
        {
            var claimId = User.FindFirst("Id")?.Value;
            return int.TryParse(claimId, out int id) ? id : null;
        }
    }
}