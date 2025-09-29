namespace OfiGest.Utilities.GenerarIniciales
{
    public class InicialesDepartamentos
    {
        public static string GenerarIniciales(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return string.Empty;

            var palabras = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var iniciales = string.Concat(palabras.Select(p => char.ToUpper(p[0])));
            return iniciales;
        }
    }
}
