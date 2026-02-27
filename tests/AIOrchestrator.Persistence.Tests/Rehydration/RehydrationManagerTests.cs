using AIOrchestrator.Persistence.Rehydration;
using FluentAssertions;

namespace AIOrchestrator.Persistence.Tests.Rehydration;

public class RehydrationManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly RehydrationManager _manager;

    public RehydrationManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _manager = new RehydrationManager(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch { }
    }

    [Fact]
    public async Task SaveSessionState_persists_executor_session_metadata()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var state = new ExecutorSessionState(
            TaskId: taskId,
            CurrentStepIndex: 3,
            LastSnapshotCommitSha: "abc123",
            LastSavedAt: DateTimeOffset.UtcNow
        );

        // Act
        await _manager.SaveSessionStateAsync(state);

        // Assert
        var loaded = await _manager.LoadSessionStateAsync(taskId);
        loaded.Should().NotBeNull();
        loaded!.TaskId.Should().Be(taskId);
        loaded.CurrentStepIndex.Should().Be(3);
        loaded.LastSnapshotCommitSha.Should().Be("abc123");
    }

    [Fact]
    public async Task LoadSessionState_returns_null_if_not_found()
    {
        // Act
        var state = await _manager.LoadSessionStateAsync(Guid.NewGuid());

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSessionState_removes_persisted_state()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var state = new ExecutorSessionState(
            TaskId: taskId,
            CurrentStepIndex: 0,
            LastSnapshotCommitSha: "abc",
            LastSavedAt: DateTimeOffset.UtcNow
        );
        await _manager.SaveSessionStateAsync(state);

        // Act
        await _manager.DeleteSessionStateAsync(taskId);

        // Assert
        var loaded = await _manager.LoadSessionStateAsync(taskId);
        loaded.Should().BeNull();
    }
}
