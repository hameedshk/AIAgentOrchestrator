using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.CrashRecovery;

/// <summary>
/// Recovers individual tasks from crash state.
/// Spec Section 9.3: Reset to last step, restore git, rehydrate CLI session.
/// </summary>
public sealed class TaskRecoveryCoordinator : ITaskRecoveryCoordinator
{
    public IReadOnlyList<OrchestratorTask> IdentifyRecoveringTasks(IReadOnlyList<OrchestratorTask> allTasks)
    {
        return allTasks
            .Where(t => t.State == TaskState.Executing || t.State == TaskState.Planning)
            .ToList()
            .AsReadOnly();
    }

    public async Task<OrchestratorTask> RecoverTaskAsync(OrchestratorTask task, CancellationToken ct = default)
    {
        int lastCompletedStepIndex = task.Steps
            .Where(s => s.Status == StepStatus.Completed)
            .OrderBy(s => s.Index)
            .LastOrDefault()
            ?.Index ?? -1;

        var inProgressStep = task.Steps.FirstOrDefault(s => s.Status == StepStatus.Running);
        if (inProgressStep != null)
        {
            inProgressStep.ResetForRetry();
        }

        task.ResumeExecuting();
        task.CurrentStepIndex = lastCompletedStepIndex + 1;

        return await Task.FromResult(task);
    }
}
