using AIOrchestrator.CliRunner.Sentinel;
using FluentAssertions;

namespace AIOrchestrator.CliRunner.Tests.Sentinel;

public class SentinelTests
{
    [Fact]
    public void Inject_appends_echo_command_with_unique_marker()
    {
        var (prompt, marker) = SentinelMarkerInjector.Inject("do something");
        prompt.Should().StartWith("do something");
        prompt.Should().Contain($"echo {marker}");
        marker.Should().StartWith("__ORCHESTRATOR_DONE_");
        marker.Should().EndWith("__");
    }

    [Fact]
    public void Two_injections_produce_different_markers()
    {
        var (_, m1) = SentinelMarkerInjector.Inject("prompt");
        var (_, m2) = SentinelMarkerInjector.Inject("prompt");
        m1.Should().NotBe(m2);
    }

    [Fact]
    public void SentinelDetector_returns_false_for_non_matching_line()
    {
        var (_, marker) = SentinelMarkerInjector.Inject("prompt");
        var detector = new SentinelDetector(marker);
        detector.CheckLine("some random output").Should().BeFalse();
        detector.Detected.Should().BeFalse();
    }

    [Fact]
    public void SentinelDetector_returns_true_when_marker_appears()
    {
        var (_, marker) = SentinelMarkerInjector.Inject("prompt");
        var detector = new SentinelDetector(marker);
        detector.CheckLine($"output {marker} trailing").Should().BeTrue();
        detector.Detected.Should().BeTrue();
    }
}
