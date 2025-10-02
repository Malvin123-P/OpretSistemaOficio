using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfiGest.Manegers;
using OfiGest.Models;
using System.Text.RegularExpressions;

namespace OfiGest.Context.Controllers
{
    [Authorize]
    public class TipoOficioController : Controller
    {
        private readonly TipoOficioManenger _manenger;

        public TipoOficioController(TipoOficioManenger manenger)
        {
            _manenger = manenger;
        }

        public async Task<IActionResult> Index()
        {
            var tipos = await _manenger.ObtenerTodosAsync();
            return View(tipos);
        }

        [Authorize(Policy = "Administrador")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new TipoOficioModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TipoOficioModel model)
        {
            // Validar formato del nombre
            if (!string.IsNullOrEmpty(model.Nombre) && !EsNombreValido(model.Nombre))
            {
                TempData["Validacion"] = "El nombre solo puede contener letras, números, espacios y guiones.";
                return View(model);

            }

            ModelState.Remove(nameof(model.Iniciales));

            if (!ModelState.IsValid)
                return View(model);

            var existente = await _manenger.ObtenerPorNombreAsync(model.Nombre);
            if (existente != null)
            {
                TempData["Validacion"] = "Ya existe el tipo de oficio con ese nombre.";
                return View(model);
            }

            var existenteCodigoCorto = await _manenger.ObtenerPorCodigoAsync(model.Iniciales);
            if (existenteCodigoCorto != null)
            {
                TempData["Validacion"] = "Ya existe el tipo de oficio con ese iniciales.";
                return View(model);
            }

            var conflictoCompuesto = await _manenger.ExisteTipoOficioCompuestoAsync(model.Nombre, model.Iniciales);

            if (conflictoCompuesto)
            {
                TempData["Validacion"] = "Ya existe un tipo de oficio con el mismo nombre iniciales.";
                return View(model);
            }

            var creado = await _manenger.CrearAsync(model, ModelState);

            if (!ModelState.IsValid)
                return View(model);

            if (!creado)
            {
                TempData["Error"] = "No se pudo crear el tipo de oficio.";
                return View(model);
            }

            TempData["Success"] = "Tipo de oficio creado correctamente.";
            return RedirectToAction("Index");
        }

        [Authorize(Policy = "Administrador")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _manenger.ObtenerPorIdAsync(id);
            if (model == null)
            {
                TempData["Error"] = "Tipo de oficio no encontrado.";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TipoOficioModel model)
        {
          
            if (!string.IsNullOrEmpty(model.Nombre) && !EsNombreValido(model.Nombre))
            {
                TempData["Validacion"] = "El nombre solo puede contener letras, números, espacios y guiones.";
                return View(model);
            }

            var original = await _manenger.ObtenerPorIdAsync(model.Id);

            if (original == null)
            {
                TempData["Error"] = "Tipo de oficio no encontrado.";
                return RedirectToAction("Index");
            }

            bool sinCambios =
                original.Nombre == model.Nombre &&
                original.Iniciales == model.Iniciales &&
                original.Descripcion == model.Descripcion;

            if (sinCambios)
            {
                TempData["Warning"] = "No se detectaron cambios en el formulario. Realice alguna modificación antes de actualizar.";
                return RedirectToAction("Edit", new { id = model.Id });
            }

            var conflictoCompuesto = await _manenger.ExisteTipoOficioCompuestoAsync(model.Nombre, model.Iniciales);

            if (conflictoCompuesto)
            {
                TempData["Validacion"] = "Ya existe un tipo de oficio con el mismo nombre e iniciales .";
                return View(model);
            }

            ModelState.Remove(nameof(model.Iniciales));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var actualizado = await _manenger.ActualizarAsync(model);
                TempData[actualizado ? "Success" : "Warning"] = actualizado
                    ? "Tipo de oficio modificado correctamente."
                    : "Error al modificar el tipo de oficio. No se realizaron los cambios en la base de datos.";

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
            var (eliminado, tieneOficios) = await _manenger.EliminarAsync(id);

            if (tieneOficios)
            {
                TempData["Error"] = "No se puede eliminar el tipo de oficio porque está siendo utilizado por uno o más oficios. Reasigne o elimine esos oficios antes de continuar.";
            }
            else if (!eliminado)
            {
                TempData["Error"] = "No se pudo eliminar el tipo de oficio.";
            }
            else
            {
                TempData["Success"] = "Tipo de oficio eliminado correctamente.";
            }

            return RedirectToAction("Index");
        }

        private bool EsNombreValido(string nombre)
        {
   
            var regex = new Regex(@"^[\p{L}\p{N}\s\-]+$");
            return regex.IsMatch(nombre);
        }
    }
}