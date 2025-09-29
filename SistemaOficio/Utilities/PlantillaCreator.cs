namespace OfiGest.Utilities
{

    public class PlantillaCreator
    {
        public string GuardarPlantillaSubida(IFormFile archivo, string codigoCorto)
        {
            var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "plantillas");
            if (!Directory.Exists(carpeta))
                Directory.CreateDirectory(carpeta);

            var extension = Path.GetExtension(archivo.FileName);
            var nombreArchivo = $"{codigoCorto}_{DateTime.Now:dd-MM-yyyy}{extension}";
            var rutaFisica = Path.Combine(carpeta, nombreArchivo);

            using (var stream = new FileStream(rutaFisica, FileMode.Create))
            {
                archivo.CopyTo(stream);
            }

            return $"/plantillas/{nombreArchivo}";
        }

        public void EliminarPlantilla(string plantillaUrl)
        {
            if (string.IsNullOrWhiteSpace(plantillaUrl))
                return;

            var rutaFisica = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", plantillaUrl.TrimStart('/'));

            if (File.Exists(rutaFisica))
            {
                File.Delete(rutaFisica);
            }
        }
    }
}