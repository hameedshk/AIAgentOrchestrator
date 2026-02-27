using FluentAssertions;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.Domain.Tests.Enums;

public class TaskPriorityTests
{
    [Fact]
    public void TaskPriority_defines_three_levels()
    {
        // Act & Assert
        TaskPriority.High.Should().Be(TaskPriority.High);
        TaskPriority.Normal.Should().Be(TaskPriority.Normal);
        TaskPriority.Low.Should().Be(TaskPriority.Low);
    }

    [Fact]
    public void TaskPriority_has_numeric_values_for_comparison()
    {
        // Act & Assert
        ((int)TaskPriority.High).Should().BeGreaterThan((int)TaskPriority.Normal);
        ((int)TaskPriority.Normal).Should().BeGreaterThan((int)TaskPriority.Low);
    }
}
