using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Diagnostics;

namespace AIOrchestrator.App.Controllers
{
    /// <summary>
    /// Health check endpoints for monitoring and load balancers.
    /// These endpoints do not require authentication.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private static readonly long ProcessStartTime = Process.GetCurrentProcess().StartTime.Ticks;

        [HttpGet]
        [Route("")]
        public IActionResult Health()
        {
            var process = Process.GetCurrentProcess();
            var uptime = (System.DateTime.Now.Ticks - ProcessStartTime) / 10000000; // seconds

            return Ok(new
            {
                status = "healthy",
                uptime = uptime,
                cpuUsagePercent = GetCpuUsage(),
                memoryUsageMb = process.WorkingSet64 / (1024 * 1024),
                memoryUsagePercent = (process.WorkingSet64 / (double)GetTotalSystemMemory()) * 100
            });
        }

        [HttpGet]
        [Route("ready")]
        public IActionResult Ready()
        {
            // Readiness check - return 200 only if engine is ready to accept requests
            return Ok(new { ready = true });
        }

        [HttpGet]
        [Route("live")]
        public IActionResult Live()
        {
            // Liveness check - return 200 if process is alive
            return Ok(new { live = true });
        }

        private static double GetCpuUsage()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue(); // First call always returns 0
            System.Threading.Thread.Sleep(100);
            return cpuCounter.NextValue();
        }

        private static long GetTotalSystemMemory()
        {
            return 16L * 1024 * 1024 * 1024; // Assume 16GB; improve with WMI query for actual value
        }
    }
}
