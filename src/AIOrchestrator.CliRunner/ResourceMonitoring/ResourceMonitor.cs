using System.Diagnostics;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.CliRunner.ResourceMonitoring;

/// <summary>
/// Monitors system resources using System.Diagnostics.
/// </summary>
public class ResourceMonitor : IResourceMonitor
{
    private readonly int _maxProcesses;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _memoryCounter;

    public ResourceMonitor(int maxProcesses = 10)
    {
        _maxProcesses = maxProcesses;

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes", readOnly: true);
        }
        catch
        {
            _cpuCounter = null;
            _memoryCounter = null;
        }
    }

    public async Task<SystemResources> GetSystemResourcesAsync()
    {
        return await Task.Run(() =>
        {
            var cpuUsage = GetCpuUsage();
            var availableMemory = GetAvailableMemory();
            var processCount = GetRunningProcessCount();

            return new SystemResources
            {
                CpuUsagePercent = cpuUsage,
                AvailableMemoryMb = availableMemory,
                RunningProcessCount = processCount,
                MaxProcessesAllowed = _maxProcesses
            };
        });
    }

    private int GetCpuUsage()
    {
        try
        {
            if (_cpuCounter == null)
                return 50;

            var value = (int)_cpuCounter.NextValue();
            return Math.Clamp(value, 0, 100);
        }
        catch
        {
            return 50;
        }
    }

    private int GetAvailableMemory()
    {
        try
        {
            if (_memoryCounter == null)
            {
                var memInfo = GC.GetTotalMemory(false);
                return (int)(memInfo / (1024 * 1024));
            }

            var value = (int)_memoryCounter.NextValue();
            return Math.Max(value, 0);
        }
        catch
        {
            return 1024;
        }
    }

    private int GetRunningProcessCount()
    {
        try
        {
            var dotnetProcesses = Process.GetProcessesByName("dotnet");
            return dotnetProcesses.Length;
        }
        catch
        {
            return 1;
        }
    }
}
