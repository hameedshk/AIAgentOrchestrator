using System;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Encapsulates authentication context extracted from Bearer token.
    /// </summary>
    public class AuthenticationContext
    {
        public string Token { get; set; }
        public string DeviceName { get; set; }
        public DateTimeOffset AuthenticatedAt { get; set; }
        public string IpAddress { get; set; }

        public static AuthenticationContext FromHttpContext(Microsoft.AspNetCore.Http.HttpContext context)
        {
            if (!context.Items.TryGetValue("Token", out var token) ||
                !context.Items.TryGetValue("DeviceName", out var deviceName))
            {
                return null;
            }

            return new AuthenticationContext
            {
                Token = (string)token,
                DeviceName = (string)deviceName,
                AuthenticatedAt = DateTimeOffset.UtcNow,
                IpAddress = context.Connection.RemoteIpAddress?.ToString()
            };
        }
    }
}
