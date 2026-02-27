using FluentAssertions;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.CliRunner.FailureClassification;

namespace AIOrchestrator.CliRunner.Tests.FailureClassification;

public class FailureClassifierTests
{
    private readonly IFailureClassifier _classifier = new FailureClassifier();

    [Theory]
    [InlineData(1, "error CS0103: The name 'x' does not exist")]
    [InlineData(1, "Parse error: Unknown token")]
    public void Classify_detects_CompileError_from_output(int exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.CompileError);
        failure.Retryable.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "FAILED - test_login (AssertionError)")]
    [InlineData(1, "Tests failed: 3 failures")]
    public void Classify_detects_TestFailure_from_output(int exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.TestFailure);
        failure.Retryable.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "npm ERR! code ERESOLVE")]
    [InlineData(127, "command not found: docker")]
    public void Classify_detects_DependencyMissing_from_output(int exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.DependencyMissing);
        failure.Retryable.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, "Exception: System.NullReferenceException")]
    [InlineData(1, "Unhandled exception of type")]
    public void Classify_detects_RuntimeException_from_output(int exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.RuntimeException);
        failure.Retryable.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "CONFLICT (content): Merge conflict")]
    [InlineData(1, "error: Your local changes to")]
    public void Classify_detects_GitConflict_from_output(int exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.GitConflict);
        failure.Retryable.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "Permission denied: /etc/passwd")]
    [InlineData(1, "Access is denied")]
    public void Classify_detects_PermissionError_from_output(int exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.PermissionError);
        failure.Retryable.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "No output for 30 seconds")]
    [InlineData(124, "Step exceeded timeout")]
    public void Classify_detects_Timeout_from_output(int? exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.Timeout);
        failure.Retryable.Should().BeTrue("Timeout is transient and retryable");
    }

    [Theory]
    [InlineData(1, "Expected JSON structure but got plain text")]
    [InlineData(1, "Missing required field: 'steps' in plan output")]
    public void Classify_detects_AgentInvalidOutput_from_output(int exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.AgentInvalidOutput);
        failure.Retryable.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "DangerousCommandBlocked: rm -rf / not allowed")]
    [InlineData(1, "Blocklist violation: sudo detected")]
    public void Classify_detects_DangerousCommandBlocked_from_output(int exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.DangerousCommandBlocked);
        failure.Retryable.Should().BeFalse();
    }

    [Theory]
    [InlineData(-1, "CLI process crashed with signal 11")]
    [InlineData(null, "Unexpected EOF from CLI process")]
    public void Classify_detects_CliCrash_from_output(int? exitCode, string output)
    {
        // Act
        var failure = _classifier.Classify(exitCode, output, null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.CliCrash);
        failure.Retryable.Should().BeTrue("CLI crash is transient and retryable");
    }

    [Fact]
    public void Classify_includes_model_identity_for_diagnostics()
    {
        // Arrange
        var plannerModel = ModelType.Claude;
        var executorModel = ModelType.Codex;

        // Act
        var failure = _classifier.Classify(1, "any error", plannerModel, executorModel);

        // Assert
        failure.PlannerModel.Should().Be(plannerModel);
        failure.ExecutorModel.Should().Be(executorModel);
    }

    [Fact]
    public void Classify_returns_Unknown_for_unrecognized_output()
    {
        // Act
        var failure = _classifier.Classify(0, "Some random output that doesn't match patterns", null, ModelType.Claude);

        // Assert
        failure.Type.Should().Be(FailureType.Unknown);
        failure.Retryable.Should().BeFalse("Unknown failures are non-retryable by default");
    }

    [Fact]
    public void Classify_computes_error_hash()
    {
        // Act
        var failure = _classifier.Classify(1, "error: test", null, ModelType.Claude);

        // Assert
        failure.ErrorHash.Should().HaveLength(64);
        failure.ErrorHash.Should().MatchRegex("^[a-f0-9]{64}$");
    }
}
