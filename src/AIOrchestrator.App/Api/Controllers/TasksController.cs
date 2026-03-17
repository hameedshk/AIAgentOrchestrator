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
        if (!Enum.TryParse<TaskPriority>(request.Priority, ignoreCase: true, out var priority))
        {
            return BadRequest(new { error = "Invalid priority. Allowed values: Low, Normal, High" });
        }

        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ProjectId = request.ProjectId,
            Priority = priority,
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
        OrchestratorTask? task = null;

        foreach (var state in Enum.GetValues<TaskState>())
        {
            var tasks = await _engine.GetTasksByStateAsync(state);
            task = tasks.FirstOrDefault(t => t.Id == id);
            if (task != null)
                break;
        }

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
        TaskState taskState;
        if (string.IsNullOrEmpty(state))
        {
            taskState = TaskState.Queued;
        }
        else if (!Enum.TryParse<TaskState>(state, ignoreCase: true, out taskState))
        {
            return BadRequest(new { error = "Invalid task state." });
        }

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
            TaskId = task.Id,
            Title = task.Title,
            State = task.State.ToString(),
            ProjectId = task.ProjectId,
            Priority = task.Priority.ToString(),
            Planner = task.Planner.ToString(),
            Executor = task.Executor.ToString(),
            CurrentStepIndex = task.CurrentStepIndex,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}
