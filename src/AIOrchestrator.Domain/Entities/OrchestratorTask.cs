using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Domain.StateMachine;

namespace AIOrchestrator.Domain.Entities;

public sealed class OrchestratorTask
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ProjectId { get; init; } = string.Empty;
    public string? Description { get; init; }
    public TaskState State { get; private set; } = TaskState.Created;
    public ModelType Planner { get; init; } = ModelType.Claude;
    public ModelType Executor { get; init; } = ModelType.Codex;
    public IReadOnlyList<ExecutionStep> Steps => _steps.AsReadOnly();
    public int RetryCount { get; private set; }
    public FailureContext? LastFailure { get; private set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }
    public int CurrentStepIndex { get; set; } = 0;

    /// <summary>
    /// Spec Section 4.5: When true, triggers Planner re-invocation on non-retryable Executor failures.
    /// </summary>
    public bool AllowReplan { get; set; } = false;

    /// <summary>
    /// Tracks how many times this task has been re-planned (reset per original task).
    /// Used to enforce max replan attempts (default 3).
    /// </summary>
    public int ReplanAttempts { get; set; } = 0;

    /// <summary>
    /// Task priority level for scheduler (High > Normal > Low).
    /// Used by scheduler for priority queue ordering.
    /// </summary>
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    /// <summary>
    /// When task was queued (Queued state entered). Used for aging algorithm.
    /// </summary>
    public DateTimeOffset? QueuedAt { get; set; }

    private readonly List<ExecutionStep> _steps = [];

    public void Enqueue()
    {
        QueuedAt = DateTimeOffset.UtcNow;
        Transition(TaskState.Queued);
    }
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

    /// <summary>
    /// Appends revised plan steps during re-planning without state transition.
    /// Used by ReplanningOrchestrator to add steps to ongoing execution.
    /// </summary>
    public void AppendRevisionPlanSteps(IEnumerable<ExecutionStep> revisedSteps)
    {
        _steps.AddRange(revisedSteps);
    }

    private void Transition(TaskState next)
    {
        TaskStateMachine.ValidateTransition(State, next);
        State = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
