using AIOrchestrator.Planner.Contract;

namespace AIOrchestrator.Planner.Abstractions;

public interface IPlannerSession
{
    Task<PlanOutputContract> PlanAsync(Guid taskId, string taskDescription,
                                        CancellationToken ct = default);
}
