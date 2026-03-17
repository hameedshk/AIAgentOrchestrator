using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AIOrchestrator.App.DependencyInjection;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Health;
using AIOrchestrator.App.Hubs;
using AIOrchestrator.App.Logging;
using AIOrchestrator.App.Security;
using AIOrchestrator.App.Services;
using AIOrchestrator.CliRunner.Configuration;
using AIOrchestrator.CliRunner.DependencyInjection;
using AIOrchestrator.Persistence.Abstractions;
using AIOrchestrator.Persistence.FileSystem;

namespace AIOrchestrator.App.Startup;

/// <summary>
/// Configures all services for the AIOrchestrator application.
/// </summary>
public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureAIOrchestratorServices(
        this IServiceCollection services,
        string schedulerStateDir,
        IConfiguration configuration)
    {
        var storageBasePath = configuration["StorageBasePath"];
        if (string.IsNullOrWhiteSpace(storageBasePath))
        {
            storageBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIOrchestrator",
                "data");
        }

        var cliRunnerOptions = BuildCliRunnerOptions(configuration);

        // Phase 3: CLI Runner
        services.AddCliRunner(cliRunnerOptions);
        services.AddFailureClassification();

        // Persistence
        services.AddSingleton(new TaskStorePaths(storageBasePath));
        services.AddSingleton<ITaskRepository, FileTaskRepository>();

        // Phase 8: Scheduler
        services.AddScheduler(schedulerStateDir);

        // Phase 9: Engine
        services.AddEngine();

        // Phase 10: Security
        var tokenStore = new InMemoryTokenStore();
        var pairingTokenExpiration = configuration.GetValue<int?>("Security:PairingTokenExpirationMinutes") ?? 15;

        services.AddSingleton<ITokenHasher, TokenHasher>();
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton(sp =>
            new DevicePairingService(
                tokenExpirationMinutes: pairingTokenExpiration,
                hasher: sp.GetRequiredService<ITokenHasher>(),
                clock: sp.GetRequiredService<ISystemClock>(),
                logger: sp.GetService<ILogger<DevicePairingService>>()));

        // Initialize with demo token for testing/development
        // In production, tokens would be generated through device pairing flow
        tokenStore.StoreToken("demo-token-12345678901234567890", "DemoDevice");

        services.AddSingleton<ITokenStore>(tokenStore);
        services.AddSingleton<EngineModeStore>();

        // Crash recovery and replanning
        var recoveryDirectory = Path.Combine(storageBasePath, "recovery");
        services.AddCrashRecovery(recoveryDirectory);
        services.AddReplanning();

        // Phase 10: Audit Logging
        services.AddSingleton<AuditLogger>();

        // Phase 10: SignalR for Real-Time Updates
        services.AddSignalR();
        services.AddSingleton<HubConnectionManager>();

        // Phase 10: Health Checks
        services.AddHealthChecks()
            .AddCheck<EngineHealthCheck>("engine");

        // API and Dashboard
        services.AddControllersWithViews();

        return services;
    }

    private static CliRunnerOptions BuildCliRunnerOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(CliRunnerOptions.SectionName);
        var defaultSilenceTimeout = section.GetValue<int?>("DefaultSilenceTimeoutSeconds") ?? 120;

        var models = section.GetSection("Models")
            .GetChildren()
            .Select(modelSection =>
            {
                var modelName = modelSection["ModelName"];
                var binaryPath = modelSection["BinaryPath"];

                if (string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(binaryPath))
                    return null;

                var defaultArgs = modelSection.GetSection("DefaultArgs")
                    .GetChildren()
                    .Select(child => child.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Cast<string>()
                    .ToArray();

                return new ModelBinaryConfig
                {
                    ModelName = modelName,
                    BinaryPath = binaryPath,
                    DefaultArgs = defaultArgs,
                    SilenceTimeoutSeconds = modelSection.GetValue<int?>("SilenceTimeoutSeconds") ?? defaultSilenceTimeout
                };
            })
            .Where(model => model != null)
            .Cast<ModelBinaryConfig>()
            .ToList();

        if (models.Count == 0)
        {
            models =
            [
                new ModelBinaryConfig
                {
                    ModelName = "claude",
                    BinaryPath = "claude",
                    DefaultArgs = [],
                    SilenceTimeoutSeconds = defaultSilenceTimeout
                },
                new ModelBinaryConfig
                {
                    ModelName = "codex",
                    BinaryPath = "codex",
                    DefaultArgs = [],
                    SilenceTimeoutSeconds = defaultSilenceTimeout
                }
            ];
        }

        return new CliRunnerOptions
        {
            Models = models,
            DefaultSilenceTimeoutSeconds = defaultSilenceTimeout
        };
    }
}
