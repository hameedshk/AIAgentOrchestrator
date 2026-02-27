using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.App.CrashRecovery;

/// <summary>
/// Coordinates recovery of individual tasks from crash state.
/// Spec Section 9.3: Reset to last completed step, restore git, rehydrate CLI.
/// </summary>
public interface ITaskRecoveryCoordinator
{
    /// <summary>
    /// Recover a single task from persisted crash state.
    /// </summary>
    /// <param name="task">Task that was interrupted at crash</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task in recovered state ready for execution</returns>
    Task<OrchestratorTask> RecoverTaskAsync(OrchestratorTask task, CancellationToken ct = default);

    /// <summary>
    /// Identify tasks requiring recovery (those in Executing/Planning state).
    /// </summary>
    /// <param name="allTasks">All persisted tasks</param>
    /// <returns>Tasks that need recovery</returns>
    IReadOnlyList<OrchestratorTask> IdentifyRecoveringTasks(IReadOnlyList<OrchestratorTask> allTasks);
}
