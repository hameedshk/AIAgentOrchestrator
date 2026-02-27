using FluentAssertions;
using NSubstitute;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.CliRunner.Tests.Abstractions;

public class IResourceMonitorTests
{
    [Fact]
    public async Task GetSystemResourcesAsync_returns_resource_snapshot()
    {
        // Arrange
        var monitor = Substitute.For<IResourceMonitor>();
        var resources = new SystemResources
        {
            CpuUsagePercent = 45,
            AvailableMemoryMb = 2048,
            RunningProcessCount = 5,
            MaxProcessesAllowed = 10
        };
        monitor.GetSystemResourcesAsync().Returns(resources);

        // Act
        var result = await monitor.GetSystemResourcesAsync();

        // Assert
        result.CpuUsagePercent.Should().Be(45);
        result.AvailableMemoryMb.Should().Be(2048);
        result.RunningProcessCount.Should().Be(5);
    }

    [Fact]
    public void SystemResources_HasResourcesAvailable_checks_thresholds()
    {
        // Arrange
        var resources = new SystemResources
        {
            CpuUsagePercent = 45,
            AvailableMemoryMb = 2048,
            RunningProcessCount = 5,
            MaxProcessesAllowed = 10
        };

        // Act & Assert
        resources.HasResourcesAvailable(80, 512).Should().BeTrue();
        resources.HasResourcesAvailable(40, 512).Should().BeFalse(); // CPU too high
        resources.HasResourcesAvailable(80, 3000).Should().BeFalse(); // Memory too low
    }
}
