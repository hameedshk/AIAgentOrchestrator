using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.Engine;

public class EngineTests
{
    [Fact]
    public async Task SubmitTaskAsync_enqueues_task_in_scheduler()
    {
        var scheduler = Substitute.For<IScheduler>();
        var resourceMonitor = Substitute.For<IResourceMonitor>();
        var engine = new global::AIOrchestrator.App.Engine.Engine(scheduler, resourceMonitor);

        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", ProjectId = "ProjectA" };

        var submitted = await engine.SubmitTaskAsync(task);

        submitted.Should().NotBeNull();
        submitted.Id.Should().Be(task.Id);
        await scheduler.Received(1).EnqueueAsync(task);
    }

    [Fact]
    public async Task GetStatusAsync_returns_engine_status()
    {
        var scheduler = Substitute.For<IScheduler>();
        var resourceMonitor = Substitute.For<IResourceMonitor>();
        var resources = new SystemResources { CpuUsagePercent = 45, AvailableMemoryMb = 2048, RunningProcessCount = 3, MaxProcessesAllowed = 10 };
        resourceMonitor.GetSystemResourcesAsync().Returns(resources);

        var engine = new global::AIOrchestrator.App.Engine.Engine(scheduler, resourceMonitor);
        var status = await engine.GetStatusAsync();

        status.Should().NotBeNull();
        status.CpuUsagePercent.Should().Be(45);
        status.AvailableMemoryMb.Should().Be(2048);
    }
}
