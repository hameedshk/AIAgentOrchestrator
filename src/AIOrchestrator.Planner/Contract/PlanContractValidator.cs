namespace AIOrchestrator.Planner.Contract;

public sealed class PlanContractValidator
{
    public ValidationResult Validate(PlanOutputContract contract)
    {
        var errors = new List<string>();

        if (contract.PlanVersion != "1")
            errors.Add("planVersion must be '1'");

        if (string.IsNullOrWhiteSpace(contract.TaskId))
            errors.Add("taskId is required and must not be empty");

        if (contract.Steps.Count == 0)
            errors.Add("steps must not be empty");

        foreach (var step in contract.Steps)
        {
            if (step.Type is not ("Shell" or "Agent"))
                errors.Add($"Step {step.Index}: type must be 'Shell' or 'Agent', got '{step.Type}'");
        }

        return errors.Count == 0 ? ValidationResult.Ok() : ValidationResult.Fail(errors);
    }
}
