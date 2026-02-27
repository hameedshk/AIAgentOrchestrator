using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Middleware that validates Bearer tokens on all incoming requests.
    /// Requests without valid Bearer tokens are rejected with 401 Unauthorized.
    /// </summary>
    public class BearerTokenAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public BearerTokenAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITokenStore tokenStore, ILogger<BearerTokenAuthMiddleware> logger = null)
        {
            // Skip auth for health check endpoints
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/status"))
            {
                await _next(context);
                return;
            }

            var authHeader = context.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogWarning("Request rejected: missing or invalid Authorization header from {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid Authorization header" });
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            if (!tokenStore.ValidateToken(token, out var deviceName))
            {
                logger?.LogWarning("Request rejected: invalid Bearer token from {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid Bearer token" });
                return;
            }

            // Store device context for logging
            context.Items["DeviceName"] = deviceName;
            context.Items["Token"] = token;

            logger?.LogInformation("Request authenticated for device {DeviceName}", deviceName);
            await _next(context);
        }
    }
}
