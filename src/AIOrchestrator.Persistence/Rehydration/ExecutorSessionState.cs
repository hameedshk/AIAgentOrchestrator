namespace AIOrchestrator.Persistence.Rehydration;

/// <summary>
/// Persisted state for an executor session, used for crash recovery.
/// </summary>
public sealed record ExecutorSessionState(
    Guid TaskId,
    int CurrentStepIndex,
    string LastSnapshotCommitSha,
    DateTimeOffset LastSavedAt
);
