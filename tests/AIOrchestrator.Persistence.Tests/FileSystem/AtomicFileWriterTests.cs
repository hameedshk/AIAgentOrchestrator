using AIOrchestrator.Persistence.FileSystem;
using FluentAssertions;

namespace AIOrchestrator.Persistence.Tests.FileSystem;

public class AtomicFileWriterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public AtomicFileWriterTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task WriteAllTextAsync_creates_file_with_expected_content()
    {
        var path = Path.Combine(_tempDir, "test.json");
        await AtomicFileWriter.WriteAllTextAsync(path, "{\"key\":\"value\"}");
        var content = await File.ReadAllTextAsync(path);
        content.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public async Task WriteAllTextAsync_replaces_existing_file()
    {
        var path = Path.Combine(_tempDir, "test.json");
        await AtomicFileWriter.WriteAllTextAsync(path, "first");
        await AtomicFileWriter.WriteAllTextAsync(path, "second");
        (await File.ReadAllTextAsync(path)).Should().Be("second");
    }

    [Fact]
    public async Task No_temp_file_left_after_write()
    {
        var path = Path.Combine(_tempDir, "test.json");
        await AtomicFileWriter.WriteAllTextAsync(path, "content");
        File.Exists(path + ".tmp").Should().BeFalse();
    }
}
