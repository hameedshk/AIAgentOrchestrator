namespace AIOrchestrator.Persistence.FileSystem;

public sealed class TaskStorePaths(string baseDirectory)
{
    public string GetTaskFilePath(Guid taskId) =>
        Path.Combine(baseDirectory, "tasks", $"{taskId:N}.json");

    public string GetTaskDirectory() =>
        Path.Combine(baseDirectory, "tasks");
}
