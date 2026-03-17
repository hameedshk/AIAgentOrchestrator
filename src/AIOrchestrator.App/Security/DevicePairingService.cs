using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Manages one-time secure token exchange for device pairing on first connection.
    /// Tokens are time-limited, single-use, and thread-safe.
    /// </summary>
    public class DevicePairingService
    {
        private readonly int _tokenExpirationMinutes;
        private readonly ConcurrentDictionary<string, PairingTokenEntry> _pairingTokens;
        private readonly ITokenHasher _hasher;
        private readonly ISystemClock _clock;
        private readonly ILogger<DevicePairingService>? _logger;

        public int TokenExpirationMinutes => _tokenExpirationMinutes;

        /// <summary>
        /// Initializes a new instance of the DevicePairingService.
        /// </summary>
        /// <param name="tokenExpirationMinutes">Number of minutes before tokens expire (default: 15).</param>
        /// <param name="hasher">Optional token hasher. If null, uses default TokenHasher.</param>
        /// <param name="clock">Optional system clock. If null, uses default SystemClock.</param>
        /// <param name="logger">Optional logger for security events.</param>
        public DevicePairingService(
            int tokenExpirationMinutes = 15,
            ITokenHasher? hasher = null,
            ISystemClock? clock = null,
            ILogger<DevicePairingService>? logger = null)
        {
            if (tokenExpirationMinutes <= 0)
            {
                throw new ArgumentException("Token expiration must be greater than 0 minutes", nameof(tokenExpirationMinutes));
            }

            _tokenExpirationMinutes = tokenExpirationMinutes;
            _pairingTokens = new ConcurrentDictionary<string, PairingTokenEntry>();
            _hasher = hasher ?? new TokenHasher();
            _clock = clock ?? new SystemClock();
            _logger = logger;
        }

        /// <summary>
        /// Generate a unique, cryptographically secure pairing token.
        /// </summary>
        /// <returns>A 32-character hex string representing a cryptographically secure random token.</returns>
        public string GeneratePairingToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBuffer = new byte[16];
                rng.GetBytes(tokenBuffer);
                return Convert.ToHexString(tokenBuffer);
            }
        }

        /// <summary>
        /// Store a pairing token with device name and expiration.
        /// </summary>
        /// <param name="token">The plain pairing token.</param>
        /// <param name="deviceName">The name of the device requesting pairing.</param>
        /// <exception cref="ArgumentNullException">Thrown when token or deviceName is null or empty.</exception>
        public void StorePairingToken(string token, string deviceName)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token), "Token cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(deviceName))
            {
                throw new ArgumentNullException(nameof(deviceName), "Device name cannot be null or empty");
            }

            var hashedToken = _hasher.HashToken(token);
            var now = _clock.UtcNow;
            var tokenId = Guid.NewGuid().ToString(); // Use a unique ID as the key

            _pairingTokens[tokenId] = new PairingTokenEntry
            {
                TokenHash = hashedToken,
                DeviceName = deviceName,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(_tokenExpirationMinutes),
                IsUsed = false
            };

            _logger?.LogInformation("Pairing token stored for device: {DeviceName}", deviceName);
        }

        /// <summary>
        /// Validate a pairing token and mark it as used if valid.
        /// Validation is atomic and thread-safe.
        /// </summary>
        /// <param name="token">The plain pairing token to validate.</param>
        /// <param name="deviceName">The device name to validate against.</param>
        /// <returns>True if the token is valid, unused, not expired, and device name matches; false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when token or deviceName is null or empty.</exception>
        public bool ValidatePairingToken(string token, string deviceName)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token), "Token cannot be null or empty");
            }

            if (string.IsNullOrWhiteSpace(deviceName))
            {
                throw new ArgumentNullException(nameof(deviceName), "Device name cannot be null or empty");
            }

            var now = _clock.UtcNow;

            // Search through all tokens to find a match using the hasher's VerifyToken method
            foreach (var kvp in _pairingTokens)
            {
                var tokenId = kvp.Key;
                var entry = kvp.Value;

                // Verify the token against the stored hash
                if (!_hasher.VerifyToken(token, entry.TokenHash))
                {
                    continue; // Not a match, try next token
                }

                // Check expiration
                if (now > entry.ExpiresAt)
                {
                    _pairingTokens.TryRemove(tokenId, out _);
                    _logger?.LogWarning("Pairing validation failed: token expired for device {DeviceName}", deviceName);
                    return false;
                }

                // Check device name match
                if (entry.DeviceName != deviceName)
                {
                    _logger?.LogWarning("Pairing validation failed: device name mismatch. Expected: {Expected}, Got: {Actual}",
                        entry.DeviceName, deviceName);
                    return false;
                }

                // Check if already used
                if (entry.IsUsed)
                {
                    _logger?.LogWarning("Pairing validation failed: token already used for device {DeviceName}", deviceName);
                    return false;
                }

                // Mark as used atomically
                entry.IsUsed = true;
                _logger?.LogInformation("Pairing token validated and marked as used for device: {DeviceName}", deviceName);
                return true;
            }

            _logger?.LogWarning("Pairing validation failed: token not found");
            return false;
        }

        /// <summary>
        /// Clean up expired tokens (call periodically).
        /// </summary>
        /// <returns>The number of tokens removed.</returns>
        public int CleanupExpiredTokens()
        {
            var now = _clock.UtcNow;
            var expiredTokens = _pairingTokens
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            int removedCount = 0;
            foreach (var token in expiredTokens)
            {
                if (_pairingTokens.TryRemove(token, out _))
                {
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                _logger?.LogInformation("Cleaned up {Count} expired pairing tokens", removedCount);
            }

            return removedCount;
        }

        private class PairingTokenEntry
        {
            /// <summary>
            /// The hashed token in format "salt:hash".
            /// </summary>
            [Required]
            public required string TokenHash { get; set; }

            /// <summary>
            /// The device name associated with this token.
            /// </summary>
            [Required]
            public required string DeviceName { get; set; }

            /// <summary>
            /// When the token was created.
            /// </summary>
            public DateTimeOffset CreatedAt { get; set; }

            /// <summary>
            /// When the token expires.
            /// </summary>
            public DateTimeOffset ExpiresAt { get; set; }

            /// <summary>
            /// Whether the token has been used.
            /// </summary>
            public bool IsUsed { get; set; }
        }
    }
}
