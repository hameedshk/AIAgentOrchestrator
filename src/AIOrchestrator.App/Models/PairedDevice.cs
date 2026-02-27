using System;

namespace AIOrchestrator.App.Models
{
    public class PairedDevice
    {
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string TokenHash { get; set; }
        public DateTimeOffset PairedAt { get; set; }
        public DateTimeOffset LastAccessAt { get; set; }
        public bool IsActive { get; set; }
    }
}
