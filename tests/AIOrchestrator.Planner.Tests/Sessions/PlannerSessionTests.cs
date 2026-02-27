using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.CliRunner.Sessions;
using AIOrchestrator.Planner.Sessions;
using FluentAssertions;
using NSubstitute;

namespace AIOrchestrator.Planner.Tests.Sessions;

public class PlannerSessionTests
{
    private static string ValidPlanJson(Guid taskId) => $$"""
        {
          "planVersion": "1",
          "taskId": "{{taskId}}",
          "steps": [{ "index": 0, "type": "Shell", "description": "Build", "command": "dotnet build" }]
        }
        """;

    private static ICliSession MockSession(string output)
    {
        var session = Substitute.For<ICliSession>();
        session.ExecuteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new SessionResult(output, 0, false, true));
        return session;
    }

    private static ICliSessionFactory MockFactory(ICliSession session)
    {
        var factory = Substitute.For<ICliSessionFactory>();
        factory.Create(Arg.Any<string>()).Returns(session);
        return factory;
    }

    [Fact]
    public async Task PlanAsync_returns_valid_plan_on_first_attempt()
    {
        var taskId = Guid.NewGuid();
        var session = MockSession(ValidPlanJson(taskId));
        var factory = MockFactory(session);
        var planner = new PlannerSession(factory, new PlannerSessionOptions { ModelName = "claude" });

        var plan = await planner.PlanAsync(taskId, "Implement feature X");

        plan.PlanVersion.Should().Be("1");
        plan.TaskId.Should().Be(taskId.ToString());
        plan.Steps.Should().HaveCount(1);
    }

    [Fact]
    public async Task PlanAsync_retries_on_invalid_output_and_succeeds()
    {
        var taskId = Guid.NewGuid();
        var badSession = MockSession("not json at all");
        var goodSession = MockSession(ValidPlanJson(taskId));
        var factory = Substitute.For<ICliSessionFactory>();
        factory.Create(Arg.Any<string>()).Returns(badSession, goodSession);

        var planner = new PlannerSession(factory, new PlannerSessionOptions
            { ModelName = "claude", MaxPlannerRetries = 2 });

        var plan = await planner.PlanAsync(taskId, "task");
        plan.PlanVersion.Should().Be("1");
    }

    [Fact]
    public async Task PlanAsync_throws_PlannerOutputException_when_retries_exhausted()
    {
        var taskId = Guid.NewGuid();
        var badSession = MockSession("garbage output");
        var factory = Substitute.For<ICliSessionFactory>();
        factory.Create(Arg.Any<string>()).Returns(badSession);

        var planner = new PlannerSession(factory, new PlannerSessionOptions
            { ModelName = "claude", MaxPlannerRetries = 0 });

        var act = () => planner.PlanAsync(taskId, "task");
        await act.Should().ThrowAsync<PlannerOutputException>();
    }
}
