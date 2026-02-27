using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using FluentAssertions;

namespace AIOrchestrator.Domain.Tests.Entities;

public class ExecutionStepTests
{
    [Fact]
    public void New_step_has_Pending_status()
    {
        var step = new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "Build" };
        step.Status.Should().Be(StepStatus.Pending);
    }

    [Fact]
    public void MarkStarted_sets_Running_status_and_timestamp()
    {
        var step = new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "Build" };
        step.MarkStarted();
        step.Status.Should().Be(StepStatus.Running);
        step.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompleted_sets_Completed_status_and_captures_output()
    {
        var step = new ExecutionStep { Index = 0, Type = StepType.Agent, Description = "Write code" };
        step.MarkStarted();
        step.MarkCompleted("output text");
        step.Status.Should().Be(StepStatus.Completed);
        step.ActualOutput.Should().Be("output text");
        step.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_sets_Failed_status_and_captures_failure()
    {
        var step = new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "Build" };
        var failure = new FailureContext(
            Type: FailureType.CompileError,
            RawOutput: "Build error: syntax error",
            ExitCode: 1,
            ErrorHash: "hash123",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow);
        step.MarkStarted();
        step.MarkFailed(failure);
        step.Status.Should().Be(StepStatus.Failed);
        step.LastFailure.Should().Be(failure);
    }
}
