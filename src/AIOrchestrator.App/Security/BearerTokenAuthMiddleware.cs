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

        /// <summary>
        /// Validates Bearer token on incoming request and stores authentication context.
        /// </summary>
        /// <param name="context">The HTTP context</param>
        /// <param name="tokenStore">Token store for validation</param>
        /// <param name="logger">Logger instance</param>
        public async Task InvokeAsync(HttpContext context, ITokenStore tokenStore, ILogger<BearerTokenAuthMiddleware> logger = null)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (tokenStore == null) throw new ArgumentNullException(nameof(tokenStore));

            // Skip auth for health check endpoints
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/api/health") ||
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

            // Validate header length before extracting token
            if (authHeader.Length <= "Bearer ".Length)
            {
                logger?.LogWarning("Request rejected: invalid Bearer token format from {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid Authorization header format" });
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Validate token is not empty after trimming
            if (string.IsNullOrWhiteSpace(token) || !tokenStore.ValidateToken(token, out var deviceName))
            {
                logger?.LogWarning("Request rejected: invalid Bearer token from {RemoteIp}", context.Connection.RemoteIpAddress);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid Bearer token" });
                return;
            }

            // Store authentication context for logging (NOT the token itself)
            context.Items["DeviceName"] = deviceName;
            context.Items["AuthenticatedAt"] = DateTimeOffset.UtcNow;

            logger?.LogInformation("Request authenticated for device {DeviceName}", deviceName);
            await _next(context);
        }
    }
}
