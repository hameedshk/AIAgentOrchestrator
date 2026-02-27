using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.CliRunner.Configuration;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.ExecutorSession;

/// <summary>
/// Persistent executor session bound to a task and model pairing.
/// Maintains a single long-running CLI process for step execution.
/// </summary>
public sealed class ExecutorSession : IExecutorSession
{
    private readonly ICliSession _cliSession;
    private readonly ModelBinaryConfig _config;
    private bool _disposed;

    public Guid TaskId { get; }

    public ExecutorSession(Guid taskId, ICliSession cliSession, ModelBinaryConfig config)
    {
        TaskId = taskId;
        _cliSession = cliSession;
        _config = config;
    }

    public async Task<ExecutionStepResult> ExecuteStepAsync(ExecutionStep step, string? stepPrompt = null,
                                                             CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            string prompt;

            if (step.Type == StepType.Shell)
            {
                // Shell step: use the command directly
                prompt = step.Command ?? throw new InvalidOperationException("Shell step requires Command");
            }
            else if (step.Type == StepType.Agent)
            {
                // Agent step: use provided prompt or fall back to step prompt
                prompt = stepPrompt ?? step.Prompt ?? throw new InvalidOperationException("Agent step requires Prompt");
            }
            else
            {
                throw new InvalidOperationException($"Unknown step type: {step.Type}");
            }

            // Execute via CLI session
            var result = await _cliSession.ExecuteAsync(prompt, ct);

            var duration = DateTimeOffset.UtcNow - startTime;

            return new ExecutionStepResult(
                Output: result.Output,
                ExitCode: result.ExitCode,
                TimedOut: result.TimedOut,
                SentinelDetected: result.SentinelDetected,
                ExecutedAt: startTime,
                Duration: duration
            );
        }
        catch (Exception ex)
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            throw new InvalidOperationException(
                $"Failed to execute step {step.Index} ({step.Type}): {ex.Message}",
                ex);
        }
    }

    public async Task<bool> IsAliveAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        try
        {
            // Send a no-op health check
            var result = await _cliSession.ExecuteAsync("", ct);
            return !result.TimedOut && result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task CloseAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _cliSession.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            await _cliSession.DisposeAsync();
        }
        catch
        {
            // Ignore dispose errors
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ExecutorSession));
    }
}
