using System;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Provides secure token hashing and verification functionality.
    /// </summary>
    public interface ITokenHasher
    {
        /// <summary>
        /// Hash a token for secure storage.
        /// </summary>
        /// <param name="token">The plain token to hash.</param>
        /// <returns>The hashed token.</returns>
        /// <exception cref="ArgumentNullException">Thrown when token is null or empty.</exception>
        string HashToken(string token);

        /// <summary>
        /// Verify a token against a stored hash.
        /// </summary>
        /// <param name="token">The plain token to verify.</param>
        /// <param name="storedHash">The stored hash to verify against.</param>
        /// <returns>True if the token matches the hash, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when token or storedHash is null or empty.</exception>
        bool VerifyToken(string token, string storedHash);
    }
}
