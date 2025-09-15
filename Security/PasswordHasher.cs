using System;
using System.Security.Cryptography;

namespace WinCalc.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16;   
        private const int KeySize = 32;    
        private const int Iterations = 150_000;
        private const char Delim = ';';

        public static string Hash(string password)
        {
            if (password is null) throw new ArgumentNullException(nameof(password));
            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            return $"v1{Delim}{Iterations}{Delim}{Convert.ToBase64String(salt)}{Delim}{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return false;
            var parts = stored.Split(Delim);
            if (parts.Length != 4 || parts[0] != "v1") return false;
            if (!int.TryParse(parts[1], out var iters)) return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);

            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, iters, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
