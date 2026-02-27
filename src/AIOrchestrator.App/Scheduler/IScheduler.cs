using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.App.Scheduler;

public interface IScheduler
{
    /// <summary>
    /// Enqueue a task for scheduling (typically called when task enters Queued state).
    /// </summary>
    Task EnqueueAsync(OrchestratorTask task);

    /// <summary>
    /// Dispatch next eligible task respecting resource limits and project isolation.
    /// Returns null if no eligible task or resources exhausted.
    /// </summary>
    Task<OrchestratorTask?> DispatchAsync(int cpuAvailable, int memoryAvailableMb, int maxProcesses);

    /// <summary>
    /// Mark a project as currently executing (enforce mutual exclusion).
    /// </summary>
    Task MarkRunningAsync(string projectId);

    /// <summary>
    /// Mark a project as complete (frees it for next task).
    /// </summary>
    Task MarkCompleteAsync(string projectId);
}
