using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AIOrchestrator.App.Logging
{
    /// <summary>
    /// Writes audit log entries to audit.log file in JSON format.
    /// Append-only log ensures no entries are modified or deleted.
    /// </summary>
    public class AuditLogger
    {
        private readonly string _logPath;
        private readonly ILogger<AuditLogger> _logger;
        private readonly object _fileLock = new();

        public AuditLogger(IConfiguration config, ILogger<AuditLogger> logger)
        {
            _logger = logger;

            var logDir = config["Logging:LogDirectory"] ?? "data/logs";
            Directory.CreateDirectory(logDir);
            _logPath = Path.Combine(logDir, "audit.log");
        }

        /// <summary>
        /// Log an audit entry. Thread-safe append-only operation.
        /// </summary>
        public async Task LogAsync(AuditLogEntry entry)
        {
            try
            {
                var json = entry.ToString();

                lock (_fileLock)
                {
                    File.AppendAllText(_logPath, json + Environment.NewLine);
                }

                _logger.LogInformation("Audit logged: {OperationId} - {Method} {Path}",
                    entry.Id, entry.HttpMethod, entry.RequestPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit log entry");
            }
        }

        /// <summary>
        /// Stream audit log entries (for dashboard real-time view).
        /// </summary>
        public IAsyncEnumerable<AuditLogEntry> StreamEntriesAsync(int lastNEntries = 100)
        {
            return StreamEntriesInternalAsync(lastNEntries);
        }

        private async IAsyncEnumerable<AuditLogEntry> StreamEntriesInternalAsync(int lastNEntries)
        {
            if (!File.Exists(_logPath))
                yield break;

            string[] lines = Array.Empty<string>();
            try
            {
                lines = await File.ReadAllLinesAsync(_logPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stream audit log entries");
                yield break;
            }

            // Return last N entries
            var startIndex = Math.Max(0, lines.Length - lastNEntries);
            for (int i = startIndex; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                AuditLogEntry? entry = null;
                try
                {
                    entry = System.Text.Json.JsonSerializer.Deserialize<AuditLogEntry>(lines[i]);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse audit log line");
                }

                if (entry != null)
                    yield return entry;
            }
        }
    }
}
