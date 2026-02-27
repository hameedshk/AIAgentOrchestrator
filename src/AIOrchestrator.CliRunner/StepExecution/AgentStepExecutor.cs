using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.StepExecution;

/// <summary>
/// Executes natural language prompts via LLM CLI session.
/// Validates that the step has a Prompt field.
/// </summary>
public sealed class AgentStepExecutor : IStepExecutor
{
    public StepType StepType => StepType.Agent;

    public void ValidateStep(ExecutionStep step)
    {
        if (string.IsNullOrWhiteSpace(step.Prompt))
            throw new InvalidOperationException(
                $"Agent step {step.Index} requires a Prompt field");
    }
}
