namespace AIOrchestrator.CliRunner.GitSnapshot;

/// <summary>
/// Manages git snapshots for idempotent retry strategy.
/// Takes snapshots before step execution and resets to them on retry.
/// </summary>
public interface IGitSnapshotManager
{
    /// <summary>
    /// Take a snapshot of the current git working directory state.
    /// Returns metadata including commit SHA and timestamp.
    /// </summary>
    Task<GitSnapshotMetadata> TakeSnapshotAsync(int stepIndex, CancellationToken ct = default);

    /// <summary>
    /// Reset the working directory to a previously captured snapshot.
    /// Uses `git reset --hard <commit>` to ensure deterministic state.
    /// </summary>
    Task<bool> ResetToSnapshotAsync(GitSnapshotMetadata snapshot, CancellationToken ct = default);

    /// <summary>
    /// Retrieve a previously captured snapshot by step index.
    /// Returns null if snapshot not found.
    /// </summary>
    Task<GitSnapshotMetadata?> GetSnapshotAsync(int stepIndex, CancellationToken ct = default);
}
