using System.Text.Json;
using System.Text.Json.Serialization;
using AIOrchestrator.Persistence.Dto;

namespace AIOrchestrator.Persistence.Json;

public sealed class TaskJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize(OrchestratorTaskDto dto)
    {
        return JsonSerializer.Serialize(dto, Options);
    }

    public OrchestratorTaskDto? Deserialize(string json)
    {
        return JsonSerializer.Deserialize<OrchestratorTaskDto>(json, Options);
    }
}
