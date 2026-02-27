using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.Replanning;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.App.DependencyInjection;

/// <summary>
/// Service collection extension for Phase 7 re-planning services.
/// </summary>
public static class ReplanningServiceCollectionExtensions
{
    /// <summary>
    /// Register re-planning services.
    /// </summary>
    public static IServiceCollection AddReplanning(this IServiceCollection services)
    {
        services.AddSingleton<IReplanningOrchestrator>(sp =>
            new ReplanningOrchestrator(
                sp.GetRequiredService<ICliSessionManager>()));

        return services;
    }
}
