using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.Engine;

public class EngineIntegrationTests
{
    [Fact]
    public async Task Engine_coordinates_full_task_lifecycle()
    {
        var scheduler = new global::AIOrchestrator.App.Scheduler.Scheduler();
        var resourceMonitor = Substitute.For<IResourceMonitor>();
        var resources = new SystemResources { CpuUsagePercent = 45, AvailableMemoryMb = 2048, RunningProcessCount = 3, MaxProcessesAllowed = 10 };
        resourceMonitor.GetSystemResourcesAsync().Returns(resources);

        var engine = new global::AIOrchestrator.App.Engine.Engine(scheduler, resourceMonitor);

        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Integration", ProjectId = "ProjectA", Priority = TaskPriority.Normal };

        var submitted = await engine.SubmitTaskAsync(task);
        submitted.State.Should().Be(TaskState.Queued);

        var dispatched = await scheduler.DispatchAsync(100, 2048, 10);
        dispatched.Should().NotBeNull();

        await engine.CompleteTaskAsync(dispatched!);
        var status = await engine.GetStatusAsync();

        status.CompletedTasks.Should().Be(1);
    }

    [Fact]
    public async Task Engine_respects_resource_thresholds()
    {
        var scheduler = new global::AIOrchestrator.App.Scheduler.Scheduler();
        var resourceMonitor = Substitute.For<IResourceMonitor>();
        var resources = new SystemResources { CpuUsagePercent = 45, AvailableMemoryMb = 2048, RunningProcessCount = 3, MaxProcessesAllowed = 10 };
        resourceMonitor.GetSystemResourcesAsync().Returns(resources);

        var engine = new global::AIOrchestrator.App.Engine.Engine(scheduler, resourceMonitor);

        var status = await engine.GetStatusAsync();

        status.CpuUsagePercent.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        status.AvailableMemoryMb.Should().BeGreaterThan(0);
    }
}
