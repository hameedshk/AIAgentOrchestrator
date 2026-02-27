using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.CliRunner.Configuration;
using AIOrchestrator.CliRunner.Sessions;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.ExecutorSession;

/// <summary>
/// Creates persistent executor sessions bound to tasks and models.
/// One session per task, with one CLI process per session.
/// </summary>
public sealed class ExecutorSessionFactory(CliRunnerOptions options)
{
    /// <summary>
    /// Create a new executor session for the given task and model.
    /// The session maintains a persistent CLI process for step execution.
    /// </summary>
    public IExecutorSession Create(Guid taskId, ModelType model)
    {
        // Resolve model name from enum
        var modelName = model.ToString().ToLowerInvariant();

        // Get binary path from options
        var binaryPath = options.GetBinaryPath(modelName);

        // Find the model config
        var config = options.Models.FirstOrDefault(m =>
            m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Model '{modelName}' not configured");

        // Create the underlying CLI session
        var cliSession = new CliSession(config);

        // Wrap in executor session
        return new ExecutorSession(taskId, cliSession, config);
    }
}
