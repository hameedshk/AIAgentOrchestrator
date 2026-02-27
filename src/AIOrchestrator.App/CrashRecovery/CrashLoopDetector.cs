namespace AIOrchestrator.App.CrashRecovery;

/// <summary>
/// Prevents engine restart spirals by tracking restart count and timing.
/// Spec Section 9.4: Enter safe mode if > 3 restarts within 5 minutes.
/// </summary>
public sealed class CrashLoopDetector : ICrashLoopDetector
{
    private const int MaxRestartsBeforeSafeMode = 3;
    private const int TimeWindowMinutes = 5;

    private readonly string _restartCounterPath;
    private List<DateTimeOffset> _restartTimestamps = new();

    public int RestartCount => _restartTimestamps.Count;

    public CrashLoopDetector(string dataDirectory)
    {
        _restartCounterPath = Path.Combine(dataDirectory, "crash_restart_counter.json");
        LoadRestartHistory();
    }

    public void RecordRestart()
    {
        var now = DateTimeOffset.UtcNow;
        _restartTimestamps.Add(now);
        PersistRestartHistory();
    }

    public bool ShouldEnterSafeMode()
    {
        var now = DateTimeOffset.UtcNow;
        var timeWindow = now.AddMinutes(-TimeWindowMinutes);
        int recentRestarts = _restartTimestamps.Count(ts => ts > timeWindow);
        return recentRestarts > MaxRestartsBeforeSafeMode;
    }

    public void ResetCounter()
    {
        _restartTimestamps.Clear();
        if (File.Exists(_restartCounterPath))
            File.Delete(_restartCounterPath);
    }

    private void LoadRestartHistory()
    {
        if (!File.Exists(_restartCounterPath))
            return;

        try
        {
            var json = File.ReadAllText(_restartCounterPath);
            _restartTimestamps = System.Text.Json.JsonSerializer.Deserialize<List<DateTimeOffset>>(json) ?? new();
        }
        catch
        {
            _restartTimestamps = new();
        }
    }

    private void PersistRestartHistory()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_restartTimestamps);
            Directory.CreateDirectory(Path.GetDirectoryName(_restartCounterPath)!);

            string tempPath = _restartCounterPath + ".tmp";
            File.WriteAllText(tempPath, json);
            if (File.Exists(_restartCounterPath))
                File.Replace(tempPath, _restartCounterPath, null);
            else
                File.Move(tempPath, _restartCounterPath);
        }
        catch
        {
            // Silently fail - don't let crash detection break the engine
        }
    }
}
