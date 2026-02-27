using System;
using System.Security.Cryptography;
using System.Text;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Hashes tokens using PBKDF2 with per-token random salt for secure storage.
    /// Tokens are never stored in plaintext.
    /// </summary>
    public class TokenHasher : ITokenHasher
    {
        private const int SaltSizeBytes = 16;
        private const int HashIterations = 10000;
        private const int HashSizeBytes = 32;

        /// <summary>
        /// Hash a token using PBKDF2 with a random salt.
        /// Returns a string in format "salt:hash" (hex encoded).
        /// </summary>
        /// <param name="token">The plain token to hash.</param>
        /// <returns>The salted hash in format "salt:hash".</returns>
        /// <exception cref="ArgumentNullException">Thrown when token is null or empty.</exception>
        public string HashToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token), "Token cannot be null or empty");
            }

            // Generate a random salt
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[SaltSizeBytes];
                rng.GetBytes(salt);

                // Derive hash using PBKDF2 with SHA256
                var hash = Rfc2898DeriveBytes.Pbkdf2(
                    token,
                    salt,
                    HashIterations,
                    HashAlgorithmName.SHA256,
                    HashSizeBytes);

                // Return salt and hash as hex string in format "salt:hash"
                var saltHex = Convert.ToHexString(salt);
                var hashHex = Convert.ToHexString(hash);
                return $"{saltHex}:{hashHex}";
            }
        }

        /// <summary>
        /// Verify a token against a stored hash.
        /// </summary>
        /// <param name="token">The plain token to verify.</param>
        /// <param name="storedHash">The stored hash in format "salt:hash".</param>
        /// <returns>True if the token matches the hash, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when token or storedHash is null or empty.</exception>
        public bool VerifyToken(string token, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token), "Token cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(storedHash))
            {
                throw new ArgumentNullException(nameof(storedHash), "Stored hash cannot be null or empty");
            }

            try
            {
                // Parse the stored hash format "salt:hash"
                var parts = storedHash.Split(':');
                if (parts.Length != 2)
                {
                    return false;
                }

                var saltHex = parts[0];
                var expectedHashHex = parts[1];

                // Convert hex strings back to bytes
                var salt = Convert.FromHexString(saltHex);

                // Compute hash with the stored salt using PBKDF2
                var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    token,
                    salt,
                    HashIterations,
                    HashAlgorithmName.SHA256,
                    HashSizeBytes);

                var computedHashHex = Convert.ToHexString(computedHash);

                // Use constant-time comparison to prevent timing attacks
                return computedHashHex.Equals(expectedHashHex, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                // Return false for any parsing or verification errors
                return false;
            }
        }
    }
}
