using AIOrchestrator.CliRunner.GitSnapshot;
using FluentAssertions;

namespace AIOrchestrator.CliRunner.Tests.GitSnapshot;

public class GitSnapshotManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoDir;
    private readonly GitSnapshotManager _manager;

    public GitSnapshotManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _repoDir = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(_repoDir);

        // Initialize a git repo
        InitializeGitRepo(_repoDir);

        _manager = new GitSnapshotManager(_repoDir);
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
    public async Task TakeSnapshot_captures_current_commit_hash()
    {
        // Arrange
        var testFile = Path.Combine(_repoDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "content");
        RunGitCommand(_repoDir, "add .");
        RunGitCommand(_repoDir, "commit -m \"test commit\"");

        // Act
        var snapshot = await _manager.TakeSnapshotAsync(stepIndex: 0);

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.CommitSha.Should().NotBeNullOrEmpty();
        snapshot.CommitSha.Should().HaveLength(40); // Full SHA
        snapshot.TakenAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ResetToSnapshot_restores_working_directory_to_snapshot_state()
    {
        // Arrange: Create initial file and snapshot
        var testFile = Path.Combine(_repoDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "initial");
        RunGitCommand(_repoDir, "add .");
        RunGitCommand(_repoDir, "commit -m \"initial\"");

        var snapshot = await _manager.TakeSnapshotAsync(stepIndex: 0);

        // Act: Modify file
        await File.WriteAllTextAsync(testFile, "modified");

        // Act: Reset to snapshot
        var result = await _manager.ResetToSnapshotAsync(snapshot);

        // Assert: File is restored
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("initial");
    }

    [Fact]
    public async Task ResetToSnapshot_fails_if_snapshot_commit_not_found()
    {
        // Arrange
        var invalidSnapshot = new GitSnapshotMetadata(
            CommitSha: "0000000000000000000000000000000000000000",
            TakenAt: DateTimeOffset.UtcNow
        );

        // Act
        var result = await _manager.ResetToSnapshotAsync(invalidSnapshot);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TakeSnapshot_metadata_saved_and_retrievable()
    {
        // Arrange
        var testFile = Path.Combine(_repoDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "content");
        RunGitCommand(_repoDir, "add .");
        RunGitCommand(_repoDir, "commit -m \"test\"");

        // Act
        var snapshot1 = await _manager.TakeSnapshotAsync(stepIndex: 0);
        var snapshot2 = await _manager.GetSnapshotAsync(stepIndex: 0);

        // Assert
        snapshot2.Should().NotBeNull();
        snapshot2!.CommitSha.Should().Be(snapshot1.CommitSha);
    }

    private static void InitializeGitRepo(string repoDir)
    {
        RunGitCommand(repoDir, "init");
        RunGitCommand(repoDir, "config user.name \"Test\"");
        RunGitCommand(repoDir, "config user.email \"test@test.com\"");
    }

    private static void RunGitCommand(string repoDir, string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = repoDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(psi);
        process?.WaitForExit();
    }
}
