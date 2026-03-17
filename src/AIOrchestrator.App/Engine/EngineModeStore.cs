namespace AIOrchestrator.App.Engine;

public enum ExecutionMode
{
    Safe,
    SemiAuto,
    FullAuto
}

/// <summary>
/// Stores engine mode selected from the dashboard/API.
/// </summary>
public sealed class EngineModeStore
{
    private readonly object _lock = new();
    private ExecutionMode _mode = ExecutionMode.Safe;

    public ExecutionMode CurrentMode
    {
        get
        {
            lock (_lock)
            {
                return _mode;
            }
        }
    }

    public void SetMode(ExecutionMode mode)
    {
        lock (_lock)
        {
            _mode = mode;
        }
    }
}
