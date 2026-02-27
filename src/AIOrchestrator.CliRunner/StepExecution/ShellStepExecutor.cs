using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.StepExecution;

/// <summary>
/// Executes deterministic shell commands.
/// Validates that the step has a Command field.
/// </summary>
public sealed class ShellStepExecutor : IStepExecutor
{
    public StepType StepType => StepType.Shell;

    public void ValidateStep(ExecutionStep step)
    {
        if (string.IsNullOrWhiteSpace(step.Command))
            throw new InvalidOperationException(
                $"Shell step {step.Index} requires a Command field");
    }
}
