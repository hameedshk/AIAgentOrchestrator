using System.Collections.Generic;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Thread-safe in-memory implementation of token storage.
    /// For production, implement with database persistence.
    /// </summary>
    public class InMemoryTokenStore : ITokenStore
    {
        private readonly Dictionary<string, string> _tokens = new();
        private readonly object _lock = new();

        /// <summary>
        /// Validate a token and retrieve the associated device name.
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <param name="deviceName">Output: the device name if token is valid</param>
        /// <returns>True if token is valid and found; false otherwise</returns>
        public bool ValidateToken(string? token, out string? deviceName)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                deviceName = null;
                return false;
            }

            lock (_lock)
            {
                return _tokens.TryGetValue(token, out deviceName);
            }
        }

        /// <summary>
        /// Store a token with its associated device name.
        /// </summary>
        /// <param name="token">The token to store</param>
        /// <param name="deviceName">The device name associated with the token</param>
        /// <exception cref="ArgumentException">Thrown if token or deviceName is null or empty</exception>
        public void StoreToken(string token, string deviceName)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token cannot be null or empty", nameof(token));
            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentException("Device name cannot be null or empty", nameof(deviceName));

            lock (_lock)
            {
                _tokens[token] = deviceName;
            }
        }
    }
}
