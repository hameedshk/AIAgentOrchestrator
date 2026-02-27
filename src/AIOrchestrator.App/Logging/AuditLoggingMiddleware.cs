using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AIOrchestrator.App.Logging
{
    /// <summary>
    /// Middleware that captures all incoming requests and responses for audit logging.
    /// Logs device name, IP, response status, and timing information.
    /// </summary>
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public AuditLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AuditLogger auditLogger, ILogger<AuditLoggingMiddleware> logger)
        {
            var stopwatch = Stopwatch.StartNew();

            // Capture original response stream
            var originalResponseBody = context.Response.Body;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Extract authentication context
                var deviceName = context.Items.TryGetValue("DeviceName", out var device) ? device as string : "unknown";
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Create audit log entry
                var auditEntry = new AuditLogEntry
                {
                    HttpMethod = context.Request.Method,
                    RequestPath = context.Request.Path.Value ?? string.Empty,
                    DeviceName = deviceName,
                    IpAddress = ipAddress,
                    ResponseStatusCode = context.Response.StatusCode,
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTimeOffset.UtcNow
                };

                // Log audit entry asynchronously
                _ = auditLogger.LogAsync(auditEntry);
            }
        }
    }
}
