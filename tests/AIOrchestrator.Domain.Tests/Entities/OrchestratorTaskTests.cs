using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Domain.StateMachine;
using FluentAssertions;

namespace AIOrchestrator.Domain.Tests.Entities;

public class OrchestratorTaskTests
{
    private static OrchestratorTask NewTask() =>
        new() { Id = Guid.NewGuid(), Title = "Test task" };

    [Fact]
    public void New_task_starts_in_Created_state()
    {
        NewTask().State.Should().Be(TaskState.Created);
    }

    [Fact]
    public void Enqueue_transitions_Created_to_Queued()
    {
        var task = NewTask();
        task.Enqueue();
        task.State.Should().Be(TaskState.Queued);
    }

    [Fact]
    public void StartPlanning_transitions_Queued_to_Planning()
    {
        var task = NewTask();
        task.Enqueue();
        task.StartPlanning();
        task.State.Should().Be(TaskState.Planning);
    }

    [Fact]
    public void Complete_from_Executing_sets_terminal_state()
    {
        var task = NewTask();
        task.Enqueue();
        task.StartPlanning();
        task.ApprovePlan("1", Enumerable.Empty<ExecutionStep>());
        task.StartExecuting();
        task.Complete();
        task.State.Should().Be(TaskState.Completed);
    }

    [Fact]
    public void Invalid_transition_throws_InvalidStateTransitionException()
    {
        var task = NewTask();
        // Cannot go Created → Executing
        var act = () => task.StartExecuting();
        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Fail_captures_failure_context()
    {
        var task = NewTask();
        task.Enqueue();
        task.StartPlanning();
        var failure = new FailureContext(
            Type: FailureType.Unknown,
            RawOutput: "plan error",
            ExitCode: null,
            ErrorHash: "hash789",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: null,
            OccurredAt: DateTimeOffset.UtcNow);
        task.Fail(failure);
        task.State.Should().Be(TaskState.Failed);
        task.LastFailure.Should().Be(failure);
    }

    [Fact]
    public void UpdatedAt_is_set_on_each_transition()
    {
        var task = NewTask();
        task.UpdatedAt.Should().BeNull();
        task.Enqueue();
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void OrchestratorTask_initializes_with_normal_priority()
    {
        // Arrange & Act
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test"
        };

        // Assert
        task.Priority.Should().Be(TaskPriority.Normal);
    }

    [Fact]
    public void OrchestratorTask_can_set_priority()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test"
        };

        // Act
        task.Priority = TaskPriority.High;

        // Assert
        task.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public void OrchestratorTask_tracks_queue_time()
    {
        // Arrange & Act
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test"
        };

        // Assert
        task.QueuedAt.Should().BeNull();
    }
}
