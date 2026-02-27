using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.CrashRecovery;
using AIOrchestrator.Persistence.Abstractions;

namespace AIOrchestrator.App.DependencyInjection;

/// <summary>
/// Service collection extension for Phase 6 crash recovery services.
/// </summary>
public static class CrashRecoveryServiceCollectionExtensions
{
    /// <summary>
    /// Register crash recovery services.
    /// </summary>
    public static IServiceCollection AddCrashRecovery(this IServiceCollection services, string dataDirectory)
    {
        services.AddSingleton<ICrashLoopDetector>(new CrashLoopDetector(dataDirectory));
        services.AddSingleton<IRecoveryEventLogger>(new RecoveryEventLogger(dataDirectory));
        services.AddSingleton<ITaskRecoveryCoordinator, TaskRecoveryCoordinator>();
        services.AddSingleton<ICrashRecoveryManager>(sp =>
            new CrashRecoveryManager(
                sp.GetRequiredService<ITaskRepository>(),
                sp.GetRequiredService<ICrashLoopDetector>(),
                sp.GetRequiredService<ITaskRecoveryCoordinator>(),
                sp.GetRequiredService<IRecoveryEventLogger>()));

        return services;
    }
}
