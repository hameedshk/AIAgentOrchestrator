using System;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Default implementation of ISystemClock that returns actual system time.
    /// </summary>
    public class SystemClock : ISystemClock
    {
        /// <summary>
        /// Get the current UTC time.
        /// </summary>
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
