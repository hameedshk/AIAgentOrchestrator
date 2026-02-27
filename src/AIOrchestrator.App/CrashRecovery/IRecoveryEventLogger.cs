using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.App.CrashRecovery;

/// <summary>
/// Structured logging for crash recovery events.
/// Spec Section 9.3: Emit recovery diagnostic log entry for each recovered task (including model identities).
/// Spec Section 15: Every log entry includes model ID field.
/// </summary>
public interface IRecoveryEventLogger
{
    /// <summary>
    /// Log engine startup with crash loop check result.
    /// </summary>
    void LogEngineStartup(int restartCount, bool enteredSafeMode);

    /// <summary>
    /// Log task recovery event with model identity.
    /// </summary>
    void LogTaskRecovered(Guid taskId, string taskTitle, int recoveredToStepIndex, string plannerModel, string executorModel);

    /// <summary>
    /// Log safe mode entry.
    /// </summary>
    void LogSafeModeEntered(int restartCount);

    /// <summary>
    /// Log clean shutdown (reset crash counter).
    /// </summary>
    void LogCleanShutdown();
}
