using System.Text.Json;
using AIOrchestrator.Persistence.Abstractions;
using AIOrchestrator.Persistence.Dto;

namespace AIOrchestrator.Persistence.FileSystem;

public class FileSystemSchedulerStateRepository : ISchedulerStateRepository
{
    private readonly string _stateDir;
    private readonly string _stateFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public FileSystemSchedulerStateRepository(string stateDir)
    {
        _stateDir = stateDir;
        _stateFilePath = Path.Combine(stateDir, "scheduler_state.json");
        Directory.CreateDirectory(stateDir);
    }

    public async Task SaveAsync(SchedulerStateDto state)
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        var tempFile = _stateFilePath + ".tmp";
        await File.WriteAllTextAsync(tempFile, json);

        if (File.Exists(_stateFilePath))
            File.Delete(_stateFilePath);
        File.Move(tempFile, _stateFilePath, overwrite: true);
    }

    public async Task<SchedulerStateDto?> LoadAsync()
    {
        if (!File.Exists(_stateFilePath))
            return null;

        var json = await File.ReadAllTextAsync(_stateFilePath);
        return JsonSerializer.Deserialize<SchedulerStateDto>(json);
    }
}
