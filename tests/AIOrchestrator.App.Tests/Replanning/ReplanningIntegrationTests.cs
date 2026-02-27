using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.Replanning;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using System.Text.Json;

namespace AIOrchestrator.App.Tests.Replanning;

public class ReplanningIntegrationTests
{
    [Fact]
    public async Task FullReplan_flow_from_failure_to_revised_plan_execution()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new OrchestratorTask
        {
            Id = taskId,
            Title = "Complete Application Setup",
            Description = "Setup and configure the application",
            AllowReplan = true,
            ReplanAttempts = 0,
            Planner = ModelType.Claude,
            Executor = ModelType.Codex
        };

        // Initial plan: 3 steps
        var step0 = new ExecutionStep
        {
            Index = 0,
            Type = StepType.Shell,
            Description = "Install dependencies",
            Command = "npm install"
        };
        step0.MarkStarted();
        step0.MarkCompleted("dependencies installed");

        var step1 = new ExecutionStep
        {
            Index = 1,
            Type = StepType.Shell,
            Description = "Build application",
            Command = "npm run build"
        };

        var step2 = new ExecutionStep
        {
            Index = 2,
            Type = StepType.Shell,
            Description = "Start application",
            Command = "npm start"
        };

        var initialSteps = new List<ExecutionStep> { step0, step1, step2 };
        task.ApprovePlan("1", initialSteps);

        var failedStep = initialSteps[1];
        var failureContext = new FailureContext(
            Type: FailureType.TestFailure,
            RawOutput: "TypeScript compilation failed",
            ExitCode: 1,
            ErrorHash: "ts_error_123",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        // Revised plan from Planner
        var revisedPlanJson = JsonSerializer.Serialize(new
        {
            planVersion = "1",
            taskId = taskId.ToString(),
            steps = new object[]
            {
                new
                {
                    index = 1,
                    type = "Agent",
                    description = "Diagnose TypeScript issues",
                    prompt = "Analyze the TypeScript compilation errors and suggest fixes",
                    command = (string?)null
                },
                new
                {
                    index = 2,
                    type = "Shell",
                    description = "Apply fixes and rebuild",
                    command = "npm run build",
                    prompt = (string?)null
                },
                new
                {
                    index = 3,
                    type = "Shell",
                    description = "Verify build succeeded",
                    command = "ls -la dist/",
                    prompt = (string?)null
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
        updatedTask.Steps.Should().HaveCount(6); // 3 original + 3 revised

        // Verify revised steps are marked as from replan
        updatedTask.Steps[3].IsFromReplan.Should().BeTrue();
        updatedTask.Steps[3].Type.Should().Be(StepType.Agent);
        updatedTask.Steps[3].Description.Should().Be("Diagnose TypeScript issues");

        updatedTask.Steps[4].IsFromReplan.Should().BeTrue();
        updatedTask.Steps[4].Type.Should().Be(StepType.Shell);
        updatedTask.Steps[4].Command.Should().Be("npm run build");

        updatedTask.Steps[5].IsFromReplan.Should().BeTrue();
        updatedTask.Steps[5].Type.Should().Be(StepType.Shell);
        updatedTask.Steps[5].Description.Should().Be("Verify build succeeded");
    }

    [Fact]
    public async Task Recursive_replanning_works_with_second_failure()
    {
        // Arrange: Start with a task that has already been re-planned once
        var taskId = Guid.NewGuid();
        var task = new OrchestratorTask
        {
            Id = taskId,
            Title = "Complex Workflow",
            Description = "Multi-stage workflow with potential failures",
            AllowReplan = true,
            ReplanAttempts = 1, // Already re-planned once
            Planner = ModelType.Claude,
            Executor = ModelType.Codex
        };

        var stepsStep0 = new ExecutionStep
        {
            Index = 0,
            Type = StepType.Shell,
            Description = "Stage 1",
            Command = "stage1.sh"
        };
        stepsStep0.MarkStarted();
        stepsStep0.MarkCompleted("Stage 1 completed");

        var stepsStep1 = new ExecutionStep
        {
            Index = 1,
            Type = StepType.Shell,
            Description = "Stage 2 - revised attempt",
            Command = "stage2_revised.sh"
        };

        var stepsAfterFirstReplan = new List<ExecutionStep> { stepsStep0, stepsStep1 };
        task.ApprovePlan("1", stepsAfterFirstReplan);

        var secondFailedStep = stepsAfterFirstReplan[1];
        var secondFailureContext = new FailureContext(
            Type: FailureType.RuntimeException,
            RawOutput: "Resource unavailable",
            ExitCode: 503,
            ErrorHash: "resource_error_456",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        var secondRevisedPlanJson = JsonSerializer.Serialize(new
        {
            planVersion = "1",
            taskId = taskId.ToString(),
            steps = new object[]
            {
                new
                {
                    index = 1,
                    type = "Agent",
                    description = "Check resource availability and suggest alternative approach",
                    prompt = "The resource is unavailable. Suggest an alternative approach.",
                    command = (string?)null
                },
                new
                {
                    index = 2,
                    type = "Shell",
                    description = "Continue with fallback strategy",
                    command = "fallback_strategy.sh",
                    prompt = (string?)null
                }
            }
        });

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        cliSessionManager.InvokeAsync(
            Arg.Any<ModelType>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        ).Returns(secondRevisedPlanJson);

        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var updatedTaskAfterSecondReplan = await orchestrator.ReplanAsync(
            task,
            secondFailedStep,
            secondFailureContext
        );

        // Assert
        updatedTaskAfterSecondReplan.ReplanAttempts.Should().Be(2, "should increment from 1 to 2");
        updatedTaskAfterSecondReplan.Steps.Should().HaveCount(4); // 2 original + 2 second replan

        // Verify second replan steps are marked as from replan
        updatedTaskAfterSecondReplan.Steps[2].IsFromReplan.Should().BeTrue();
        updatedTaskAfterSecondReplan.Steps[2].Type.Should().Be(StepType.Agent);

        updatedTaskAfterSecondReplan.Steps[3].IsFromReplan.Should().BeTrue();
        updatedTaskAfterSecondReplan.Steps[3].Type.Should().Be(StepType.Shell);
    }

    [Fact]
    public void Replan_eligibility_prevents_excessive_replans()
    {
        // Arrange: Task at maximum replan attempts
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Problematic Task",
            AllowReplan = true,
            ReplanAttempts = 3  // At max limit
        };

        var failureContext = new FailureContext(
            Type: FailureType.TestFailure,
            RawOutput: "Error occurred",
            ExitCode: 1,
            ErrorHash: "error_789",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var canReplan = orchestrator.CanReplan(task, failureContext);

        // Assert
        canReplan.Should().BeFalse("max replan attempts (3) have been reached");
    }

    [Fact]
    public void Replan_eligibility_allows_up_to_max_attempts()
    {
        // Arrange: Task just below maximum replan attempts
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Task with some replans",
            AllowReplan = true,
            ReplanAttempts = 2  // One below max
        };

        var failureContext = new FailureContext(
            Type: FailureType.TestFailure,
            RawOutput: "Error occurred",
            ExitCode: 1,
            ErrorHash: "error_101",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var canReplan = orchestrator.CanReplan(task, failureContext);

        // Assert
        canReplan.Should().BeTrue("still within max replan attempts");
    }
}
