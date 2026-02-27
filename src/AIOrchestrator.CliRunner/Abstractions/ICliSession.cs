using AIOrchestrator.CliRunner.Sessions;

namespace AIOrchestrator.CliRunner.Abstractions;

public interface ICliSession : IAsyncDisposable
{
    Task<SessionResult> ExecuteAsync(string prompt, CancellationToken ct = default);
}
