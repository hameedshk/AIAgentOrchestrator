using System.Text.Json;
using System.Text.Json.Serialization;
using AIOrchestrator.Persistence.FileSystem;

namespace AIOrchestrator.Persistence.Rehydration;

/// <summary>
/// Manages persisted executor session state for crash recovery.
/// </summary>
public sealed class RehydrationManager(string baseDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private string GetSessionStateFilePath(Guid taskId) =>
        Path.Combine(baseDirectory, "rehydration", $"{taskId:N}.json");

    public async Task SaveSessionStateAsync(ExecutorSessionState state, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.Combine(baseDirectory, "rehydration"));

        var dto = new ExecutorSessionStateDto
        {
            TaskId = state.TaskId.ToString(),
            CurrentStepIndex = state.CurrentStepIndex,
            LastSnapshotCommitSha = state.LastSnapshotCommitSha,
            LastSavedAt = state.LastSavedAt
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var filePath = GetSessionStateFilePath(state.TaskId);

        await AtomicFileWriter.WriteAllTextAsync(filePath, json, ct);
    }

    public async Task<ExecutorSessionState?> LoadSessionStateAsync(Guid taskId, CancellationToken ct = default)
    {
        var filePath = GetSessionStateFilePath(taskId);

        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath, ct);
        var dto = JsonSerializer.Deserialize<ExecutorSessionStateDto>(json, JsonOptions);

        if (dto == null)
            return null;

        return new ExecutorSessionState(
            TaskId: Guid.Parse(dto.TaskId),
            CurrentStepIndex: dto.CurrentStepIndex,
            LastSnapshotCommitSha: dto.LastSnapshotCommitSha,
            LastSavedAt: dto.LastSavedAt
        );
    }

    public async Task DeleteSessionStateAsync(Guid taskId, CancellationToken ct = default)
    {
        var filePath = GetSessionStateFilePath(taskId);
        if (File.Exists(filePath))
            await Task.Run(() => File.Delete(filePath), ct);
    }
}

/// <summary>
/// DTO for JSON serialization of executor session state.
/// </summary>
internal sealed class ExecutorSessionStateDto
{
    [JsonPropertyName("taskId")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("currentStepIndex")]
    public int CurrentStepIndex { get; set; }

    [JsonPropertyName("lastSnapshotCommitSha")]
    public string LastSnapshotCommitSha { get; set; } = string.Empty;

    [JsonPropertyName("lastSavedAt")]
    public DateTimeOffset LastSavedAt { get; set; }
}
