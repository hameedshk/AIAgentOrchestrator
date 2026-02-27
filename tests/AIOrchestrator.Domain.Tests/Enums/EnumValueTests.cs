using AIOrchestrator.Domain.Enums;
using FluentAssertions;

namespace AIOrchestrator.Domain.Tests.Enums;

public class EnumValueTests
{
    [Fact]
    public void TaskState_contains_all_spec_states()
    {
        var expected = new[]
        {
            "Created", "Queued", "Planning", "AwaitingPlanApproval",
            "Executing", "AwaitingUserFix", "Retrying", "Paused",
            "Completed", "Failed", "Halted", "Cancelled"
        };
        var actual = Enum.GetNames<TaskState>();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ModelType_has_Claude_and_Codex()
    {
        Enum.GetNames<ModelType>().Should().BeEquivalentTo("Claude", "Codex");
    }
}
