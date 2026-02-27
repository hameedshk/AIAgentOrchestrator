using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Persistence.Abstractions;

namespace AIOrchestrator.App.CrashRecovery;

/// <summary>
/// Orchestrates crash recovery flow on engine startup.
/// Spec Section 9.3: Load scheduler state, identify recovering tasks, resume execution.
/// </summary>
public sealed class CrashRecoveryManager : ICrashRecoveryManager
{
    private readonly ITaskRepository _taskRepository;
    private readonly ICrashLoopDetector _crashLoopDetector;
    private readonly ITaskRecoveryCoordinator _taskRecoveryCoordinator;
    private readonly IRecoveryEventLogger _eventLogger;

    private bool _safeMode;

    public bool IsInSafeMode => _safeMode;

    public CrashRecoveryManager(
        ITaskRepository taskRepository,
        ICrashLoopDetector crashLoopDetector,
        ITaskRecoveryCoordinator taskRecoveryCoordinator,
        IRecoveryEventLogger eventLogger)
    {
        _taskRepository = taskRepository;
        _crashLoopDetector = crashLoopDetector;
        _taskRecoveryCoordinator = taskRecoveryCoordinator;
        _eventLogger = eventLogger;
    }

    public async Task<int> RecoverAsync(CancellationToken ct = default)
    {
        _crashLoopDetector.RecordRestart();
        int restartCount = _crashLoopDetector.RestartCount;

        if (_crashLoopDetector.ShouldEnterSafeMode())
        {
            _safeMode = true;
            _eventLogger.LogSafeModeEntered(restartCount);
            _eventLogger.LogEngineStartup(restartCount, enteredSafeMode: true);
            return 0;
        }

        _eventLogger.LogEngineStartup(restartCount, enteredSafeMode: false);

        var allTasks = await _taskRepository.LoadAllAsync(ct);
        var recoveringTasks = _taskRecoveryCoordinator.IdentifyRecoveringTasks(allTasks);

        foreach (var task in recoveringTasks)
        {
            var recoveredTask = await _taskRecoveryCoordinator.RecoverTaskAsync(task, ct);
            await _taskRepository.SaveAsync(recoveredTask, ct);

            _eventLogger.LogTaskRecovered(
                recoveredTask.Id,
                recoveredTask.Title,
                recoveredTask.CurrentStepIndex,
                recoveredTask.Planner.ToString(),
                recoveredTask.Executor.ToString());
        }

        return recoveringTasks.Count;
    }

    public void ResetCrashCounter()
    {
        _crashLoopDetector.ResetCounter();
        _eventLogger.LogCleanShutdown();
    }
}
