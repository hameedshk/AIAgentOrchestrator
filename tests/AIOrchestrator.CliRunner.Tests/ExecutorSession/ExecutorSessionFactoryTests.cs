using AIOrchestrator.CliRunner.Configuration;
using AIOrchestrator.CliRunner.ExecutorSession;
using AIOrchestrator.Domain.Enums;
using FluentAssertions;

namespace AIOrchestrator.CliRunner.Tests.ExecutorSession;

public class ExecutorSessionFactoryTests
{
    private static CliRunnerOptions BuildOptions()
    {
        return new CliRunnerOptions
        {
            Models =
            [
                new ModelBinaryConfig { ModelName = "claude", BinaryPath = "cmd", DefaultArgs = ["/c"] },
                new ModelBinaryConfig { ModelName = "codex", BinaryPath = "cmd", DefaultArgs = ["/c"] }
            ]
        };
    }

    [Fact]
    public void ExecutorSessionFactory_creates_session_with_correct_model()
    {
        // Arrange
        var factory = new ExecutorSessionFactory(BuildOptions());
        var taskId = Guid.NewGuid();

        // Act
        var session = factory.Create(taskId, ModelType.Claude);

        // Assert
        session.Should().NotBeNull();
        session.Should().BeAssignableTo<IExecutorSession>();
    }

    [Fact]
    public void ExecutorSessionFactory_creates_different_sessions_for_different_task_ids()
    {
        // Arrange
        var factory = new ExecutorSessionFactory(BuildOptions());
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();

        // Act
        var session1 = factory.Create(taskId1, ModelType.Claude);
        var session2 = factory.Create(taskId2, ModelType.Claude);

        // Assert
        session1.Should().NotBe(session2);
    }

    [Fact]
    public void ExecutorSessionFactory_throws_for_unsupported_model()
    {
        // Arrange
        var factory = new ExecutorSessionFactory(new CliRunnerOptions { Models = [] });

        // Act & Assert
        var act = () => factory.Create(Guid.NewGuid(), ModelType.Claude);
        act.Should().Throw<InvalidOperationException>();
    }
}
