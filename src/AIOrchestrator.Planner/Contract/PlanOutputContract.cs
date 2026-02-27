using System.Text.Json.Serialization;

namespace AIOrchestrator.Planner.Contract;

public sealed record PlanOutputContract(
    [property: JsonPropertyName("planVersion")] string PlanVersion,
    [property: JsonPropertyName("taskId")] string TaskId,
    [property: JsonPropertyName("steps")] List<PlanStepContract> Steps
)
{
    public PlanOutputContract() : this(string.Empty, string.Empty, [])
    {
    }
}
