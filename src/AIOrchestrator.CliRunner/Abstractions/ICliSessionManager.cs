using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.CliRunner.Abstractions;

/// <summary>
/// Manages CLI sessions for invoking models (Planner/Executor).
/// Provides high-level interface for invoking models and getting responses.
/// </summary>
public interface ICliSessionManager
{
    /// <summary>
    /// Invoke a model with a prompt and get the response.
    /// </summary>
    /// <param name="model">The model to invoke</param>
    /// <param name="prompt">The prompt to send to the model</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The response from the model</returns>
    Task<string> InvokeAsync(
        ModelType model,
        string prompt,
        CancellationToken ct = default);
}
