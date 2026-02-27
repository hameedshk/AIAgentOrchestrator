using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.FailureClassification;

/// <summary>
/// Classifies step failures from exit codes and output.
/// Implements the 4-step pipeline from spec Section 8.2.
/// </summary>
public interface IFailureClassifier
{
    /// <summary>
    /// Classify a step failure from exit code, stdout/stderr output, and context.
    /// </summary>
    /// <param name="exitCode">Process exit code (null for agent steps)</param>
    /// <param name="output">Captured stdout/stderr combined</param>
    /// <param name="plannerModel">Model assigned as planner for diagnostic correlation</param>
    /// <param name="executorModel">Model assigned as executor (which one failed)</param>
    /// <returns>Classified failure context with type, hash, and retryability verdict</returns>
    FailureContext Classify(
        int? exitCode,
        string output,
        ModelType? plannerModel,
        ModelType? executorModel);

    /// <summary>
    /// Compute SHA256 fingerprint of normalized error output.
    /// Used for detecting same error on consecutive retries.
    /// </summary>
    string ComputeErrorHash(string output);
}
