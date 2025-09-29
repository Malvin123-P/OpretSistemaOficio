using System.Security.Cryptography;

namespace OfiGest.Utilities
{
    public static class TokenGenerate
    {
        public static string GenerarToken(int longitudBytes = 32)
        {
            var tokenBytes = new byte[longitudBytes];
            RandomNumberGenerator.Fill(tokenBytes);
            return Convert.ToBase64String(tokenBytes);
        }

    }
}
