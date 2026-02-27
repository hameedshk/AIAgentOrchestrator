using AIOrchestrator.CliRunner.GitSnapshot;
using AIOrchestrator.CliRunner.StepExecution;
using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.CliRunner.ExecutorSession;

/// <summary>
/// Manages the full lifecycle of executor session: step validation, git snapshots, and retries.
/// </summary>
public sealed class ExecutorSessionLifecycle
{
    private readonly StepExecutionDispatcher _dispatcher;
    private readonly IGitSnapshotManager? _snapshotManager;

    public ExecutorSessionLifecycle(StepExecutionDispatcher dispatcher, IGitSnapshotManager? snapshotManager = null)
    {
        _dispatcher = dispatcher;
        _snapshotManager = snapshotManager;
    }

    /// <summary>
    /// Validates a step before execution by dispatching to the appropriate executor.
    /// </summary>
    public void ValidateStep(ExecutionStep step)
    {
        var executor = _dispatcher.GetExecutor(step.Type);
        executor.ValidateStep(step);
    }
}
