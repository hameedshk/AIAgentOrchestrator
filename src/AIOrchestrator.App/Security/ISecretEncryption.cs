namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Interface for secret encryption and decryption.
    /// Secrets are encrypted at rest using this service.
    /// </summary>
    public interface ISecretEncryption
    {
        /// <summary>
        /// Encrypt plaintext secret.
        /// </summary>
        string Encrypt(string plaintext);

        /// <summary>
        /// Decrypt encrypted secret.
        /// </summary>
        string Decrypt(string ciphertext);
    }
}
