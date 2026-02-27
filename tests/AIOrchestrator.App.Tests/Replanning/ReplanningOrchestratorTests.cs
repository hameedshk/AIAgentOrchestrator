using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.Replanning;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using System.Text.Json;

namespace AIOrchestrator.App.Tests.Replanning;

public class ReplanningOrchestratorTests
{
    [Fact]
    public void CanReplan_returns_true_when_conditions_met()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            AllowReplan = true,
            ReplanAttempts = 0
        };

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "Command not found",
            ExitCode: 127,
            ErrorHash: "hash123",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var result = orchestrator.CanReplan(task, failureContext);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanReplan_returns_false_when_allowReplan_false()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            AllowReplan = false,
            ReplanAttempts = 0
        };

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "Command not found",
            ExitCode: 127,
            ErrorHash: "hash123",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var result = orchestrator.CanReplan(task, failureContext);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReplan_returns_false_when_failure_retryable()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            AllowReplan = true,
            ReplanAttempts = 0
        };

        var failureContext = new FailureContext(
            Type: FailureType.Timeout,
            RawOutput: "Timeout",
            ExitCode: 124,
            ErrorHash: "hash456",
            Retryable: true,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var result = orchestrator.CanReplan(task, failureContext);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanReplan_returns_false_when_max_attempts_exceeded()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            AllowReplan = true,
            ReplanAttempts = 3
        };

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "Command not found",
            ExitCode: 127,
            ErrorHash: "hash123",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var result = orchestrator.CanReplan(task, failureContext);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReplanAsync_appends_revised_steps_with_isFromReplan_flag()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new OrchestratorTask
        {
            Id = taskId,
            Title = "Test Task",
            Description = "Original task description",
            AllowReplan = true,
            ReplanAttempts = 0,
            Planner = ModelType.Claude,
            Executor = ModelType.Codex
        };

        var completedStep = new ExecutionStep
        {
            Index = 0,
            Type = StepType.Shell,
            Description = "First step",
            Command = "echo hello"
        };
        completedStep.MarkStarted();
        completedStep.MarkCompleted("hello");

        var failedStep = new ExecutionStep
        {
            Index = 1,
            Type = StepType.Shell,
            Description = "Failed step",
            Command = "invalid-command"
        };

        task.Enqueue();
        task.StartPlanning();
        task.ApprovePlan("1", new[] { completedStep, failedStep });
        task.StartExecuting();

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "command not found",
            ExitCode: 127,
            ErrorHash: "hash789",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        // Revised plan response from Planner
        var revisedPlanJson = JsonSerializer.Serialize(new
        {
            planVersion = "1",
            taskId = taskId.ToString(),
            steps = new[]
            {
                new
                {
                    index = 1,
                    type = "Shell",
                    description = "Revised approach",
                    command = "ls -la"
                },
                new
                {
                    index = 2,
                    type = "Shell",
                    description = "Verify result",
                    command = "echo success"
                }
            }
        });

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        cliSessionManager.InvokeAsync(
            Arg.Any<ModelType>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        ).Returns(revisedPlanJson);

        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var updatedTask = await orchestrator.ReplanAsync(task, failedStep, failureContext);

        // Assert
        updatedTask.ReplanAttempts.Should().Be(1);
        updatedTask.Steps.Should().HaveCount(4); // 2 original + 2 revised

        // Original steps should not have IsFromReplan flag
        updatedTask.Steps[0].IsFromReplan.Should().BeFalse();
        updatedTask.Steps[1].IsFromReplan.Should().BeFalse();

        // New steps from replan should have IsFromReplan flag
        updatedTask.Steps[2].IsFromReplan.Should().BeTrue();
        updatedTask.Steps[2].Description.Should().Be("Revised approach");
        updatedTask.Steps[3].IsFromReplan.Should().BeTrue();
        updatedTask.Steps[3].Description.Should().Be("Verify result");
    }

    [Fact]
    public async Task ReplanAsync_throws_on_invalid_json()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new OrchestratorTask
        {
            Id = taskId,
            Title = "Test Task",
            Description = "Original task description",
            AllowReplan = true,
            ReplanAttempts = 0,
            Planner = ModelType.Claude,
            Executor = ModelType.Codex
        };

        var failedStep = new ExecutionStep
        {
            Index = 0,
            Type = StepType.Shell,
            Description = "Failed step",
            Command = "invalid-command"
        };

        task.Enqueue();
        task.StartPlanning();
        task.ApprovePlan("1", new[] { failedStep });
        task.StartExecuting();

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "error occurred",
            ExitCode: 1,
            ErrorHash: "hash999",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        cliSessionManager.InvokeAsync(
            Arg.Any<ModelType>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        ).Returns("{ invalid json }");

        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act & Assert
        await orchestrator.Invoking(o => o.ReplanAsync(task, failedStep, failureContext))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid JSON*");
    }
}
