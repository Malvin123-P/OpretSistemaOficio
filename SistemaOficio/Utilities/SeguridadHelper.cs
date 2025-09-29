namespace OfiGest.Utilities
{
        public static class SeguridadHelper
        {
            public static string GenerarContraseñaAleatoria(int longitud = 5)
            {
                const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
                var random = new Random();
                return new string(Enumerable.Range(0, longitud)
                    .Select(_ => caracteres[random.Next(caracteres.Length)]).ToArray());
            }
        }
    
}
