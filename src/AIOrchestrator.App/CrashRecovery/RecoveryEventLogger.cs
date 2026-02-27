namespace AIOrchestrator.App.CrashRecovery;

/// <summary>
/// Logs recovery events in structured JSON format to system.log.
/// Spec Section 15: Structured append-only logs with modelId field.
/// </summary>
public sealed class RecoveryEventLogger : IRecoveryEventLogger
{
    private readonly string _logPath;

    public RecoveryEventLogger(string dataDirectory)
    {
        _logPath = Path.Combine(dataDirectory, "logs", "system.log");
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
    }

    public void LogEngineStartup(int restartCount, bool enteredSafeMode)
    {
        var entry = new
        {
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            eventType = "EngineStartup",
            restartCount,
            enteredSafeMode,
            modelId = (string?)null
        };
        LogStructured(entry);
    }

    public void LogTaskRecovered(Guid taskId, string taskTitle, int recoveredToStepIndex, string plannerModel, string executorModel)
    {
        var entry = new
        {
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            eventType = "TaskRecovered",
            taskId = taskId.ToString(),
            taskTitle,
            recoveredToStepIndex,
            plannerModel,
            executorModel,
            modelId = executorModel
        };
        LogStructured(entry);
    }

    public void LogSafeModeEntered(int restartCount)
    {
        var entry = new
        {
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            eventType = "SafeModeEntered",
            restartCount,
            reason = "Too many restarts within 5 minutes",
            modelId = (string?)null
        };
        LogStructured(entry);
    }

    public void LogCleanShutdown()
    {
        var entry = new
        {
            timestamp = DateTimeOffset.UtcNow.ToString("o"),
            eventType = "CleanShutdown",
            action = "CrashCounterReset",
            modelId = (string?)null
        };
        LogStructured(entry);
    }

    private void LogStructured(object entry)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(entry);
            File.AppendAllText(_logPath, json + Environment.NewLine);
        }
        catch { }
    }
}
