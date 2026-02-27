using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.CliRunner.ExecutorSession;

/// <summary>
/// Persistent executor session for long-running step execution.
/// One instance per active task. Maintains a single CLI process.
/// </summary>
public interface IExecutorSession : IAsyncDisposable
{
    /// <summary>
    /// Unique task ID this session is bound to.
    /// </summary>
    Guid TaskId { get; }

    /// <summary>
    /// Execute a single step (Shell or Agent).
    /// Returns output, status, and execution metadata.
    /// </summary>
    Task<ExecutionStepResult> ExecuteStepAsync(ExecutionStep step, string? stepPrompt = null,
                                                CancellationToken ct = default);

    /// <summary>
    /// Check if session is still alive (process hasn't crashed).
    /// </summary>
    Task<bool> IsAliveAsync(CancellationToken ct = default);

    /// <summary>
    /// Close session gracefully (send exit command, wait for process to finish).
    /// </summary>
    Task CloseAsync(CancellationToken ct = default);
}

/// <summary>
/// Result of a single step execution.
/// </summary>
public sealed record ExecutionStepResult(
    string Output,
    int ExitCode,
    bool TimedOut,
    bool SentinelDetected,
    DateTimeOffset ExecutedAt,
    TimeSpan Duration
)
{
    public bool IsSuccess => !TimedOut && ExitCode == 0;
}
