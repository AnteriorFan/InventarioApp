using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace InventarioApp.Security
{
        public static class PasswordHasher // Clase estática para el hashing de contraseñas
    {
            private const int SaltSize = 16;
            private const int HashSize = 32;
            private const int Iterations = 100000;

            public static string Hash(string password)
            {
                using (var rfc2898 = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] salt = rfc2898.Salt;
                    byte[] hash = rfc2898.GetBytes(HashSize);

                    byte[] hashBytes = new byte[SaltSize + HashSize];
                    Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                    Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

                    return Convert.ToBase64String(hashBytes);
                }
            }

            public static bool Verify(string password, string storedHash) // Verifica si la contraseña proporcionada coincide con el hash almacenado
        {
                byte[] hashBytes = Convert.FromBase64String(storedHash);

                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                using (var rfc2898 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] hash = rfc2898.GetBytes(HashSize);

                    for (int i = 0; i < HashSize; i++)
                    {
                        if (hashBytes[SaltSize + i] != hash[i])
                            return false;
                    }
                    return true;
                }
            }
        }
}