using System.Text.Json.Serialization;

namespace AIOrchestrator.Persistence.Dto;

public sealed class FailureContextDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("rawOutput")]
    public string? RawOutput { get; set; }

    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    [JsonPropertyName("errorHash")]
    public string? ErrorHash { get; set; }

    [JsonPropertyName("retryable")]
    public bool Retryable { get; set; }

    [JsonPropertyName("plannerModel")]
    public string? PlannerModel { get; set; }

    [JsonPropertyName("executorModel")]
    public string? ExecutorModel { get; set; }

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; set; }
}
