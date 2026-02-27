namespace AIOrchestrator.CliRunner.Abstractions;

/// <summary>
/// Snapshot of current system resources.
/// </summary>
public sealed class SystemResources
{
    /// <summary>
    /// Current CPU usage as percentage (0-100).
    /// </summary>
    public int CpuUsagePercent { get; init; }

    /// <summary>
    /// Available system memory in megabytes.
    /// </summary>
    public int AvailableMemoryMb { get; init; }

    /// <summary>
    /// Number of CLI processes currently running.
    /// </summary>
    public int RunningProcessCount { get; init; }

    /// <summary>
    /// Maximum concurrent CLI processes allowed (configuration).
    /// </summary>
    public int MaxProcessesAllowed { get; init; }

    /// <summary>
    /// Checks if resources are available for new task execution.
    /// </summary>
    public bool HasResourcesAvailable(int cpuThresholdPercent, int memoryThresholdMb)
    {
        return CpuUsagePercent < cpuThresholdPercent &&
               AvailableMemoryMb > memoryThresholdMb &&
               RunningProcessCount < MaxProcessesAllowed;
    }
}
