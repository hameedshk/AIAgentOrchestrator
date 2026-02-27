using AIOrchestrator.CliRunner.Configuration;
using AIOrchestrator.CliRunner.Sessions;
using FluentAssertions;

namespace AIOrchestrator.CliRunner.Tests.Sessions;

public class CliSessionIntegrationTests
{
    private static CliSessionFactory BuildFactory()
    {
        var opts = new CliRunnerOptions
        {
            Models =
            [
                new ModelBinaryConfig { ModelName = "test", BinaryPath = "cmd",
                                         DefaultArgs = ["/c"], SilenceTimeoutSeconds = 10 }
            ]
        };
        return new CliSessionFactory(opts);
    }

    [Fact]
    public void CliSessionFactory_creates_session_for_known_model()
    {
        // Arrange
        var factory = BuildFactory();

        // Act
        var session = factory.Create("test");

        // Assert
        session.Should().NotBeNull();
    }

    [Fact]
    public void CliSessionFactory_throws_for_unknown_model()
    {
        var factory = BuildFactory();
        var act = () => factory.Create("unknown_model");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task CliSession_disposes_properly()
    {
        // Arrange
        var factory = BuildFactory();
        var session = factory.Create("test");

        // Act & Assert: dispose the session without exception
        await session.DisposeAsync();
    }
}
