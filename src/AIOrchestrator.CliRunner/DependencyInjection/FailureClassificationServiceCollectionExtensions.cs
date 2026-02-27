using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.CliRunner.FailureClassification;

namespace AIOrchestrator.CliRunner.DependencyInjection;

/// <summary>
/// Service collection extension for Phase 5 failure classification services.
/// </summary>
public static class FailureClassificationServiceCollectionExtensions
{
    /// <summary>
    /// Register FailureClassification services (FailureClassifier, LoopGuard).
    /// </summary>
    public static IServiceCollection AddFailureClassification(this IServiceCollection services)
    {
        services.AddSingleton<IFailureClassifier, FailureClassifier>();
        services.AddSingleton<ILoopGuard, LoopGuard>();
        return services;
    }
}
