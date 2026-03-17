using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.CliRunner.Configuration;
using AIOrchestrator.CliRunner.ResourceMonitoring;
using AIOrchestrator.CliRunner.Sessions;

namespace AIOrchestrator.CliRunner.DependencyInjection;

/// <summary>
/// Service collection extensions for CliRunner services.
/// </summary>
public static class CliRunnerServiceCollectionExtensions
{
    /// <summary>
    /// Add CliRunner services including ResourceMonitor.
    /// </summary>
    public static IServiceCollection AddCliRunner(this IServiceCollection services, CliRunnerOptions? options = null)
    {
        options ??= new CliRunnerOptions
        {
            DefaultSilenceTimeoutSeconds = 120,
            Models =
            [
                new ModelBinaryConfig
                {
                    ModelName = "claude",
                    BinaryPath = "claude",
                    DefaultArgs = [],
                    SilenceTimeoutSeconds = 120
                },
                new ModelBinaryConfig
                {
                    ModelName = "codex",
                    BinaryPath = "codex",
                    DefaultArgs = [],
                    SilenceTimeoutSeconds = 180
                }
            ]
        };

        services.AddSingleton(options);
        services.AddSingleton<ICliSessionFactory, CliSessionFactory>();
        services.AddSingleton<ICliSessionManager, CliSessionManager>();
        services.AddSingleton<IResourceMonitor>(sp =>
            new ResourceMonitor(maxProcesses: 10));
        return services;
    }
}
