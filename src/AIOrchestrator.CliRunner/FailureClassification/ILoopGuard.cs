namespace AIOrchestrator.CliRunner.FailureClassification;

/// <summary>
/// Guards against infinite retry loops per spec Section 8.3.
/// Enforces limits on:
/// - Retries per individual step (maxRetriesPerStep)
/// - Total loops across all steps (maxLoopsPerTask)
/// - Same error fingerprint on consecutive retries
/// </summary>
public interface ILoopGuard
{
    /// <summary>
    /// Determine if another retry is allowed for a step.
    /// </summary>
    /// <param name="taskId">Task being retried</param>
    /// <param name="stepIndex">Step index</param>
    /// <param name="retryCount">Current retry count for this step</param>
    /// <param name="errorHash">SHA256 hash of normalized error output</param>
    /// <param name="maxRetriesPerStep">Hard limit per step (default: 3)</param>
    /// <param name="maxLoopsPerTask">Hard limit for task (default: 10)</param>
    /// <returns>True if retry is allowed; false if limit reached or same error detected</returns>
    bool CanRetry(
        Guid taskId,
        int stepIndex,
        int retryCount,
        string errorHash,
        int maxRetriesPerStep,
        int maxLoopsPerTask);

    /// <summary>
    /// Clear retry state for a completed or cancelled task.
    /// </summary>
    void Reset(Guid taskId);
}
