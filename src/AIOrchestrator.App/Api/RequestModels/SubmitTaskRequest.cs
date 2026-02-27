namespace AIOrchestrator.App.Api.RequestModels;

public sealed class SubmitTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public bool AllowReplan { get; set; } = false;
}
