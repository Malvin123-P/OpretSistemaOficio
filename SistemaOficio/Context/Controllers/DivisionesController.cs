using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfiGest.Manegers;
using OfiGest.Models;

namespace OfiGest.Context.Controllers
{
    [Authorize(Policy = "Administrador")]
    public class DivisionesController : Controller
    {
        private readonly DivisionesManager _divisionesManenger;

        public DivisionesController(DivisionesManager manenger)
        {
            _divisionesManenger = manenger;
        }

        public async Task<IActionResult> Index()
        {
            var divisiones = await _divisionesManenger.ObtenerTodosAsync();
            return View(divisiones);
        }

        private async Task CargarListasAsync(int? departamentoId = null)
        {
            var departamentos = await _divisionesManenger.ObtenerDepartamentosComboBoxAsync();
            var departamentoSeleccionado = departamentos.Any(d => d.Id == departamentoId) ? departamentoId : null;
            ViewBag.Departamentos = new SelectList(departamentos, "Id", "Nombre", departamentoSeleccionado);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View(new DivisionesModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DivisionesModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarListasAsync(model.DepartamentoId);
                return View(model);
            }

            var existente = await _divisionesManenger.ObtenerPorNombreYDepartamentosAsync(model.Nombre, model.DepartamentoId);
            if (existente != null)
            {
                TempData["Validacion"] = "Ya existe una división con ese nombre en el departamento.";
                await CargarListasAsync(model.DepartamentoId);
                return View(model);
            }

            var existenteIniciales = await _divisionesManenger.ObtenerPorInicialesYDepartamentoAsync(model.Iniciales, model.DepartamentoId);
            if (existenteIniciales != null)
            {
                TempData["Validacion"] = "Ya existe una división con esas iniciales en este departamento. Puedes modificarlas.";
                await CargarListasAsync(model.DepartamentoId);
                return View(model);
            }

            try
            {
                var creado = await _divisionesManenger.CrearAsync(model);
                if (creado)
                {
                    TempData["Success"] = "División creada correctamente.";
                    return RedirectToAction("Index");
                }

                TempData["Error"] = "Error al crear la división.";
                await CargarListasAsync(model.DepartamentoId);
                return View(model);
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await CargarListasAsync(model.DepartamentoId);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var division = await _divisionesManenger.ObtenerPorIdAsync(id);
            if (division == null)
            {
                TempData["Error"] = "División no encontrada.";
                return RedirectToAction("Index");
            }

            await CargarListasAsync(division.DepartamentoId);
            return View(division);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DivisionesModel model)
        {
            var original = await _divisionesManenger.ObtenerPorIdAsync(model.Id);
            if (original == null)
            {
                TempData["Error"] = "División no encontrada.";
                return RedirectToAction("Index");
            }


            var conflictoIniciales = await _divisionesManenger.ExisteInicialesEnDepartamentoAsync(model.Iniciales, model.DepartamentoId, model.Id);
            if (conflictoIniciales)
            {
                TempData["Validacion"] = "Ya existe una división con esas iniciales en el departamento seleccionado.";
                await CargarListasAsync(model.DepartamentoId);
                return View(model);
            }

            bool sinCambios =
                original.Nombre == model.Nombre &&
                original.Iniciales == model.Iniciales &&
                original.DepartamentoId == model.DepartamentoId &&
                (original.Descripcion ?? "") == (model.Descripcion ?? "");

            if (sinCambios)
            {
                TempData["Warning"] = "No se detectaron cambios en el formulario. Realice alguna modificación antes de actualizar.";
                return RedirectToAction("Edit", new { id = model.Id });
            }

            if (!ModelState.IsValid)
            {
                await CargarListasAsync(model.DepartamentoId);
                return View(model);
            }

            try
            {
                var actualizado = await _divisionesManenger.ActualizarAsync(model);
                TempData[actualizado ? "Success" : "Error"] = actualizado
                    ? "División modificada correctamente."
                    : "Error al modificar la división. No se realizaron los cambios en la base de datos.";

                return RedirectToAction("Index");
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await CargarListasAsync(model.DepartamentoId);
                return View(model);
            }
            catch (Exception)
            {
                TempData["Error"] = "Ocurrió un error inesperado al procesar la solicitud.";
                await CargarListasAsync(model.DepartamentoId);
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

            var (eliminado, tieneUsuarios) = await _divisionesManenger.EliminarAsync(id);

            if (tieneUsuarios)
            {
                TempData["Error"] = "No se puede eliminar la división porque tiene usuarios asignados. Reasigne o elimine los usuarios antes de continuar.";
            }
            else if (!eliminado)
            {
                TempData["Error"] = "No se pudo eliminar la división. Intenta nuevamente.";
            }
            else
            {
                TempData["Success"] = "División eliminada correctamente.";
            }

            return RedirectToAction("Index");
        }
    }
}