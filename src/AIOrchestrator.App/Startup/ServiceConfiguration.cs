using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.DependencyInjection;
using AIOrchestrator.App.Health;
using AIOrchestrator.App.Hubs;
using AIOrchestrator.App.Logging;
using AIOrchestrator.App.Security;
using AIOrchestrator.App.Services;
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

        // Phase 10: Audit Logging
        services.AddSingleton<AuditLogger>();

        // Phase 10: SignalR for Real-Time Updates
        services.AddSignalR();
        services.AddSingleton<HubConnectionManager>();

        // Phase 10: Health Checks
        services.AddHealthChecks()
            .AddCheck<EngineHealthCheck>("engine");

        // API Controllers
        services.AddControllers();

        return services;
    }
}
