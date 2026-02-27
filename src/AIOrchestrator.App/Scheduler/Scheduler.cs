using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Scheduler;

public class Scheduler : IScheduler
{
    private readonly List<OrchestratorTask> _queue = [];
    protected readonly HashSet<string> _runningProjects = [];
    private readonly object _queueLock = new();
    private const int AgingThresholdMinutes = 5;

    public virtual Task EnqueueAsync(OrchestratorTask task)
    {
        lock (_queueLock)
        {
            if (task.QueuedAt == null)
                task.QueuedAt = DateTimeOffset.UtcNow;

            _queue.Add(task);
            SortQueue();
        }

        return Task.CompletedTask;
    }

    public virtual Task<OrchestratorTask?> DispatchAsync(int cpuAvailable, int memoryAvailableMb, int maxProcesses)
    {
        lock (_queueLock)
        {
            SortQueue();

            for (int i = 0; i < _queue.Count; i++)
            {
                var task = _queue[i];

                if (!string.IsNullOrEmpty(task.ProjectId) && _runningProjects.Contains(task.ProjectId))
                    continue;

                _queue.RemoveAt(i);
                return Task.FromResult((OrchestratorTask?)task);
            }

            return Task.FromResult((OrchestratorTask?)null);
        }
    }

    public virtual Task MarkRunningAsync(string projectId)
    {
        lock (_queueLock)
        {
            _runningProjects.Add(projectId);
        }

        return Task.CompletedTask;
    }

    public virtual Task MarkCompleteAsync(string projectId)
    {
        lock (_queueLock)
        {
            _runningProjects.Remove(projectId);
        }

        return Task.CompletedTask;
    }

    protected void SortQueue()
    {
        var now = DateTimeOffset.UtcNow;
        _queue.Sort((a, b) =>
        {
            var aEffective = GetEffectivePriority(a, now);
            var bEffective = GetEffectivePriority(b, now);

            int priorityCompare = bEffective.CompareTo(aEffective);
            if (priorityCompare != 0)
                return priorityCompare;

            return (a.QueuedAt ?? now).CompareTo(b.QueuedAt ?? now);
        });
    }

    private int GetEffectivePriority(OrchestratorTask task, DateTimeOffset now)
    {
        var basePriority = (int)task.Priority;

        if (task.QueuedAt.HasValue)
        {
            var waitTime = now - task.QueuedAt.Value;
            if (waitTime.TotalMinutes > AgingThresholdMinutes)
                basePriority += 1;
        }

        return basePriority;
    }
}
