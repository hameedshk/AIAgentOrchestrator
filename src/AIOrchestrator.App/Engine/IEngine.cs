using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Engine;

/// <summary>
/// Central orchestration engine coordinating Scheduler, Resources, and Task Execution.
/// </summary>
public interface IEngine
{
    /// <summary>
    /// Start the engine dispatch loop (runs continuously until stopped).
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a new task to the engine for execution.
    /// </summary>
    Task<OrchestratorTask> SubmitTaskAsync(OrchestratorTask task);

    /// <summary>
    /// Get all tasks with specified state.
    /// </summary>
    Task<IReadOnlyList<OrchestratorTask>> GetTasksByStateAsync(TaskState state);

    /// <summary>
    /// Get current engine status and resource snapshot.
    /// </summary>
    Task<EngineStatus> GetStatusAsync();
}

/// <summary>
/// Real-time engine status.
/// </summary>
public sealed class EngineStatus
{
    public int TotalTasks { get; init; }
    public int QueuedTasks { get; init; }
    public int ExecutingTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int FailedTasks { get; init; }
    public int CpuUsagePercent { get; init; }
    public int AvailableMemoryMb { get; init; }
    public int RunningProcessCount { get; init; }
    public DateTimeOffset LastDispatchTime { get; init; }
}
