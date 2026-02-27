using FluentAssertions;
using AIOrchestrator.CliRunner.FailureClassification;

namespace AIOrchestrator.CliRunner.Tests.FailureClassification;

public class LoopGuardTests
{
    private readonly ILoopGuard _guard = new LoopGuard();

    [Fact]
    public void LoopGuard_allows_retry_within_maxRetriesPerStep_limit()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var stepIndex = 0;
        var maxRetriesPerStep = 3;

        // Act & Assert
        _guard.CanRetry(taskId, stepIndex, retryCount: 0, errorHash: "abc", maxRetriesPerStep, maxLoopsPerTask: 10)
            .Should().BeTrue();
        _guard.CanRetry(taskId, stepIndex, retryCount: 1, errorHash: "abc", maxRetriesPerStep, maxLoopsPerTask: 10)
            .Should().BeTrue();
        _guard.CanRetry(taskId, stepIndex, retryCount: 2, errorHash: "abc", maxRetriesPerStep, maxLoopsPerTask: 10)
            .Should().BeTrue();
        _guard.CanRetry(taskId, stepIndex, retryCount: 3, errorHash: "abc", maxRetriesPerStep, maxLoopsPerTask: 10)
            .Should().BeFalse("at limit");
    }

    [Fact]
    public void LoopGuard_blocks_retry_if_same_error_hash_on_consecutive_attempts()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var stepIndex = 0;
        var errorHash = "deadbeef";

        // Act
        var canRetry1 = _guard.CanRetry(taskId, stepIndex, retryCount: 0, errorHash: errorHash, maxRetriesPerStep: 3, maxLoopsPerTask: 10);
        var canRetry2 = _guard.CanRetry(taskId, stepIndex, retryCount: 1, errorHash: errorHash, maxRetriesPerStep: 3, maxLoopsPerTask: 10);

        // Assert
        canRetry1.Should().BeTrue();
        canRetry2.Should().BeFalse("same error hash means retry would not help");
    }

    [Fact]
    public void LoopGuard_allows_retry_if_error_hash_changes()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var stepIndex = 0;

        // Act
        var canRetry1 = _guard.CanRetry(taskId, stepIndex, retryCount: 0, errorHash: "hash1", maxRetriesPerStep: 3, maxLoopsPerTask: 10);
        var canRetry2 = _guard.CanRetry(taskId, stepIndex, retryCount: 1, errorHash: "hash2", maxRetriesPerStep: 3, maxLoopsPerTask: 10);

        // Assert
        canRetry1.Should().BeTrue();
        canRetry2.Should().BeTrue("different error hash indicates different problem");
    }

    [Fact]
    public void LoopGuard_tracks_loop_count_across_steps()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var maxLoopsPerTask = 3;

        // Act & Assert - simulate retries across multiple steps
        _guard.CanRetry(taskId, stepIndex: 0, retryCount: 0, errorHash: "a", maxRetriesPerStep: 3, maxLoopsPerTask)
            .Should().BeTrue();
        _guard.CanRetry(taskId, stepIndex: 0, retryCount: 1, errorHash: "b", maxRetriesPerStep: 3, maxLoopsPerTask)
            .Should().BeTrue();
        _guard.CanRetry(taskId, stepIndex: 1, retryCount: 0, errorHash: "c", maxRetriesPerStep: 3, maxLoopsPerTask)
            .Should().BeTrue();
        _guard.CanRetry(taskId, stepIndex: 1, retryCount: 1, errorHash: "d", maxRetriesPerStep: 3, maxLoopsPerTask)
            .Should().BeFalse("total loop count exceeded");
    }

    [Fact]
    public void LoopGuard_tracks_state_per_task_independently()
    {
        // Arrange
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();

        // Act
        var task1_retry1 = _guard.CanRetry(task1Id, 0, 0, "hash1", maxRetriesPerStep: 2, maxLoopsPerTask: 3);
        var task1_retry2 = _guard.CanRetry(task1Id, 0, 1, "hash1", maxRetriesPerStep: 2, maxLoopsPerTask: 3);
        var task2_retry1 = _guard.CanRetry(task2Id, 0, 0, "hash1", maxRetriesPerStep: 2, maxLoopsPerTask: 3);

        // Assert
        task1_retry1.Should().BeTrue();
        task1_retry2.Should().BeFalse("same error on task1");
        task2_retry1.Should().BeTrue("task2 state is independent");
    }

    [Fact]
    public void LoopGuard_Reset_clears_state_for_task()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _guard.CanRetry(taskId, 0, 0, "hash1", maxRetriesPerStep: 3, maxLoopsPerTask: 10);
        _guard.CanRetry(taskId, 0, 1, "hash1", maxRetriesPerStep: 3, maxLoopsPerTask: 10);

        // Act
        _guard.Reset(taskId);

        // Assert - should be able to retry again after reset
        _guard.CanRetry(taskId, 0, 0, "hash1", maxRetriesPerStep: 3, maxLoopsPerTask: 10)
            .Should().BeTrue("state was reset");
    }
}
