using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Domain.StateMachine;
using FluentAssertions;

namespace AIOrchestrator.Domain.Tests.StateMachine;

public class InvalidStateTransitionExceptionTests
{
    [Fact]
    public void Message_describes_the_invalid_transition()
    {
        var ex = new InvalidStateTransitionException(TaskState.Completed, TaskState.Queued);
        ex.Message.Should().Contain("Completed").And.Contain("Queued");
        ex.From.Should().Be(TaskState.Completed);
        ex.To.Should().Be(TaskState.Queued);
    }
}
