namespace AIOrchestrator.App.CrashRecovery;

/// <summary>
/// Detects and tracks restart loops to prevent spiral behavior.
/// Spec Section 9.4: Tracks engine restart count, enters safe mode if > 3 restarts within 5 minutes.
/// </summary>
public interface ICrashLoopDetector
{
    /// <summary>
    /// Record an engine restart event.
    /// </summary>
    void RecordRestart();

    /// <summary>
    /// Check if safe mode should be entered (too many restarts recently).
    /// </summary>
    /// <returns>True if > 3 restarts within 5 minutes</returns>
    bool ShouldEnterSafeMode();

    /// <summary>
    /// Reset restart counter on clean shutdown.
    /// </summary>
    void ResetCounter();

    /// <summary>
    /// Get current restart count for diagnostics.
    /// </summary>
    int RestartCount { get; }
}
