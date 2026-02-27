using System;
using System.Security.Cryptography;
using System.Text;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Hashes tokens using SHA-256 + salt for secure storage.
    /// Tokens are never stored in plaintext.
    /// </summary>
    public class TokenHasher
    {
        private const string _salt = "AIOrchestrator_Token_Salt_v1"; // Static salt for now; consider per-token salt for higher security

        /// <summary>
        /// Hash a token using SHA-256 + salt.
        /// </summary>
        public string HashToken(string token)
        {
            using (var sha256 = SHA256.Create())
            {
                var input = $"{token}{_salt}";
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToHexString(hash);
            }
        }

        /// <summary>
        /// Verify a token against a stored hash.
        /// </summary>
        public bool VerifyToken(string token, string storedHash)
        {
            var computedHash = HashToken(token);
            return computedHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
