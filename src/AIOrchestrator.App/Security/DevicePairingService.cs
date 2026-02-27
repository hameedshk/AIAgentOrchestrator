using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Manages one-time secure token exchange for device pairing on first connection.
    /// Tokens are time-limited and single-use.
    /// </summary>
    public class DevicePairingService
    {
        private readonly int _tokenExpirationMinutes;
        private readonly Dictionary<string, PairingTokenEntry> _pairingTokens; // In-memory store; upgrade to persistent for production
        private readonly TokenHasher _hasher;

        public DevicePairingService(int tokenExpirationMinutes = 15)
        {
            _tokenExpirationMinutes = tokenExpirationMinutes;
            _pairingTokens = new Dictionary<string, PairingTokenEntry>();
            _hasher = new TokenHasher();
        }

        /// <summary>
        /// Generate a unique, cryptographically secure pairing token.
        /// </summary>
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
        public void StorePairingToken(string token, string deviceName)
        {
            var hashedToken = _hasher.HashToken(token);
            _pairingTokens[hashedToken] = new PairingTokenEntry
            {
                DeviceName = deviceName,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_tokenExpirationMinutes),
                IsUsed = false
            };
        }

        /// <summary>
        /// Validate a pairing token and mark it as used if valid.
        /// </summary>
        public bool ValidatePairingToken(string token, string deviceName)
        {
            var hashedToken = _hasher.HashToken(token);

            if (!_pairingTokens.ContainsKey(hashedToken))
                return false;

            var entry = _pairingTokens[hashedToken];

            // Check expiration
            if (DateTimeOffset.UtcNow > entry.ExpiresAt)
            {
                _pairingTokens.Remove(hashedToken);
                return false;
            }

            // Check device name match
            if (entry.DeviceName != deviceName)
                return false;

            // Check if already used
            if (entry.IsUsed)
                return false;

            // Mark as used
            entry.IsUsed = true;
            return true;
        }

        /// <summary>
        /// Clean up expired tokens (call periodically).
        /// </summary>
        public void CleanupExpiredTokens()
        {
            var now = DateTimeOffset.UtcNow;
            var expiredTokens = _pairingTokens
                .Where(kvp => kvp.Value.ExpiresAt < now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _pairingTokens.Remove(token);
            }
        }

        private class PairingTokenEntry
        {
            public string DeviceName { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }
            public bool IsUsed { get; set; }
        }
    }
}
