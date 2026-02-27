using System.Text.Json.Serialization;

namespace AIOrchestrator.Persistence.Dto;

public sealed class OrchestratorTaskDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("planner")]
    public string Planner { get; set; } = string.Empty;

    [JsonPropertyName("executor")]
    public string Executor { get; set; } = string.Empty;

    [JsonPropertyName("steps")]
    public List<ExecutionStepDto> Steps { get; set; } = [];

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }

    [JsonPropertyName("lastFailure")]
    public FailureContextDto? LastFailure { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }
}
