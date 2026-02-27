using FluentAssertions;
using AIOrchestrator.Persistence.FileSystem;
using AIOrchestrator.Persistence.Dto;
using System.Text.Json;

namespace AIOrchestrator.Persistence.Tests.FileSystem;

public class FileSystemSchedulerStateRepositoryTests
{
    private readonly string _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public FileSystemSchedulerStateRepositoryTests()
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task SaveAsync_writes_state_to_file()
    {
        var repository = new FileSystemSchedulerStateRepository(_testDir);
        var state = new SchedulerStateDto
        {
            Id = "scheduler_1",
            TaskQueue = [Guid.NewGuid()],
            RunningProjects = ["ProjectA"],
            LastUpdated = DateTimeOffset.UtcNow
        };

        await repository.SaveAsync(state);

        var filePath = Path.Combine(_testDir, "scheduler_state.json");
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_returns_null_when_file_missing()
    {
        var repository = new FileSystemSchedulerStateRepository(_testDir);
        var result = await repository.LoadAsync();
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_reads_state_from_file()
    {
        var repository = new FileSystemSchedulerStateRepository(_testDir);
        var state = new SchedulerStateDto { Id = "scheduler_1", TaskQueue = [Guid.NewGuid()] };
        await repository.SaveAsync(state);

        var result = await repository.LoadAsync();

        result.Should().NotBeNull();
        result!.Id.Should().Be("scheduler_1");
    }
}
