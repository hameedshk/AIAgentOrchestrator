using Microsoft.AspNetCore.Mvc;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Api.RequestModels;
using AIOrchestrator.App.Api.ResponseModels;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Api.Controllers;

/// <summary>
/// API endpoints for task management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly IEngine _engine;

    public TasksController(IEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Submit a new task for execution.
    /// POST /api/tasks
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> SubmitTask([FromBody] SubmitTaskRequest request)
    {
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ProjectId = request.ProjectId,
            Priority = Enum.Parse<TaskPriority>(request.Priority),
            AllowReplan = request.AllowReplan
        };

        var submitted = await _engine.SubmitTaskAsync(task);

        var response = MapToResponse(submitted);
        return CreatedAtAction(nameof(GetTask), new { id = response.Id }, response);
    }

    /// <summary>
    /// Get a specific task by ID.
    /// GET /api/tasks/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid id)
    {
        var allTasks = await _engine.GetTasksByStateAsync(TaskState.Queued);
        var task = allTasks.FirstOrDefault(t => t.Id == id);

        if (task == null)
            return NotFound();

        return Ok(MapToResponse(task));
    }

    /// <summary>
    /// Get all tasks with specified state.
    /// GET /api/tasks?state=Queued
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> GetTasks([FromQuery] string? state = null)
    {
        TaskState taskState = string.IsNullOrEmpty(state)
            ? TaskState.Queued
            : Enum.Parse<TaskState>(state);

        var tasks = await _engine.GetTasksByStateAsync(taskState);
        var responses = tasks.Select(MapToResponse).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Get engine status and system resources.
    /// GET /api/tasks/status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<EngineStatusResponse>> GetStatus()
    {
        var status = await _engine.GetStatusAsync();

        var response = new EngineStatusResponse
        {
            TotalTasks = status.TotalTasks,
            QueuedTasks = status.QueuedTasks,
            ExecutingTasks = status.ExecutingTasks,
            CompletedTasks = status.CompletedTasks,
            FailedTasks = status.FailedTasks,
            CpuUsagePercent = status.CpuUsagePercent,
            AvailableMemoryMb = status.AvailableMemoryMb,
            RunningProcessCount = status.RunningProcessCount,
            LastDispatchTime = status.LastDispatchTime
        };

        return Ok(response);
    }

    private TaskResponse MapToResponse(OrchestratorTask task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            State = task.State.ToString(),
            ProjectId = task.ProjectId,
            Priority = task.Priority.ToString(),
            CurrentStepIndex = task.CurrentStepIndex,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
