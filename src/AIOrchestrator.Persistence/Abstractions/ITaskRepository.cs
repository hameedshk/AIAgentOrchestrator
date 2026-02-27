using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.Persistence.Abstractions;

public interface ITaskRepository
{
    Task SaveAsync(OrchestratorTask task, CancellationToken ct = default);
    Task<OrchestratorTask?> LoadAsync(Guid taskId, CancellationToken ct = default);
    Task<IReadOnlyList<OrchestratorTask>> LoadAllAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid taskId, CancellationToken ct = default);
    Task DeleteAsync(Guid taskId, CancellationToken ct = default);
}
