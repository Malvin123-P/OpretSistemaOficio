using System.Security.Cryptography;
using System.Text;

namespace OfiGest.Utilities
{
    public static class PasswordHash
    {
        private const int SaltSize = 16; 
        private const int KeySize = 32;  
        private const int Iterations = 100_000;

        public static string HashPassword(this string password)
        {
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            var hashBytes = new byte[SaltSize + KeySize];
            Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
            Buffer.BlockCopy(key, 0, hashBytes, SaltSize, KeySize);

            return Convert.ToBase64String(hashBytes);
        }

        public static bool VerifyPassword(this string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            var hashBytes = Convert.FromBase64String(hashedPassword);
            if (hashBytes.Length < SaltSize + KeySize)
                return false;

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            return CryptographicOperations.FixedTimeEquals(
                hashBytes.AsSpan(SaltSize, KeySize),
                key.AsSpan()
            );
        }
    }
}
