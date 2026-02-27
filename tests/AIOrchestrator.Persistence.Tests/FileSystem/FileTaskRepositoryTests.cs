using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Persistence.FileSystem;
using FluentAssertions;

namespace AIOrchestrator.Persistence.Tests.FileSystem;

public class FileTaskRepositoryTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly FileTaskRepository _repo;

    public FileTaskRepositoryTests()
    {
        Directory.CreateDirectory(_tempDir);
        _repo = new FileTaskRepository(new TaskStorePaths(_tempDir));
    }
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private static OrchestratorTask NewTask(Guid? id = null) =>
        new() { Id = id ?? Guid.NewGuid(), Title = "Test task",
                Planner = ModelType.Claude, Executor = ModelType.Codex };

    [Fact]
    public async Task SaveAsync_and_LoadAsync_roundtrip_preserves_state()
    {
        var task = NewTask();
        task.Enqueue();

        await _repo.SaveAsync(task);
        var loaded = await _repo.LoadAsync(task.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(task.Id);
        loaded.Title.Should().Be("Test task");
        loaded.State.Should().Be(TaskState.Queued);
        loaded.Planner.Should().Be(ModelType.Claude);
        loaded.Executor.Should().Be(ModelType.Codex);
    }

    [Fact]
    public async Task LoadAsync_returns_null_for_unknown_id()
    {
        var result = await _repo.LoadAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_returns_true_after_save()
    {
        var task = NewTask();
        await _repo.SaveAsync(task);
        (await _repo.ExistsAsync(task.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAllAsync_returns_all_saved_tasks()
    {
        var t1 = NewTask();
        var t2 = NewTask();
        await _repo.SaveAsync(t1);
        await _repo.SaveAsync(t2);
        var all = await _repo.LoadAllAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_removes_the_file()
    {
        var task = NewTask();
        await _repo.SaveAsync(task);
        await _repo.DeleteAsync(task.Id);
        (await _repo.ExistsAsync(task.Id)).Should().BeFalse();
    }
}
