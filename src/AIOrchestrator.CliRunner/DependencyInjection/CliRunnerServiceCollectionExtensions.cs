using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.CliRunner.ResourceMonitoring;

namespace AIOrchestrator.CliRunner.DependencyInjection;

/// <summary>
/// Service collection extensions for CliRunner services.
/// </summary>
public static class CliRunnerServiceCollectionExtensions
{
    /// <summary>
    /// Add CliRunner services including ResourceMonitor.
    /// </summary>
    public static IServiceCollection AddCliRunner(this IServiceCollection services)
    {
        services.AddSingleton<IResourceMonitor>(sp =>
            new ResourceMonitor(maxProcesses: 10));
        return services;
    }
}
