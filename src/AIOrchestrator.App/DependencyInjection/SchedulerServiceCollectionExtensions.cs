using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.Persistence.Abstractions;
using AIOrchestrator.Persistence.FileSystem;

namespace AIOrchestrator.App.DependencyInjection;

/// <summary>
/// Service collection extension for Phase 8 scheduler services.
/// </summary>
public static class SchedulerServiceCollectionExtensions
{
    /// <summary>
    /// Register scheduler services with file system state persistence.
    /// </summary>
    public static IServiceCollection AddScheduler(
        this IServiceCollection services,
        string schedulerStateDir)
    {
        services.AddSingleton<ISchedulerStateRepository>(
            new FileSystemSchedulerStateRepository(schedulerStateDir));

        services.AddSingleton<IScheduler>(sp =>
        {
            var repository = sp.GetRequiredService<ISchedulerStateRepository>();
            var scheduler = new PersistentScheduler(repository);
            scheduler.LoadAsync().GetAwaiter().GetResult();
            return scheduler;
        });

        return services;
    }
}
