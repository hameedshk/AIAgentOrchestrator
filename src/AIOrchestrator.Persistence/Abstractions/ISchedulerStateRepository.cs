using AIOrchestrator.Persistence.Dto;

namespace AIOrchestrator.Persistence.Abstractions;

public interface ISchedulerStateRepository
{
    Task SaveAsync(SchedulerStateDto state);
    Task<SchedulerStateDto?> LoadAsync();
}
