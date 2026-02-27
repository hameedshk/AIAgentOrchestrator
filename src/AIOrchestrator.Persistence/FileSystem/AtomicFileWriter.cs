namespace AIOrchestrator.Persistence.FileSystem;

public static class AtomicFileWriter
{
    public static async Task WriteAllTextAsync(string path, string content,
                                                CancellationToken ct = default)
    {
        string tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, content, ct);

        if (File.Exists(path))
            File.Replace(tempPath, path, null);
        else
            File.Move(tempPath, path);
    }
}
