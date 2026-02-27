namespace AIOrchestrator.CliRunner.FailureClassification;

/// <summary>
/// Guards against infinite retry loops per spec Section 8.3.
/// Enforces limits on:
/// - Retries per individual step (maxRetriesPerStep)
/// - Total loops across all steps (maxLoopsPerTask)
/// - Same error fingerprint on consecutive retries
/// </summary>
public class LoopGuard : ILoopGuard
{
    /// <summary>
    /// Tracks per-task retry state.
    /// </summary>
    private class PerTaskState
    {
        /// <summary>
        /// Maps step index to last error hash only.
        /// </summary>
        public Dictionary<int, string> StepLastErrorHash { get; } = new();

        /// <summary>
        /// Total number of loop attempts across all steps.
        /// </summary>
        public int TotalLoopCount { get; set; }
    }

    /// <summary>
    /// Tracks state for each task by taskId.
    /// </summary>
    private readonly Dictionary<Guid, PerTaskState> _taskStates = new();

    /// <summary>
    /// Determine if another retry is allowed for a step.
    /// </summary>
    public bool CanRetry(
        Guid taskId,
        int stepIndex,
        int retryCount,
        string errorHash,
        int maxRetriesPerStep,
        int maxLoopsPerTask)
    {
        // Get or create task state
        if (!_taskStates.ContainsKey(taskId))
        {
            _taskStates[taskId] = new PerTaskState();
        }

        var taskState = _taskStates[taskId];

        // Get previous error hash for this step (if any)
        var previousErrorHash = taskState.StepLastErrorHash.GetValueOrDefault(stepIndex, "");

        // Check 1: Exceeded max retries for this step
        // Allow retries 0, 1, ..., (maxRetriesPerStep-1)
        // Deny at maxRetriesPerStep and beyond
        if (retryCount >= maxRetriesPerStep)
        {
            return false;
        }

        // Check 2: Same error hash on consecutive retries = error dedup
        // Block if we're retrying (retryCount > 0) with the same error
        if (retryCount > 0 && errorHash == previousErrorHash && previousErrorHash != "")
        {
            return false;
        }

        // Check 3: Track loop count
        // Increment when entering a new retry attempt (changing hash or first time at step)
        if (retryCount == 0)
        {
            // First attempt at this step, increment loop count
            taskState.TotalLoopCount++;
        }
        else if (errorHash != previousErrorHash)
        {
            // Different error, counts as new loop
            taskState.TotalLoopCount++;
        }

        // Check 4: Exceeded max total loops for task
        if (taskState.TotalLoopCount > maxLoopsPerTask)
        {
            return false;
        }

        // Update state before returning
        taskState.StepLastErrorHash[stepIndex] = errorHash;

        return true;
    }

    /// <summary>
    /// Clear retry state for a completed or cancelled task.
    /// </summary>
    public void Reset(Guid taskId)
    {
        _taskStates.Remove(taskId);
    }
}
