namespace AIOrchestrator.CliRunner.Abstractions;

/// <summary>
/// Monitors system resources (CPU, memory, process counts) for dispatch decisions.
/// </summary>
public interface IResourceMonitor
{
    /// <summary>
    /// Get current system resource snapshot.
    /// </summary>
    Task<SystemResources> GetSystemResourcesAsync();
}
