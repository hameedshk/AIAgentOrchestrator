using FluentAssertions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.CliRunner.FailureClassification;

namespace AIOrchestrator.CliRunner.Tests.FailureClassification;

public class FailureClassificationIntegrationTests
{
    private readonly IFailureClassifier _classifier = new FailureClassifier();
    private readonly ILoopGuard _guard = new LoopGuard();

    [Fact]
    public void Full_pipeline_classifies_and_prevents_retry_on_non_retryable_error()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var stepIndex = 0;
        var output = "error CS0103: The name 'foo' does not exist";

        // Act 1: Classify error
        var failure1 = _classifier.Classify(1, output, ModelType.Claude, ModelType.Claude);

        // Assert 1: Correctly classified as non-retryable
        failure1.Type.Should().Be(FailureType.CompileError);
        failure1.Retryable.Should().BeFalse();
        failure1.ErrorHash.Should().HaveLength(64);

        // Act 2: Since the error is non-retryable, we should not even call loop guard
        // The integration is: classifier says non-retryable -> skip retry attempt
        // The loop guard is only consulted for retryable errors

        // Assert 2: Application logic should skip LoopGuard check for non-retryable
        if (failure1.Retryable)
        {
            var canRetry = _guard.CanRetry(
                taskId,
                stepIndex,
                retryCount: 0,
                errorHash: failure1.ErrorHash,
                maxRetriesPerStep: 3,
                maxLoopsPerTask: 10);
            canRetry.Should().BeTrue("First attempt should be allowed");
        }
    }

    [Fact]
    public void Full_pipeline_allows_retry_on_transient_error_with_different_hash()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var stepIndex = 0;
        var output1 = "npm ERR! code ERESOLVE (first attempt)";
        var output2 = "npm ERR! code ERESOLVE (second attempt - different state)";

        // Act 1: First failure
        var failure1 = _classifier.Classify(1, output1, ModelType.Claude, ModelType.Claude);
        var canRetry1 = _guard.CanRetry(taskId, stepIndex, 0, failure1.ErrorHash, 3, 10);

        // Act 2: Second failure with different hash
        var failure2 = _classifier.Classify(1, output2, ModelType.Claude, ModelType.Claude);
        var canRetry2 = _guard.CanRetry(taskId, stepIndex, 1, failure2.ErrorHash, 3, 10);

        // Assert
        failure1.Type.Should().Be(FailureType.DependencyMissing);
        failure1.Retryable.Should().BeTrue();
        canRetry1.Should().BeTrue();

        failure2.ErrorHash.Should().NotBe(failure1.ErrorHash);
        canRetry2.Should().BeTrue("different error hash means try again");
    }

    [Fact]
    public void Full_pipeline_halts_on_same_error_repeated()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var stepIndex = 0;
        var output = "error: something went wrong";

        var failure = _classifier.Classify(1, output, ModelType.Claude, ModelType.Claude);

        // Act 1: First retry attempt
        var canRetry1 = _guard.CanRetry(taskId, stepIndex, 0, failure.ErrorHash, 3, 10);

        // Act 2: Second retry with same error
        var canRetry2 = _guard.CanRetry(taskId, stepIndex, 1, failure.ErrorHash, 3, 10);

        // Assert
        canRetry1.Should().BeTrue();
        canRetry2.Should().BeFalse("same error hash blocks retry");
    }

    [Fact]
    public void Full_pipeline_includes_model_identity_for_diagnostics()
    {
        // Arrange
        var plannerModel = ModelType.Claude;
        var executorModel = ModelType.Codex;
        var output = "Some error output";

        // Act
        var failure = _classifier.Classify(1, output, plannerModel, executorModel);

        // Assert
        failure.PlannerModel.Should().Be(plannerModel);
        failure.ExecutorModel.Should().Be(executorModel);
    }

    [Fact]
    public void Full_pipeline_tracks_multi_step_execution_with_loop_guard()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var step0Output = "error: npm not found";
        var step1Output = "error: docker not found";

        // Step 0: First error
        var failure0 = _classifier.Classify(127, step0Output, null, ModelType.Claude);
        var canRetry0_1 = _guard.CanRetry(taskId, 0, 0, failure0.ErrorHash, 3, 10);

        // Still on step 0, different error
        var failure0_alt = _classifier.Classify(127, "error: git not found", null, ModelType.Claude);
        var canRetry0_2 = _guard.CanRetry(taskId, 0, 1, failure0_alt.ErrorHash, 3, 10);

        // Move to step 1
        var failure1 = _classifier.Classify(127, step1Output, null, ModelType.Claude);
        var canRetry1_1 = _guard.CanRetry(taskId, 1, 0, failure1.ErrorHash, 3, 10);

        // Assert: All retries allowed within limits
        canRetry0_1.Should().BeTrue("First attempt");
        failure0.Retryable.Should().BeTrue("DependencyMissing is retryable");

        canRetry0_2.Should().BeTrue("Different error, retry allowed");
        failure0_alt.ErrorHash.Should().NotBe(failure0.ErrorHash);

        canRetry1_1.Should().BeTrue("New step, new error hash");
    }

    [Fact]
    public void Full_pipeline_prevents_infinite_loops()
    {
        // Arrange: Simulate maxLoopsPerTask = 2
        var taskId = Guid.NewGuid();
        var output = "error: transient error";
        var failure = _classifier.Classify(1, output, null, ModelType.Claude);

        // Act: Attempt to exceed loop limit with different errors each time
        var canRetry1 = _guard.CanRetry(taskId, 0, 0, "hash1", 3, maxLoopsPerTask: 2);  // Loop 1
        var canRetry2 = _guard.CanRetry(taskId, 0, 1, "hash2", 3, maxLoopsPerTask: 2);  // Loop 2
        var canRetry3 = _guard.CanRetry(taskId, 0, 2, "hash3", 3, maxLoopsPerTask: 2);  // Loop 3 - should fail

        // Assert
        canRetry1.Should().BeTrue("Within limit");
        canRetry2.Should().BeTrue("Within limit");
        canRetry3.Should().BeFalse("Exceeds maxLoopsPerTask");
    }
}
