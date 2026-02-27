using System;
using System.Security.Cryptography;
using System.Text;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Windows DPAPI-based secret encryption. Secrets are encrypted using the current Windows user context.
    /// This service is Windows-only and suitable for local-first execution.
    /// </summary>
    public class DpapiSecretEncryption : ISecretEncryption
    {
        private const DataProtectionScope _scope = DataProtectionScope.CurrentUser;

        /// <summary>
        /// Encrypt plaintext using DPAPI.
        /// </summary>
        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return string.Empty;

            try
            {
                var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                var encryptedBytes = ProtectedData.Protect(plaintextBytes, null, _scope);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("DPAPI encryption failed", ex);
            }
        }

        /// <summary>
        /// Decrypt DPAPI-encrypted ciphertext.
        /// </summary>
        public string Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext))
                return string.Empty;

            try
            {
                var encryptedBytes = Convert.FromBase64String(ciphertext);
                var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, _scope);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Invalid base64 format", ex);
            }
            catch (CryptographicException)
            {
                throw;
            }
        }
    }
}
