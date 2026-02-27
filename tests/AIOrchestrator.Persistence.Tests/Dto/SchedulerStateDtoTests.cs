using FluentAssertions;
using AIOrchestrator.Persistence.Dto;

namespace AIOrchestrator.Persistence.Tests.Dto;

public class SchedulerStateDtoTests
{
    [Fact]
    public void SchedulerStateDto_serializes_task_queue()
    {
        var dto = new SchedulerStateDto
        {
            Id = "scheduler_1",
            TaskQueue = [Guid.NewGuid(), Guid.NewGuid()],
            RunningProjects = ["ProjectA"],
            LastUpdated = DateTimeOffset.UtcNow
        };

        dto.TaskQueue.Should().HaveCount(2);
        dto.RunningProjects.Should().Contain("ProjectA");
    }

    [Fact]
    public void SchedulerStateDto_initializes_with_empty_collections()
    {
        var dto = new SchedulerStateDto { Id = "scheduler_1" };
        dto.TaskQueue.Should().BeEmpty();
        dto.RunningProjects.Should().BeEmpty();
    }
}
