using System.Text.Json.Serialization;

namespace AIOrchestrator.Persistence.Dto;

public sealed class FailureContextDto
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("rawOutput")]
    public string? RawOutput { get; set; }

    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; set; }

    [JsonPropertyName("occurredAt")]
    public DateTimeOffset OccurredAt { get; set; }

    [JsonPropertyName("retryAttempt")]
    public int RetryAttempt { get; set; }
}
