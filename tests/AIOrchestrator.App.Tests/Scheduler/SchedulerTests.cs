using FluentAssertions;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.Scheduler;

public class SchedulerTests
{
    [Fact]
    public async Task EnqueueAsync_adds_task_to_queue()
    {
        var scheduler = new App.Scheduler.Scheduler();
        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", Priority = TaskPriority.Normal };

        await scheduler.EnqueueAsync(task);

        var dispatched = await scheduler.DispatchAsync(100, 2048, 10);
        dispatched.Should().NotBeNull();
        dispatched!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task DispatchAsync_returns_highest_priority_task()
    {
        var scheduler = new App.Scheduler.Scheduler();
        var lowTask = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Low", Priority = TaskPriority.Low };
        var highTask = new OrchestratorTask { Id = Guid.NewGuid(), Title = "High", Priority = TaskPriority.High };

        await scheduler.EnqueueAsync(lowTask);
        await scheduler.EnqueueAsync(highTask);
        var dispatched = await scheduler.DispatchAsync(100, 2048, 10);

        dispatched!.Id.Should().Be(highTask.Id);
    }

    [Fact]
    public async Task DispatchAsync_respects_project_mutual_exclusion()
    {
        var scheduler = new App.Scheduler.Scheduler();
        var projectId = "ProjectA";

        await scheduler.MarkRunningAsync(projectId);

        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", ProjectId = projectId };
        await scheduler.EnqueueAsync(task);
        var dispatched = await scheduler.DispatchAsync(100, 2048, 10);

        dispatched.Should().BeNull();
    }

    [Fact]
    public async Task MarkCompleteAsync_frees_project_for_next_task()
    {
        var scheduler = new App.Scheduler.Scheduler();
        var projectId = "ProjectA";
        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", ProjectId = projectId };

        await scheduler.MarkRunningAsync(projectId);
        await scheduler.EnqueueAsync(task);
        var dispatched1 = await scheduler.DispatchAsync(100, 2048, 10);

        await scheduler.MarkCompleteAsync(projectId);
        var dispatched2 = await scheduler.DispatchAsync(100, 2048, 10);

        dispatched1.Should().BeNull();
        dispatched2.Should().NotBeNull();
    }
}
