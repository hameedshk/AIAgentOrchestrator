using AIOrchestrator.App.Scheduler;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Engine;

/// <summary>
/// Central orchestration engine coordinating Scheduler, Resources, and Task Execution.
/// </summary>
public class Engine : IEngine
{
    private readonly IScheduler _scheduler;
    private readonly IResourceMonitor _resourceMonitor;
    private readonly List<OrchestratorTask> _allTasks = [];
    private DateTimeOffset _lastDispatchTime = DateTimeOffset.UtcNow;
    private readonly object _tasksLock = new();

    private const int CpuThresholdPercent = 80;
    private const int MemoryThresholdMb = 512;
    private const int DispatchIntervalMs = 1000;

    public Engine(IScheduler scheduler, IResourceMonitor resourceMonitor)
    {
        _scheduler = scheduler;
        _resourceMonitor = resourceMonitor;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var resources = await _resourceMonitor.GetSystemResourcesAsync();

                if (resources.HasResourcesAvailable(CpuThresholdPercent, MemoryThresholdMb))
                {
                    var task = await _scheduler.DispatchAsync(
                        cpuAvailable: 100 - resources.CpuUsagePercent,
                        memoryAvailableMb: resources.AvailableMemoryMb,
                        maxProcesses: resources.MaxProcessesAllowed - resources.RunningProcessCount);

                    if (task != null)
                    {
                        await MarkTaskRunningAsync(task);
                        _lastDispatchTime = DateTimeOffset.UtcNow;
                    }
                }

                await Task.Delay(DispatchIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Engine dispatch error: {ex.Message}");
            }
        }
    }

    public async Task<OrchestratorTask> SubmitTaskAsync(OrchestratorTask task)
    {
        task.Enqueue();

        lock (_tasksLock)
        {
            _allTasks.Add(task);
        }

        await _scheduler.EnqueueAsync(task);
        return task;
    }

    public Task<IReadOnlyList<OrchestratorTask>> GetTasksByStateAsync(TaskState state)
    {
        lock (_tasksLock)
        {
            var tasks = _allTasks.Where(t => t.State == state).ToList().AsReadOnly();
            return Task.FromResult((IReadOnlyList<OrchestratorTask>)tasks);
        }
    }

    public async Task<EngineStatus> GetStatusAsync()
    {
        var resources = await _resourceMonitor.GetSystemResourcesAsync();

        lock (_tasksLock)
        {
            return new EngineStatus
            {
                TotalTasks = _allTasks.Count,
                QueuedTasks = _allTasks.Count(t => t.State == TaskState.Queued),
                ExecutingTasks = _allTasks.Count(t => t.State == TaskState.Executing),
                CompletedTasks = _allTasks.Count(t => t.State == TaskState.Completed),
                FailedTasks = _allTasks.Count(t => t.State == TaskState.Failed),
                CpuUsagePercent = resources.CpuUsagePercent,
                AvailableMemoryMb = resources.AvailableMemoryMb,
                RunningProcessCount = resources.RunningProcessCount,
                LastDispatchTime = _lastDispatchTime
            };
        }
    }

    private async Task MarkTaskRunningAsync(OrchestratorTask task)
    {
        task.StartExecuting();

        lock (_tasksLock)
        {
            var existing = _allTasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing != null)
            {
                var index = _allTasks.IndexOf(existing);
                _allTasks[index] = task;
            }
        }

        if (!string.IsNullOrEmpty(task.ProjectId))
        {
            await _scheduler.MarkRunningAsync(task.ProjectId);
        }
    }
}
