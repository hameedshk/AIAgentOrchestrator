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

        // Transition to Executing based on current state
        if (task.State == TaskState.Planning)
        {
            // If planning crashed, approve empty plan first then execute
            if (task.Steps.Count == 0)
            {
                task.ApprovePlan("1", []);
            }
            task.StartExecuting();
        }
        else if (task.State != TaskState.Executing)
        {
            // If not Executing or Planning, try to resume (for other states that allow it)
            task.ResumeExecuting();
        }

        task.CurrentStepIndex = lastCompletedStepIndex + 1;

        return await Task.FromResult(task);
    }
}
