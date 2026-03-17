using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.Sessions;

/// <summary>
/// High-level adapter that invokes planner/executor models through CLI sessions.
/// </summary>
public sealed class CliSessionManager(ICliSessionFactory sessionFactory) : ICliSessionManager
{
    public async Task<string> InvokeAsync(
        ModelType model,
        string prompt,
        CancellationToken ct = default)
    {
        var modelName = model switch
        {
            ModelType.Claude => "claude",
            ModelType.Codex => "codex",
            _ => model.ToString().ToLowerInvariant()
        };

        await using var session = sessionFactory.Create(modelName);
        var result = await session.ExecuteAsync(prompt, ct);
        return result.Output;
    }
}
