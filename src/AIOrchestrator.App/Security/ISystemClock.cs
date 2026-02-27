using System;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Provides abstraction over system time for testability.
    /// </summary>
    public interface ISystemClock
    {
        /// <summary>
        /// Get the current UTC time.
        /// </summary>
        DateTimeOffset UtcNow { get; }
    }
}
