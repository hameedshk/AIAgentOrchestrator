using System.Text.Json.Serialization;

namespace AIOrchestrator.Persistence.Dto;

public sealed class SchedulerStateDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("taskQueue")]
    public List<Guid> TaskQueue { get; set; } = [];

    [JsonPropertyName("runningProjects")]
    public List<string> RunningProjects { get; set; } = [];

    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset? LastUpdated { get; set; }
}
