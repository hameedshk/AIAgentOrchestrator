using System.Diagnostics;

namespace AIOrchestrator.CliRunner.GitSnapshot;

/// <summary>
/// Manages git snapshots for idempotent retry strategy.
/// Takes snapshots before step execution and resets to them on retry.
/// </summary>
public sealed class GitSnapshotManager(string repositoryPath) : IGitSnapshotManager
{
    private readonly Dictionary<int, GitSnapshotMetadata> _snapshots = new();

    public async Task<GitSnapshotMetadata> TakeSnapshotAsync(int stepIndex, CancellationToken ct = default)
    {
        // Get current commit SHA
        var sha = await RunGitCommandAsync("rev-parse HEAD", ct);

        if (string.IsNullOrEmpty(sha))
            throw new InvalidOperationException("Failed to get git commit SHA");

        var metadata = new GitSnapshotMetadata(
            CommitSha: sha.Trim(),
            TakenAt: DateTimeOffset.UtcNow
        );

        // Cache it
        _snapshots[stepIndex] = metadata;

        return metadata;
    }

    public async Task<bool> ResetToSnapshotAsync(GitSnapshotMetadata snapshot, CancellationToken ct = default)
    {
        try
        {
            // Verify the commit exists
            var output = await RunGitCommandAsync($"cat-file -t {snapshot.CommitSha}", ct);
            if (string.IsNullOrEmpty(output))
                return false;

            // Reset to snapshot
            await RunGitCommandAsync($"reset --hard {snapshot.CommitSha}", ct);

            // Verify clean state
            var status = await RunGitCommandAsync("status --porcelain", ct);
            return string.IsNullOrWhiteSpace(status);
        }
        catch
        {
            return false;
        }
    }

    public async Task<GitSnapshotMetadata?> GetSnapshotAsync(int stepIndex, CancellationToken ct = default)
    {
        return _snapshots.TryGetValue(stepIndex, out var metadata) ? metadata : null;
    }

    private async Task<string> RunGitCommandAsync(string args, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start git process");

        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}
