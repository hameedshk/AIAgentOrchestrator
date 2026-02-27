using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.StepExecution;

/// <summary>
/// Routes step execution requests to the appropriate executor (Shell or Agent).
/// Maintains a singleton instance of each executor type.
/// </summary>
public sealed class StepExecutionDispatcher
{
    private readonly ShellStepExecutor _shellExecutor = new();
    private readonly AgentStepExecutor _agentExecutor = new();

    /// <summary>
    /// Get the executor for the given step type.
    /// Returns a cached executor instance.
    /// </summary>
    public IStepExecutor GetExecutor(StepType stepType) => stepType switch
    {
        StepType.Shell => _shellExecutor,
        StepType.Agent => _agentExecutor,
        _ => throw new InvalidOperationException($"Unknown step type: {stepType}")
    };
}
