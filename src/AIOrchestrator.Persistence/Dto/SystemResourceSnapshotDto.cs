using System.Text.Json.Serialization;

namespace AIOrchestrator.Persistence.Dto;

/// <summary>
/// Historical snapshot of system resources at time of task dispatch.
/// </summary>
public sealed class SystemResourceSnapshotDto
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("cpuUsagePercent")]
    public int CpuUsagePercent { get; set; }

    [JsonPropertyName("availableMemoryMb")]
    public int AvailableMemoryMb { get; set; }

    [JsonPropertyName("runningProcessCount")]
    public int RunningProcessCount { get; set; }
}
