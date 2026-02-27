using FluentAssertions;
using AIOrchestrator.Persistence.Dto;

namespace AIOrchestrator.Persistence.Tests.Dto;

public class OrchestratorTaskDtoTests
{
    [Fact]
    public void OrchestratorTaskDto_includes_priority_and_queued_at()
    {
        // Arrange
        var dto = new OrchestratorTaskDto
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Priority = "High",
            QueuedAt = DateTimeOffset.UtcNow
        };

        // Act & Assert
        dto.Priority.Should().Be("High");
        dto.QueuedAt.Should().NotBeNull();
    }
}
