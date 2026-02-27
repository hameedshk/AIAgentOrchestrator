using AIOrchestrator.CliRunner.Configuration;
using Microsoft.Extensions.Configuration;
using FluentAssertions;

namespace AIOrchestrator.CliRunner.Tests.Configuration;

public class CliRunnerOptionsTests
{
    [Fact]
    public void CliRunnerOptions_loads_model_binaries_from_config()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CliRunner:DefaultSilenceTimeoutSeconds"] = "30",
                ["CliRunner:Models:0:ModelName"] = "claude",
                ["CliRunner:Models:0:BinaryPath"] = "claude.cmd",
                ["CliRunner:Models:0:SilenceTimeoutSeconds"] = "60",
                ["CliRunner:Models:1:ModelName"] = "codex",
                ["CliRunner:Models:1:BinaryPath"] = "codex.cmd",
                ["CliRunner:Models:1:SilenceTimeoutSeconds"] = "120",
            })
            .Build();

        var opts = config.GetSection(CliRunnerOptions.SectionName).Get<CliRunnerOptions>()!;
        opts.DefaultSilenceTimeoutSeconds.Should().Be(30);
        opts.Models.Should().HaveCount(2);
        opts.Models[0].ModelName.Should().Be("claude");
        opts.Models[0].BinaryPath.Should().Be("claude.cmd");
    }

    [Fact]
    public void GetBinaryPath_returns_path_for_known_model()
    {
        var opts = new CliRunnerOptions
        {
            Models = [new ModelBinaryConfig { ModelName = "claude", BinaryPath = "claude.cmd" }]
        };
        opts.GetBinaryPath("claude").Should().Be("claude.cmd");
    }

    [Fact]
    public void GetBinaryPath_throws_for_unknown_model()
    {
        var opts = new CliRunnerOptions { Models = [] };
        var act = () => opts.GetBinaryPath("unknown");
        act.Should().Throw<InvalidOperationException>();
    }
}
