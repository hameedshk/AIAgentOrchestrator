using System;
using System.Collections.Generic;

namespace AIOrchestrator.App.Logging
{
    /// <summary>
    /// Structured audit log entry for all remote API operations.
    /// Captures request context, response, and device information.
    /// </summary>
    public class AuditLogEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? HttpMethod { get; set; }
        public string? RequestPath { get; set; }
        public string? DeviceName { get; set; }
        public string? IpAddress { get; set; }
        public int ResponseStatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public string? RequestUserId { get; set; }
        public Dictionary<string, object> AdditionalContext { get; set; } = new();

        /// <summary>
        /// Convert to JSON for logging.
        /// </summary>
        public override string ToString()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }
    }
}
