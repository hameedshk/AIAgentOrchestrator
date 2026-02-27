using System.Collections.Generic;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// In-memory token store. For production, implement with database persistence.
    /// </summary>
    public class InMemoryTokenStore : ITokenStore
    {
        private readonly Dictionary<string, string> _tokens = new();
        private readonly object _lock = new();

        public bool ValidateToken(string token, out string deviceName)
        {
            lock (_lock)
            {
                return _tokens.TryGetValue(token, out deviceName);
            }
        }

        public void StoreToken(string token, string deviceName)
        {
            lock (_lock)
            {
                _tokens[token] = deviceName;
            }
        }
    }
}
