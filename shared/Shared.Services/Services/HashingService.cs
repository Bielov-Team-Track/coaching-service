using System.Security.Cryptography;
using Shared.Services.Services.Interfaces;

namespace Shared.Services.Services
{
    public class HashingService : IHashingService
    {
        public string Hash(string value)
        {
            // Generate cryptographically secure random salt
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            // Create the Rfc2898DeriveBytes and get the hash value
            var pbkdf2 = new Rfc2898DeriveBytes(value, salt, 100000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(20);

            // Combine the salt and password bytes for later use
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            // Turn the combined salt+hash into a string for storage
            return Convert.ToBase64String(hashBytes);
        }
    }
}
