namespace AIOrchestrator.CliRunner.GitSnapshot;

/// <summary>
/// Metadata for a git snapshot taken before a step executes.
/// Used to enable idempotent retries via git reset.
/// </summary>
public sealed record GitSnapshotMetadata(
    string CommitSha,
    DateTimeOffset TakenAt
);
