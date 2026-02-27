using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.StepExecution;

/// <summary>
/// Executes a single step of a given type (Shell or Agent).
/// </summary>
public interface IStepExecutor
{
    /// <summary>
    /// Get the step type this executor handles.
    /// </summary>
    StepType StepType { get; }

    /// <summary>
    /// Validate that the step has all required fields for this executor.
    /// Throws InvalidOperationException if validation fails.
    /// </summary>
    void ValidateStep(ExecutionStep step);
}
