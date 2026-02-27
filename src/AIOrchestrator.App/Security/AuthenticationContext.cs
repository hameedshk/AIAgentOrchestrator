using System;

namespace AIOrchestrator.App.Security
{
    /// <summary>
    /// Encapsulates authentication context extracted from Bearer token.
    /// </summary>
    public class AuthenticationContext
    {
        /// <summary>
        /// The authenticated device name.
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// When the device was authenticated.
        /// </summary>
        public DateTimeOffset AuthenticatedAt { get; set; }

        /// <summary>
        /// Remote IP address of the client.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Extract authentication context from HttpContext items.
        /// </summary>
        public static AuthenticationContext FromHttpContext(Microsoft.AspNetCore.Http.HttpContext context)
        {
            if (context == null) return null;

            if (!context.Items.TryGetValue("DeviceName", out var deviceName) ||
                !context.Items.TryGetValue("AuthenticatedAt", out var authenticatedAt))
            {
                return null;
            }

            return new AuthenticationContext
            {
                DeviceName = (string)deviceName,
                AuthenticatedAt = (DateTimeOffset)authenticatedAt,
                IpAddress = context.Connection.RemoteIpAddress?.ToString()
            };
        }
    }
}
