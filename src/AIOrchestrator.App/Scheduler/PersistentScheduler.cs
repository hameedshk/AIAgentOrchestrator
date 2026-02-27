using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Persistence.Abstractions;
using AIOrchestrator.Persistence.Dto;

namespace AIOrchestrator.App.Scheduler;

public class PersistentScheduler : Scheduler
{
    private readonly ISchedulerStateRepository _repository;
    private readonly List<Guid> _queuedTaskIds = [];
    private readonly object _persistLock = new();

    public PersistentScheduler(ISchedulerStateRepository repository)
    {
        _repository = repository;
    }

    public async Task LoadAsync()
    {
        var state = await _repository.LoadAsync();
        if (state == null)
            return;

        lock (_persistLock)
        {
            _queuedTaskIds.Clear();
            _queuedTaskIds.AddRange(state.TaskQueue);

            foreach (var projectId in state.RunningProjects)
            {
                _ = MarkRunningAsync(projectId);
            }
        }
    }

    public override async Task EnqueueAsync(OrchestratorTask task)
    {
        await base.EnqueueAsync(task);

        lock (_persistLock)
        {
            _queuedTaskIds.Add(task.Id);
        }

        await PersistStateAsync();
    }

    public override async Task<OrchestratorTask?> DispatchAsync(int cpuAvailable, int memoryAvailableMb, int maxProcesses)
    {
        var task = await base.DispatchAsync(cpuAvailable, memoryAvailableMb, maxProcesses);

        if (task != null)
        {
            lock (_persistLock)
            {
                _queuedTaskIds.Remove(task.Id);
            }

            await PersistStateAsync();
        }

        return task;
    }

    public override async Task MarkRunningAsync(string projectId)
    {
        await base.MarkRunningAsync(projectId);
        await PersistStateAsync();
    }

    public override async Task MarkCompleteAsync(string projectId)
    {
        await base.MarkCompleteAsync(projectId);
        await PersistStateAsync();
    }

    private async Task PersistStateAsync()
    {
        var state = new SchedulerStateDto
        {
            Id = "scheduler_1",
            TaskQueue = [.._queuedTaskIds],
            RunningProjects = [.._runningProjects],
            LastUpdated = DateTimeOffset.UtcNow
        };

        await _repository.SaveAsync(state);
    }
}
