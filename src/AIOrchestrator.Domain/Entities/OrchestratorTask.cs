using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Domain.StateMachine;

namespace AIOrchestrator.Domain.Entities;

public sealed class OrchestratorTask
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public TaskState State { get; private set; } = TaskState.Created;
    public ModelType Planner { get; init; } = ModelType.Claude;
    public ModelType Executor { get; init; } = ModelType.Codex;
    public IReadOnlyList<ExecutionStep> Steps => _steps.AsReadOnly();
    public int RetryCount { get; private set; }
    public FailureContext? LastFailure { get; private set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>
    /// Spec Section 4.5: When true, triggers Planner re-invocation on non-retryable Executor failures.
    /// </summary>
    public bool AllowReplan { get; set; } = false;

    /// <summary>
    /// Tracks how many times this task has been re-planned (reset per original task).
    /// Used to enforce max replan attempts (default 3).
    /// </summary>
    public int ReplanAttempts { get; set; } = 0;

    private readonly List<ExecutionStep> _steps = [];

    public void Enqueue()                    => Transition(TaskState.Queued);
    public void StartPlanning()              => Transition(TaskState.Planning);
    public void StartExecuting()             => Transition(TaskState.Executing);
    public void RequestUserFix(FailureContext f) { LastFailure = f; Transition(TaskState.AwaitingUserFix); }
    public void Retry()                      => Transition(TaskState.Retrying);
    public void ResumeExecuting()            => Transition(TaskState.Executing);
    public void Complete()                   => Transition(TaskState.Completed);
    public void Halt()                       => Transition(TaskState.Halted);
    public void Cancel()                     => Transition(TaskState.Cancelled);

    public void ApprovePlan(string planVersion, IEnumerable<ExecutionStep> steps)
    {
        Transition(TaskState.AwaitingPlanApproval);
        _steps.Clear();
        _steps.AddRange(steps);
    }

    public void Fail(FailureContext failure)
    {
        LastFailure = failure;
        Transition(TaskState.Failed);
    }

    private void Transition(TaskState next)
    {
        TaskStateMachine.ValidateTransition(State, next);
        State = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
