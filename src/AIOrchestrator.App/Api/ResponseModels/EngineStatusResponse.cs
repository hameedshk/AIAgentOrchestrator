namespace AIOrchestrator.App.Api.ResponseModels;

public sealed class EngineStatusResponse
{
    public int TotalTasks { get; set; }
    public int QueuedTasks { get; set; }
    public int ExecutingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int CpuUsagePercent { get; set; }
    public int AvailableMemoryMb { get; set; }
    public int RunningProcessCount { get; set; }
    public DateTimeOffset LastDispatchTime { get; set; }
}
