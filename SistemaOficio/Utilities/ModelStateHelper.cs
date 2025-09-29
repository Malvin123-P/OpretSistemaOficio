using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OfiGest.Utilities
{
    public static class ModelStateHelper
    {
        public static void LimpiarPropiedadesNoValidables(ModelStateDictionary modelState, params string[] propiedades)
        {
            foreach (var prop in propiedades.Distinct())
                modelState.Remove(prop);
        }
    }
}
