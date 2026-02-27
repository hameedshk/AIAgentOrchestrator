using AIOrchestrator.Planner.Contract;
using FluentAssertions;

namespace AIOrchestrator.Planner.Tests.Contract;

public class PlanContractValidatorTests
{
    private static PlanOutputContract ValidContract(Guid taskId) => new()
    {
        PlanVersion = "1",
        TaskId = taskId.ToString(),
        Steps =
        [
            new PlanStepContract { Index = 0, Type = "Shell", Description = "Build", Command = "dotnet build" }
        ]
    };

    [Fact]
    public void Valid_contract_passes_validation()
    {
        var result = new PlanContractValidator().Validate(ValidContract(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Wrong_planVersion_fails()
    {
        var contract = ValidContract(Guid.NewGuid()) with { PlanVersion = "2" };
        var result = new PlanContractValidator().Validate(contract);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("planVersion"));
    }

    [Fact]
    public void Empty_steps_fails()
    {
        var contract = ValidContract(Guid.NewGuid()) with { Steps = [] };
        var result = new PlanContractValidator().Validate(contract);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Invalid_step_type_fails()
    {
        var contract = ValidContract(Guid.NewGuid());
        contract.Steps[0] = contract.Steps[0] with { Type = "Unknown" };
        var result = new PlanContractValidator().Validate(contract);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Missing_taskId_fails()
    {
        var contract = ValidContract(Guid.NewGuid()) with { TaskId = "" };
        var result = new PlanContractValidator().Validate(contract);
        result.IsValid.Should().BeFalse();
    }
}
