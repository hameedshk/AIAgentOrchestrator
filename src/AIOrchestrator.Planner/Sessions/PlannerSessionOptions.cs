namespace AIOrchestrator.Planner.Sessions;

public sealed class PlannerSessionOptions
{
    public const string SectionName = "Planner";

    public int MaxPlannerRetries { get; init; } = 2;
    public string ModelName { get; init; } = "claude";
}
