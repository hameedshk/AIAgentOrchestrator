namespace AIOrchestrator.Planner.Sessions;

public sealed class PlannerOutputException(string message, int attemptsCount)
    : Exception(message)
{
    public int AttemptsCount { get; } = attemptsCount;
}
