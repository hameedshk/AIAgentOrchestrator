using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.DependencyInjection;
using AIOrchestrator.App.Security;
using AIOrchestrator.CliRunner.DependencyInjection;

namespace AIOrchestrator.App.Startup;

/// <summary>
/// Configures all services for the AIOrchestrator application.
/// </summary>
public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureAIOrchestratorServices(
        this IServiceCollection services,
        string schedulerStateDir)
    {
        // Phase 3: CLI Runner
        services.AddCliRunner();

        // Phase 8: Scheduler
        services.AddScheduler(schedulerStateDir);

        // Phase 9: Engine
        services.AddEngine();

        // Phase 10: Security
        services.AddSingleton<ITokenStore, InMemoryTokenStore>();

        // API Controllers
        services.AddControllers();

        return services;
    }
}
