using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Persistence.Abstractions;
using AIOrchestrator.Persistence.Dto;
using AIOrchestrator.Persistence.Json;

namespace AIOrchestrator.Persistence.FileSystem;

public sealed class FileTaskRepository(TaskStorePaths paths) : ITaskRepository
{
    private readonly TaskJsonSerializer _serializer = new();

    public async Task SaveAsync(OrchestratorTask task, CancellationToken ct = default)
    {
        Directory.CreateDirectory(paths.GetTaskDirectory());
        var dto = MapToDto(task);
        var json = _serializer.Serialize(dto);
        var filePath = paths.GetTaskFilePath(task.Id);
        await AtomicFileWriter.WriteAllTextAsync(filePath, json, ct);
    }

    public async Task<OrchestratorTask?> LoadAsync(Guid taskId, CancellationToken ct = default)
    {
        var filePath = paths.GetTaskFilePath(taskId);
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath, ct);
        var dto = _serializer.Deserialize(json);
        return dto == null ? null : MapToDomain(dto);
    }

    public async Task<IReadOnlyList<OrchestratorTask>> LoadAllAsync(CancellationToken ct = default)
    {
        var taskDir = paths.GetTaskDirectory();
        if (!Directory.Exists(taskDir))
            return [];

        var files = Directory.GetFiles(taskDir, "*.json");
        var tasks = new List<OrchestratorTask>();

        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file, ct);
            var dto = _serializer.Deserialize(json);
            if (dto != null)
                tasks.Add(MapToDomain(dto));
        }

        return tasks.AsReadOnly();
    }

    public async Task<bool> ExistsAsync(Guid taskId, CancellationToken ct = default)
    {
        var filePath = paths.GetTaskFilePath(taskId);
        return await Task.FromResult(File.Exists(filePath));
    }

    public async Task DeleteAsync(Guid taskId, CancellationToken ct = default)
    {
        var filePath = paths.GetTaskFilePath(taskId);
        if (File.Exists(filePath))
            await Task.Run(() => File.Delete(filePath), ct);
    }

    private static OrchestratorTaskDto MapToDto(OrchestratorTask task)
    {
        return new OrchestratorTaskDto
        {
            Id = task.Id,
            Title = task.Title,
            State = task.State.ToString(),
            Planner = task.Planner.ToString(),
            Executor = task.Executor.ToString(),
            Steps = task.Steps.Select(MapStepToDto).ToList(),
            RetryCount = task.RetryCount,
            LastFailure = task.LastFailure == null ? null : MapFailureToDto(task.LastFailure),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    private static OrchestratorTask MapToDomain(OrchestratorTaskDto dto)
    {
        var task = new OrchestratorTask
        {
            Id = dto.Id,
            Title = dto.Title,
            Planner = Enum.Parse<ModelType>(dto.Planner),
            Executor = Enum.Parse<ModelType>(dto.Executor),
            CreatedAt = dto.CreatedAt
        };

        // Set the state via reflection since State is private
        var stateField = typeof(OrchestratorTask).GetProperty(nameof(OrchestratorTask.State));
        if (stateField != null)
        {
            var state = Enum.Parse<TaskState>(dto.State);
            stateField.GetSetMethod(nonPublic: true)?.Invoke(task, [state]);
        }

        // Set UpdatedAt via reflection
        var updatedAtField = typeof(OrchestratorTask).GetProperty(nameof(OrchestratorTask.UpdatedAt));
        if (updatedAtField != null)
            updatedAtField.GetSetMethod(nonPublic: true)?.Invoke(task, [dto.UpdatedAt]);

        return task;
    }

    private static ExecutionStepDto MapStepToDto(ExecutionStep step)
    {
        return new ExecutionStepDto
        {
            Id = step.Id,
            Index = step.Index,
            Type = step.Type.ToString(),
            Description = step.Description,
            Command = step.Command,
            Prompt = step.Prompt,
            ExpectedOutput = step.ExpectedOutput,
            Status = step.Status.ToString(),
            ActualOutput = step.ActualOutput,
            LastFailure = step.LastFailure == null ? null : MapFailureToDto(step.LastFailure),
            StartedAt = step.StartedAt,
            CompletedAt = step.CompletedAt
        };
    }

    private static ExecutionStep MapStepToDomain(ExecutionStepDto dto)
    {
        var step = new ExecutionStep
        {
            Id = dto.Id,
            Index = dto.Index,
            Type = Enum.Parse<StepType>(dto.Type),
            Description = dto.Description,
            Command = dto.Command,
            Prompt = dto.Prompt,
            ExpectedOutput = dto.ExpectedOutput
        };

        // Set Status via reflection
        var statusField = typeof(ExecutionStep).GetProperty(nameof(ExecutionStep.Status));
        if (statusField != null)
        {
            var status = Enum.Parse<StepStatus>(dto.Status);
            statusField.GetSetMethod(nonPublic: true)?.Invoke(step, [status]);
        }

        // Set ActualOutput via reflection
        var outputField = typeof(ExecutionStep).GetProperty(nameof(ExecutionStep.ActualOutput));
        if (outputField != null)
            outputField.GetSetMethod(nonPublic: true)?.Invoke(step, [dto.ActualOutput]);

        // Set LastFailure via reflection
        if (dto.LastFailure != null)
        {
            var failureField = typeof(ExecutionStep).GetProperty(nameof(ExecutionStep.LastFailure));
            if (failureField != null)
            {
                var failure = MapFailureToDomain(dto.LastFailure);
                failureField.GetSetMethod(nonPublic: true)?.Invoke(step, [failure]);
            }
        }

        // Set StartedAt via reflection
        var startedField = typeof(ExecutionStep).GetProperty(nameof(ExecutionStep.StartedAt));
        if (startedField != null)
            startedField.GetSetMethod(nonPublic: true)?.Invoke(step, [dto.StartedAt]);

        // Set CompletedAt via reflection
        var completedField = typeof(ExecutionStep).GetProperty(nameof(ExecutionStep.CompletedAt));
        if (completedField != null)
            completedField.GetSetMethod(nonPublic: true)?.Invoke(step, [dto.CompletedAt]);

        return step;
    }

    private static FailureContextDto MapFailureToDto(FailureContext failure)
    {
        return new FailureContextDto
        {
            Type = failure.Type.ToString(),
            RawOutput = failure.RawOutput,
            ExitCode = failure.ExitCode,
            ErrorHash = failure.ErrorHash,
            Retryable = failure.Retryable,
            PlannerModel = failure.PlannerModel?.ToString(),
            ExecutorModel = failure.ExecutorModel?.ToString(),
            OccurredAt = failure.OccurredAt
        };
    }

    private static FailureContext MapFailureToDomain(FailureContextDto dto)
    {
        return new FailureContext(
            Type: Enum.Parse<FailureType>(dto.Type ?? "Unknown"),
            RawOutput: dto.RawOutput ?? "",
            ExitCode: dto.ExitCode,
            ErrorHash: dto.ErrorHash ?? "",
            Retryable: dto.Retryable,
            PlannerModel: string.IsNullOrEmpty(dto.PlannerModel) ? null : Enum.Parse<ModelType>(dto.PlannerModel),
            ExecutorModel: string.IsNullOrEmpty(dto.ExecutorModel) ? null : Enum.Parse<ModelType>(dto.ExecutorModel),
            OccurredAt: dto.OccurredAt
        );
    }
}
