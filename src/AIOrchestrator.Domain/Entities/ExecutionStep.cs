using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.Domain.Entities;

public sealed class ExecutionStep
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int Index { get; init; }
    public StepType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Command { get; init; }
    public string? Prompt { get; init; }
    public string? ExpectedOutput { get; init; }
    public StepStatus Status { get; private set; } = StepStatus.Pending;
    public string? ActualOutput { get; private set; }
    public FailureContext? LastFailure { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastErrorHash { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Marks steps that came from a revised plan via re-planning.
    /// Used for debugging and understanding execution history.
    /// </summary>
    public bool IsFromReplan { get; set; } = false;

    public void MarkStarted()
    {
        Status = StepStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCompleted(string output)
    {
        Status = StepStatus.Completed;
        ActualOutput = output;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(FailureContext failure)
    {
        Status = StepStatus.Failed;
        LastFailure = failure;
        LastErrorHash = failure.ErrorHash;
        RetryCount++;
    }

    public void ResetForRetry()
    {
        Status = StepStatus.Pending;
        ActualOutput = null;
        StartedAt = null;
        CompletedAt = null;
    }
}
