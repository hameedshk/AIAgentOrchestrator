using FluentAssertions;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

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
    public async Task Scheduler_respects_project_isolation()
    {
        var scheduler = new global::AIOrchestrator.App.Scheduler.Scheduler();

        var task1 = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Task1", ProjectId = "ProjectA", Priority = TaskPriority.High };
        var task2 = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Task2", ProjectId = "ProjectA", Priority = TaskPriority.Normal };

        await scheduler.EnqueueAsync(task1);
        await scheduler.EnqueueAsync(task2);

        var dispatch1 = await scheduler.DispatchAsync(100, 2048, 10);
        dispatch1!.Id.Should().Be(task1.Id);

        // Mark project as running
        await scheduler.MarkRunningAsync("ProjectA");

        // Should not dispatch another task from same project
        var dispatch2 = await scheduler.DispatchAsync(100, 2048, 10);
        dispatch2.Should().BeNull();

        // Complete the project
        await scheduler.MarkCompleteAsync("ProjectA");

        // Now should dispatch the waiting task
        var dispatch3 = await scheduler.DispatchAsync(100, 2048, 10);
        dispatch3!.Id.Should().Be(task2.Id);
    }
}
