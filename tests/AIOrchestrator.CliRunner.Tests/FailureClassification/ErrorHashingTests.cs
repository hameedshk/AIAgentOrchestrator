using FluentAssertions;
using AIOrchestrator.CliRunner.FailureClassification;

namespace AIOrchestrator.CliRunner.Tests.FailureClassification;

public class ErrorHashingTests
{
    private IFailureClassifier _classifier = null!;

    public ErrorHashingTests()
    {
        _classifier = new FailureClassifier();
    }

    [Fact]
    public void ComputeErrorHash_returns_consistent_hash_for_same_output()
    {
        // Arrange
        string output = "error: cannot find module 'lodash'\n";

        // Act
        var hash1 = _classifier.ComputeErrorHash(output);
        var hash2 = _classifier.ComputeErrorHash(output);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64); // SHA256 hex = 64 chars
    }

    [Fact]
    public void ComputeErrorHash_normalizes_whitespace_variations()
    {
        // Arrange
        string output1 = "error: file not found\n";
        string output2 = "error:  file  not  found\n\n";

        // Act
        var hash1 = _classifier.ComputeErrorHash(output1);
        var hash2 = _classifier.ComputeErrorHash(output2);

        // Assert
        hash1.Should().Be(hash2, "whitespace variations should produce identical hashes");
    }

    [Fact]
    public void ComputeErrorHash_produces_different_hash_for_different_errors()
    {
        // Arrange
        string error1 = "error: cannot find module 'lodash'";
        string error2 = "error: cannot find module 'express'";

        // Act
        var hash1 = _classifier.ComputeErrorHash(error1);
        var hash2 = _classifier.ComputeErrorHash(error2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeErrorHash_ignores_timestamps_and_paths()
    {
        // Arrange
        string output1 = "error at /home/user/file.cs:42 at 2026-02-27T10:00:00Z";
        string output2 = "error at /home/user/file.cs:42 at 2026-02-27T10:00:05Z";

        // Act
        var hash1 = _classifier.ComputeErrorHash(output1);
        var hash2 = _classifier.ComputeErrorHash(output2);

        // Assert
        hash1.Should().Be(hash2, "timestamps should be normalized away");
    }
}
