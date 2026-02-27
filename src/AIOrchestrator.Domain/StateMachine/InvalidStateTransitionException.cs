using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Domain.Exceptions;

namespace AIOrchestrator.Domain.StateMachine;

public sealed class InvalidStateTransitionException(TaskState from, TaskState to)
    : DomainException($"Invalid state transition from '{from}' to '{to}'.")
{
    public TaskState From { get; } = from;
    public TaskState To { get; } = to;
}
