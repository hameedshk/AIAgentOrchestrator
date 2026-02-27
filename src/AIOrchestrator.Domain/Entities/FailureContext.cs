namespace AIOrchestrator.Domain.Entities;

public sealed record FailureContext(
    string Reason,
    string? RawOutput,
    int? ExitCode,
    DateTimeOffset OccurredAt,
    int RetryAttempt
);
