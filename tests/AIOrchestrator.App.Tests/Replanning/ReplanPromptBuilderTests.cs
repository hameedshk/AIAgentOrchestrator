using FluentAssertions;
using AIOrchestrator.App.Replanning;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.Replanning;

public class ReplanPromptBuilderTests
{
    [Fact]
    public void BuildReplanPrompt_includes_original_task_info()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new OrchestratorTask
        {
            Id = taskId,
            Title = "Deploy Application",
            Description = "Deploy the application to production environment",
            AllowReplan = true
        };

        var completedSteps = new List<ExecutionStep>().AsReadOnly();

        var failedStep = new ExecutionStep
        {
            Index = 0,
            Type = StepType.Shell,
            Description = "Build Docker image",
            Command = "docker build -t myapp:latest ."
        };

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "Docker daemon not running",
            ExitCode: 1,
            ErrorHash: "hash123",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        // Act
        var prompt = ReplanPromptBuilder.BuildReplanPrompt(task, completedSteps, failedStep, failureContext);

        // Assert
        prompt.Should().Contain("Deploy Application");
        prompt.Should().Contain(taskId.ToString());
        prompt.Should().Contain("Deploy the application to production environment");
    }

    [Fact]
    public void BuildReplanPrompt_includes_failure_context()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            AllowReplan = true
        };

        var completedSteps = new List<ExecutionStep>().AsReadOnly();

        var failedStep = new ExecutionStep
        {
            Index = 0,
            Type = StepType.Shell,
            Description = "Run tests",
            Command = "npm test"
        };

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "Test runner crashed: out of memory",
            ExitCode: 137,
            ErrorHash: "hash456",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        // Act
        var prompt = ReplanPromptBuilder.BuildReplanPrompt(task, completedSteps, failedStep, failureContext);

        // Assert
        prompt.Should().Contain("NonRetryable");
        prompt.Should().Contain("Test runner crashed: out of memory");
    }

    [Fact]
    public void BuildReplanPrompt_includes_completed_steps_summary()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Multi-step Task",
            AllowReplan = true
        };

        var completedSteps = new List<ExecutionStep>
        {
            new ExecutionStep
            {
                Index = 0,
                Type = StepType.Shell,
                Description = "Clone repository",
                Command = "git clone repo.git"
            },
            new ExecutionStep
            {
                Index = 1,
                Type = StepType.Agent,
                Description = "Review code quality",
                Prompt = "Check code for issues"
            }
        }.AsReadOnly();

        var failedStep = new ExecutionStep
        {
            Index = 2,
            Type = StepType.Shell,
            Description = "Run build",
            Command = "npm run build"
        };

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "Build failed",
            ExitCode: 1,
            ErrorHash: "hash789",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        // Act
        var prompt = ReplanPromptBuilder.BuildReplanPrompt(task, completedSteps, failedStep, failureContext);

        // Assert
        prompt.Should().Contain("Steps Completed Successfully: 2");
        prompt.Should().Contain("Step 0 (Shell): Clone repository — COMPLETED");
        prompt.Should().Contain("Step 1 (Agent): Review code quality — COMPLETED");
    }

    [Fact]
    public void BuildReplanPrompt_includes_json_format_instruction()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new OrchestratorTask
        {
            Id = taskId,
            Title = "Test Task",
            AllowReplan = true
        };

        var completedSteps = new List<ExecutionStep>().AsReadOnly();

        var failedStep = new ExecutionStep
        {
            Index = 1,
            Type = StepType.Shell,
            Description = "Deploy",
            Command = "deploy.sh"
        };

        var failureContext = new FailureContext(
            Type: FailureType.DependencyMissing,
            RawOutput: "Deployment failed",
            ExitCode: 1,
            ErrorHash: "hash999",
            Retryable: false,
            PlannerModel: ModelType.Claude,
            ExecutorModel: ModelType.Codex,
            OccurredAt: DateTimeOffset.UtcNow
        );

        // Act
        var prompt = ReplanPromptBuilder.BuildReplanPrompt(task, completedSteps, failedStep, failureContext);

        // Assert
        prompt.Should().Contain("\"planVersion\"");
        prompt.Should().Contain("\"taskId\"");
        prompt.Should().Contain("\"steps\"");
        prompt.Should().Contain("\"index\"");
        prompt.Should().Contain("\"type\"");
        prompt.Should().Contain("\"description\"");
        prompt.Should().Contain("\"command\"");
        prompt.Should().Contain("\"prompt\"");
        // Verify task ID is included in JSON example
        prompt.Should().Contain(taskId.ToString());
    }
}
