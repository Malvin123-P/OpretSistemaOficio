using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfiGest.Manegers;
using OfiGest.Models;

namespace OfiGest.Context.Controllers
{
    [Authorize]
    public class DepartamentoController : Controller
    {
        private readonly DepartamentosManenger _manenger;

        public DepartamentoController(DepartamentosManenger manenger)
        {
            _manenger = manenger;
        }

        public async Task<IActionResult> Index()
        {
            var departamentos = await _manenger.ObtenerTodosAsync();
            return View(departamentos);
        }

        [Authorize(Policy = "Administrador")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new DepartamentoModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartamentoModel model)
        {


            var existente = await _manenger.ObtenerPorNombreAsync(model.Nombre);
            if (existente != null)
            {
                TempData["Validacion"] = "Ya existe un departamento con ese nombre.";
                return View(model);
            }

            var existenteIniciales = await _manenger.ObtenerPorInicialesAsync(model.Iniciales);
            if (existenteIniciales != null)
            {
                TempData["Validacion"] = "Ya existe un departamento con esas iniciales, puedes modificarla.";
                return View(model);
            }


            var creado = await _manenger.CrearAsync(model);
            if (!creado)
            {
                TempData["Error"] = "No se pudo crear el departamento. Intenta nuevamente.";
                return View(model);
            }

            TempData["Success"] = "Departamento creado exitosamente.";
            return RedirectToAction("Index");
        }

        [Authorize(Policy = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _manenger.ObtenerPorIdAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Departamento no encontrado.";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DepartamentoModel model)
        {

            var original = await _manenger.ObtenerPorIdAsync(model.Id);
            if (original == null)
            {
                TempData["Error"] = "Departamento no encontrado.";
                return RedirectToAction("Index");
            }

            var conflictoIniciales = await _manenger.ExisteInicialesAsync(model.Iniciales, model.Id);
            if (conflictoIniciales)
            {
                TempData["Validacion"] = "Ya existe otro departamento con esas iniciales. Por favor, modifíquelas.";
                return View(model);
            }


            ModelState.Remove(nameof(model.Divisiones));
            ModelState.Remove(nameof(model.NombreEncargado));

            if (!ModelState.IsValid)
                return View(model);

            bool sinCambios =
                 original.Nombre == model.Nombre &&
                 original.Iniciales == model.Iniciales &&
                 (original.Descripcion ?? "") == (model.Descripcion ?? "");

            if (sinCambios)
            {
                TempData["Warning"] = "No se detectaron cambios en el formulario. Realice alguna modificación antes de actualizar.";
                return RedirectToAction("Edit", new { id = model.Id });
            }

            if (!ModelState.IsValid)
            {
        
                return View(model);
            }


            try
            {
                var actualizado = await _manenger.ActualizarAsync(model);
                TempData[actualizado ? "Success" : "Error"] = actualizado
                    ? "Departamento modificado correctamente."
                    : "Error al modificar el Departamento. No se realizaron los cambios en la base de datos.";

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

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var (eliminado, tieneUsuarios, tieneDivisiones) = await _manenger.EliminarAsync(id);

            if (id <= 0)
            {
                TempData["Error"] = "ID inválido.";
                return RedirectToAction("Index");
            }

            if (tieneUsuarios)
            {
                TempData["Error"] = "No se puede eliminar el departamento porque tiene usuarios asignados. Reasigne o elimine los usuarios antes de continuar.";
            }
            else if (tieneDivisiones)
            {
                TempData["Error"] = "No se puede eliminar el departamento porque tiene divisiones asociadas. Reasigne o elimine las divisiones antes de continuar.";
            }
            else if (!eliminado)
            {
                TempData["Error"] = "No se pudo eliminar el departamento. Intenta nuevamente.";
            }
            else
            {
                TempData["Success"] = "Departamento eliminado correctamente.";
            }

            return RedirectToAction("Index");
        }
    }
}