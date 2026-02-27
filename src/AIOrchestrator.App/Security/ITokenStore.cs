namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Interface for token storage and validation. Implement for persistent storage.
    /// </summary>
    public interface ITokenStore
    {
        /// <summary>
        /// Validate a token and retrieve the associated device name.
        /// </summary>
        /// <param name="token">The token to validate</param>
        /// <param name="deviceName">The device name associated with the token, if valid</param>
        /// <returns>True if token is valid, false otherwise</returns>
        bool ValidateToken(string? token, out string? deviceName);

        /// <summary>
        /// Store a token with an associated device name.
        /// </summary>
        /// <param name="token">The token to store</param>
        /// <param name="deviceName">The device name to associate with the token</param>
        void StoreToken(string token, string deviceName);
    }
}
