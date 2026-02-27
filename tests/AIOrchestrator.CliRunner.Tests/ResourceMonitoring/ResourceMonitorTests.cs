using FluentAssertions;
using AIOrchestrator.CliRunner.ResourceMonitoring;

namespace AIOrchestrator.CliRunner.Tests.ResourceMonitoring;

public class ResourceMonitorTests
{
    [Fact]
    public async Task GetSystemResourcesAsync_returns_valid_resource_snapshot()
    {
        var monitor = new ResourceMonitor(maxProcesses: 10);
        var resources = await monitor.GetSystemResourcesAsync();

        resources.Should().NotBeNull();
        resources.CpuUsagePercent.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        resources.AvailableMemoryMb.Should().BeGreaterThan(0);
        resources.RunningProcessCount.Should().BeGreaterThanOrEqualTo(0);
        resources.MaxProcessesAllowed.Should().Be(10);
    }

    [Fact]
    public async Task GetSystemResourcesAsync_counts_running_dotnet_processes()
    {
        var monitor = new ResourceMonitor(maxProcesses: 20);
        var resources = await monitor.GetSystemResourcesAsync();

        resources.RunningProcessCount.Should().BeGreaterThan(0);
        resources.RunningProcessCount.Should().BeLessThanOrEqualTo(resources.MaxProcessesAllowed);
    }
}
