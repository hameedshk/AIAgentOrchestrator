using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.Domain.StateMachine;

public static class TaskStateMachine
{
    private static readonly IReadOnlyDictionary<TaskState, IReadOnlySet<TaskState>> ValidTransitions =
        new Dictionary<TaskState, IReadOnlySet<TaskState>>
        {
            [TaskState.Created]              = new HashSet<TaskState> { TaskState.Queued, TaskState.Cancelled },
            [TaskState.Queued]               = new HashSet<TaskState> { TaskState.Planning, TaskState.Cancelled },
            [TaskState.Planning]             = new HashSet<TaskState> { TaskState.AwaitingPlanApproval, TaskState.Failed },
            [TaskState.AwaitingPlanApproval] = new HashSet<TaskState> { TaskState.Executing, TaskState.Cancelled },
            [TaskState.Executing]            = new HashSet<TaskState> { TaskState.AwaitingUserFix, TaskState.Retrying,
                                                                         TaskState.Completed, TaskState.Failed, TaskState.Halted },
            [TaskState.AwaitingUserFix]      = new HashSet<TaskState> { TaskState.Retrying, TaskState.Cancelled },
            [TaskState.Retrying]             = new HashSet<TaskState> { TaskState.Executing, TaskState.Halted, TaskState.Failed },
            [TaskState.Paused]               = new HashSet<TaskState> { TaskState.Queued, TaskState.Cancelled },
            [TaskState.Completed]            = new HashSet<TaskState>(),
            [TaskState.Failed]               = new HashSet<TaskState>(),
            [TaskState.Halted]               = new HashSet<TaskState>(),
            [TaskState.Cancelled]            = new HashSet<TaskState>(),
        };

    private static readonly IReadOnlySet<TaskState> TerminalStates =
        new HashSet<TaskState> { TaskState.Completed, TaskState.Failed, TaskState.Halted, TaskState.Cancelled };

    public static bool IsTerminal(TaskState state) => TerminalStates.Contains(state);

    public static bool CanTransition(TaskState from, TaskState to)
        => ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static void ValidateTransition(TaskState from, TaskState to)
    {
        if (!CanTransition(from, to))
            throw new InvalidStateTransitionException(from, to);
    }
}
