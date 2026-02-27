using System.Text.Json.Serialization;

namespace AIOrchestrator.Planner.Contract;

public sealed record PlanStepContract
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("command")]
    public string? Command { get; init; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; init; }

    [JsonPropertyName("expectedOutput")]
    public string? ExpectedOutput { get; init; }
}
