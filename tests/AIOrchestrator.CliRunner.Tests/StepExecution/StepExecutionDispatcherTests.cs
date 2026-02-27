using AIOrchestrator.CliRunner.StepExecution;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using FluentAssertions;

namespace AIOrchestrator.CliRunner.Tests.StepExecution;

public class StepExecutionDispatcherTests
{
    private static ExecutionStep CreateShellStep() =>
        new() { Index = 0, Type = StepType.Shell, Description = "Build", Command = "echo test" };

    private static ExecutionStep CreateAgentStep() =>
        new() { Index = 0, Type = StepType.Agent, Description = "Refactor", Prompt = "Refactor this code" };

    [Fact]
    public void StepExecutionDispatcher_routes_Shell_step_to_ShellStepExecutor()
    {
        // Arrange
        var dispatcher = new StepExecutionDispatcher();
        var step = CreateShellStep();

        // Act
        var executor = dispatcher.GetExecutor(step.Type);

        // Assert
        executor.Should().BeOfType<ShellStepExecutor>();
    }

    [Fact]
    public void StepExecutionDispatcher_routes_Agent_step_to_AgentStepExecutor()
    {
        // Arrange
        var dispatcher = new StepExecutionDispatcher();
        var step = CreateAgentStep();

        // Act
        var executor = dispatcher.GetExecutor(step.Type);

        // Assert
        executor.Should().BeOfType<AgentStepExecutor>();
    }

    [Fact]
    public void StepExecutionDispatcher_reuses_same_executor_instances()
    {
        // Arrange
        var dispatcher = new StepExecutionDispatcher();

        // Act
        var executor1 = dispatcher.GetExecutor(StepType.Shell);
        var executor2 = dispatcher.GetExecutor(StepType.Shell);

        // Assert
        executor1.Should().BeSameAs(executor2);
    }

    [Fact]
    public void ShellStepExecutor_requires_Command_in_step()
    {
        // Arrange
        var executor = new ShellStepExecutor();
        var step = new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "No command" };

        // Act
        var act = () => executor.ValidateStep(step);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AgentStepExecutor_requires_Prompt_in_step()
    {
        // Arrange
        var executor = new AgentStepExecutor();
        var step = new ExecutionStep { Index = 0, Type = StepType.Agent, Description = "No prompt" };

        // Act
        var act = () => executor.ValidateStep(step);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }
}
