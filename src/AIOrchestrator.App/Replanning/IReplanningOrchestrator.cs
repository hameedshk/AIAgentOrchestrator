using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.App.Replanning;

/// <summary>
/// Orchestrates the re-planning flow when Executor encounters non-retryable failures.
/// Spec Section 4.5: Calls Planner again with original task, completed steps, failed step, and failure context.
/// Planner produces a revised plan for remaining steps only.
/// </summary>
public interface IReplanningOrchestrator
{
    /// <summary>
    /// Execute re-planning when a step fails non-retryably.
    /// </summary>
    /// <param name="task">Task being executed</param>
    /// <param name="failedStep">The step that failed</param>
    /// <param name="failureContext">Failure details from Phase 5 classification</param>
    /// <returns>Updated task with revised plan steps appended</returns>
    Task<OrchestratorTask> ReplanAsync(
        OrchestratorTask task,
        ExecutionStep failedStep,
        FailureContext failureContext,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a task is eligible for re-planning.
    /// </summary>
    bool CanReplan(OrchestratorTask task, FailureContext failureContext);
}
