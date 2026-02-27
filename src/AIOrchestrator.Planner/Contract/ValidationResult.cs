namespace AIOrchestrator.Planner.Contract;

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ValidationResult Ok() => new(true, []);
    public static ValidationResult Fail(IEnumerable<string> errors) => new(false, errors.ToList());
}
