namespace AIOrchestrator.App.Api.ResponseModels;

public sealed class TaskResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Planner { get; set; } = string.Empty;
    public string Executor { get; set; } = string.Empty;
    public int CurrentStepIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
