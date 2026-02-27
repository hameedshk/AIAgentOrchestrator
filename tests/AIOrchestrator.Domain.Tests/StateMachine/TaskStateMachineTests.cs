using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Domain.StateMachine;
using FluentAssertions;

namespace AIOrchestrator.Domain.Tests.StateMachine;

public class TaskStateMachineTests
{
    [Theory]
    [InlineData(TaskState.Created, TaskState.Queued)]
    [InlineData(TaskState.Created, TaskState.Cancelled)]
    [InlineData(TaskState.Queued, TaskState.Planning)]
    [InlineData(TaskState.Queued, TaskState.Cancelled)]
    [InlineData(TaskState.Planning, TaskState.AwaitingPlanApproval)]
    [InlineData(TaskState.Planning, TaskState.Failed)]
    [InlineData(TaskState.AwaitingPlanApproval, TaskState.Executing)]
    [InlineData(TaskState.AwaitingPlanApproval, TaskState.Cancelled)]
    [InlineData(TaskState.Executing, TaskState.AwaitingUserFix)]
    [InlineData(TaskState.Executing, TaskState.Retrying)]
    [InlineData(TaskState.Executing, TaskState.Completed)]
    [InlineData(TaskState.Executing, TaskState.Failed)]
    [InlineData(TaskState.Executing, TaskState.Halted)]
    [InlineData(TaskState.AwaitingUserFix, TaskState.Retrying)]
    [InlineData(TaskState.AwaitingUserFix, TaskState.Cancelled)]
    [InlineData(TaskState.Retrying, TaskState.Executing)]
    [InlineData(TaskState.Retrying, TaskState.Halted)]
    [InlineData(TaskState.Retrying, TaskState.Failed)]
    [InlineData(TaskState.Paused, TaskState.Queued)]
    [InlineData(TaskState.Paused, TaskState.Cancelled)]
    public void Valid_transitions_do_not_throw(TaskState from, TaskState to)
    {
        var act = () => TaskStateMachine.ValidateTransition(from, to);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(TaskState.Completed, TaskState.Queued)]
    [InlineData(TaskState.Failed, TaskState.Executing)]
    [InlineData(TaskState.Halted, TaskState.Retrying)]
    [InlineData(TaskState.Cancelled, TaskState.Planning)]
    [InlineData(TaskState.Created, TaskState.Executing)]
    [InlineData(TaskState.Executing, TaskState.Planning)]
    public void Invalid_transitions_throw_InvalidStateTransitionException(TaskState from, TaskState to)
    {
        var act = () => TaskStateMachine.ValidateTransition(from, to);
        act.Should().Throw<InvalidStateTransitionException>()
           .Which.From.Should().Be(from);
    }

    [Theory]
    [InlineData(TaskState.Completed)]
    [InlineData(TaskState.Failed)]
    [InlineData(TaskState.Halted)]
    [InlineData(TaskState.Cancelled)]
    public void Terminal_states_are_identified_correctly(TaskState state)
    {
        TaskStateMachine.IsTerminal(state).Should().BeTrue();
    }

    [Fact]
    public void Non_terminal_states_return_false()
    {
        TaskStateMachine.IsTerminal(TaskState.Executing).Should().BeFalse();
    }
}
