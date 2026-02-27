namespace AIOrchestrator.Domain.Entities;

using AIOrchestrator.Domain.Enums;

/// <summary>
/// Captures failure context for a step execution.
/// See spec Section 4.7 for field definitions.
/// </summary>
public sealed record FailureContext(
    FailureType Type,
    string RawOutput,
    int? ExitCode,
    string ErrorHash,
    bool Retryable,
    ModelType? PlannerModel,
    ModelType? ExecutorModel,
    DateTimeOffset OccurredAt
);
