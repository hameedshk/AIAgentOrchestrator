using FluentAssertions;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Persistence.FileSystem;

namespace AIOrchestrator.App.Tests.Scheduler;

public class SchedulerIntegrationTests
{
    [Fact]
    public async Task Scheduler_manages_multiple_projects_concurrently()
    {
        var scheduler = new global::AIOrchestrator.App.Scheduler.Scheduler();

        var taskA = new OrchestratorTask { Id = Guid.NewGuid(), Title = "A", ProjectId = "ProjectA", Priority = TaskPriority.High };
        var taskB = new OrchestratorTask { Id = Guid.NewGuid(), Title = "B", ProjectId = "ProjectB", Priority = TaskPriority.Low };
        var taskC = new OrchestratorTask { Id = Guid.NewGuid(), Title = "C", ProjectId = "ProjectC", Priority = TaskPriority.Normal };

        await scheduler.EnqueueAsync(taskB);
        await scheduler.EnqueueAsync(taskA);
        await scheduler.EnqueueAsync(taskC);

        var dispatch1 = await scheduler.DispatchAsync(100, 2048, 10);
        await scheduler.MarkRunningAsync(dispatch1!.ProjectId);

        var dispatch2 = await scheduler.DispatchAsync(100, 2048, 10);
        await scheduler.MarkRunningAsync(dispatch2!.ProjectId);

        var dispatch3 = await scheduler.DispatchAsync(100, 2048, 10);

        dispatch1.Should().NotBeNull();
        dispatch2.Should().NotBeNull();
        dispatch3.Should().NotBeNull();

        dispatch1!.Id.Should().Be(taskA.Id);
        dispatch2!.Id.Should().Be(taskC.Id);
        dispatch3!.Id.Should().Be(taskB.Id);
    }

    [Fact]
    public async Task PersistentScheduler_recovers_from_crash()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var repository = new FileSystemSchedulerStateRepository(tempDir);
        var scheduler1 = new PersistentScheduler(repository);

        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", ProjectId = "ProjectA" };

        await scheduler1.EnqueueAsync(task);
        await scheduler1.MarkRunningAsync("ProjectA");

        // Simulate restart
        var scheduler2 = new PersistentScheduler(repository);
        await scheduler2.LoadAsync();

        var newTask = new OrchestratorTask { Id = Guid.NewGuid(), Title = "New", ProjectId = "ProjectB" };
        await scheduler2.EnqueueAsync(newTask);

        var dispatched = await scheduler2.DispatchAsync(100, 2048, 10);

        dispatched!.Id.Should().Be(newTask.Id);

        Directory.Delete(tempDir, true);
    }
}
