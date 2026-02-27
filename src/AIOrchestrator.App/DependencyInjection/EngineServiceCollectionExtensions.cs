using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.App.DependencyInjection;

/// <summary>
/// Service collection extension for Phase 9 Engine services.
/// </summary>
public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Register Engine services (requires Scheduler and ResourceMonitor already registered).
    /// </summary>
    public static IServiceCollection AddEngine(this IServiceCollection services)
    {
        services.AddSingleton<IEngine>(sp =>
            new Engine.Engine(
                sp.GetRequiredService<IScheduler>(),
                sp.GetRequiredService<IResourceMonitor>()));

        return services;
    }
}
