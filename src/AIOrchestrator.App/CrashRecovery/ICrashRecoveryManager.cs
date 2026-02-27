using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.App.CrashRecovery;

/// <summary>
/// Manages crash recovery flow on engine startup.
/// Spec Section 9.3: Load persisted state, identify recovering tasks, resume execution.
/// </summary>
public interface ICrashRecoveryManager
{
    /// <summary>
    /// Execute recovery flow on engine startup.
    /// Identifies tasks that were Executing/Planning at crash time and resumes them.
    /// </summary>
    /// <returns>Count of tasks recovered</returns>
    Task<int> RecoverAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if engine is in safe mode due to restart loop.
    /// Spec Section 9.4: Safe mode entered if > 3 restarts within 5 minutes.
    /// </summary>
    bool IsInSafeMode { get; }

    /// <summary>
    /// Reset crash counter on clean shutdown.
    /// </summary>
    void ResetCrashCounter();
}
