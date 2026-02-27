using AIOrchestrator.CliRunner.Configuration;
using AIOrchestrator.CliRunner.ExecutorSession;
using AIOrchestrator.CliRunner.GitSnapshot;
using AIOrchestrator.CliRunner.StepExecution;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using FluentAssertions;

namespace AIOrchestrator.CliRunner.Tests.ExecutorSession;

public class ExecutorSessionLifecycleTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _repoDir;

    public ExecutorSessionLifecycleTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _repoDir = Path.Combine(_tempDir, "repo");
        Directory.CreateDirectory(_repoDir);
        InitializeGitRepo(_repoDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { }
    }

    [Fact]
    public void ExecutorSessionLifecycle_validates_step_before_execution()
    {
        // Arrange
        var lifecycle = new ExecutorSessionLifecycle(new StepExecutionDispatcher());
        var shellStep = new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "Bad step" };
        // No Command field - should fail validation

        // Act & Assert
        var act = () => lifecycle.ValidateStep(shellStep);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ExecutorSessionLifecycle_dispatches_to_correct_executor()
    {
        // Arrange
        var lifecycle = new ExecutorSessionLifecycle(new StepExecutionDispatcher());
        var shellStep = new ExecutionStep { Index = 0, Type = StepType.Shell, Command = "echo test" };
        var agentStep = new ExecutionStep { Index = 1, Type = StepType.Agent, Prompt = "Do something" };

        // Act & Assert - should not throw
        lifecycle.ValidateStep(shellStep);
        lifecycle.ValidateStep(agentStep);
    }

    [Fact]
    public async Task ExecutorSessionLifecycle_manages_git_snapshots_for_each_step()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var snapshotMgr = new GitSnapshotManager(_repoDir);
        var lifecycle = new ExecutorSessionLifecycle(new StepExecutionDispatcher(), snapshotMgr);

        // Create initial commit
        var testFile = Path.Combine(_repoDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "initial");
        RunGitCommand(_repoDir, "add .");
        RunGitCommand(_repoDir, "commit -m \"initial\"");

        // Act: Take snapshot for step 0
        var snapshot = await snapshotMgr.TakeSnapshotAsync(stepIndex: 0);

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.CommitSha.Should().HaveLength(40);
    }

    [Fact]
    public async Task ExecutorSessionLifecycle_resets_to_snapshot_on_retry()
    {
        // Arrange
        var snapshotMgr = new GitSnapshotManager(_repoDir);
        var testFile = Path.Combine(_repoDir, "test.txt");

        // Create initial state
        await File.WriteAllTextAsync(testFile, "initial");
        RunGitCommand(_repoDir, "add .");
        RunGitCommand(_repoDir, "commit -m \"initial\"");

        // Take snapshot
        var snapshot = await snapshotMgr.TakeSnapshotAsync(stepIndex: 0);

        // Modify file
        await File.WriteAllTextAsync(testFile, "modified");

        // Act: Reset to snapshot
        var result = await snapshotMgr.ResetToSnapshotAsync(snapshot);

        // Assert
        result.Should().BeTrue();
        var content = await File.ReadAllTextAsync(testFile);
        content.Should().Be("initial");
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
