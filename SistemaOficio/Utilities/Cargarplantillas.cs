namespace OfiGest.Utilities
{
    public class Cargarplantillas
    {
        private readonly IWebHostEnvironment _env;

        public Cargarplantillas(IWebHostEnvironment env)
        {
            _env = env;
        }

        public List<string> ObtenerPlantillasDisponibles()
        {
            var ruta = Path.Combine(_env.WebRootPath, "plantillas");
            return Directory.Exists(ruta)
                ? Directory.GetFiles(ruta).Select(Path.GetFileName).ToList()
                : new List<string>();
        }

        public bool PlantillaExiste(string nombreArchivo)
        {
            var ruta = Path.Combine(_env.WebRootPath, "plantillas", nombreArchivo);
            return File.Exists(ruta);
        }

        public string ObtenerRutaVirtual(string nombreArchivo)
        {
            return $"/plantillas/{nombreArchivo}";
        }
    }
}
