using System;
using System.ComponentModel.DataAnnotations;

namespace AIOrchestrator.App.Models
{
    /// <summary>
    /// Represents a device that has been successfully paired with the system.
    /// </summary>
    public class PairedDevice
    {
        /// <summary>
        /// Gets or sets the unique identifier for the paired device.
        /// </summary>
        [Required]
        public required string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the human-readable name of the device.
        /// </summary>
        [Required]
        [StringLength(256)]
        public required string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the hashed pairing token.
        /// </summary>
        [Required]
        public required string TokenHash { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the device was paired.
        /// </summary>
        public DateTimeOffset PairedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last access from this device.
        /// </summary>
        public DateTimeOffset LastAccessAt { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether the device is currently active/enabled.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
