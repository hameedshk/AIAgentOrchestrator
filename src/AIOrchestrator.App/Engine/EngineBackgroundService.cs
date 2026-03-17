using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIOrchestrator.App.Engine;

/// <summary>
/// Runs the engine dispatch loop for the lifetime of the web process.
/// </summary>
public sealed class EngineBackgroundService(
    IEngine engine,
    ILogger<EngineBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Engine background service started.");
        await engine.RunAsync(stoppingToken);
        logger.LogInformation("Engine background service stopped.");
    }
}
