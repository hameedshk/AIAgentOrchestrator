using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.CliRunner.Abstractions;
using System.Text.Json;

namespace AIOrchestrator.App.Replanning;

/// <summary>
/// Orchestrates re-planning when Executor fails with non-retryable error.
/// Spec Section 4.5: Invokes Planner with failure context, validates revised plan, appends remaining steps.
/// </summary>
public sealed class ReplanningOrchestrator : IReplanningOrchestrator
{
    private const int DefaultMaxReplanAttempts = 3;
    private const int MaxReplanAttemptsLimit = 5;

    private readonly ICliSessionManager _cliSessionManager;

    public ReplanningOrchestrator(ICliSessionManager cliSessionManager)
    {
        _cliSessionManager = cliSessionManager;
    }

    public bool CanReplan(OrchestratorTask task, FailureContext failureContext)
    {
        // Only replan if:
        // 1. Task allows replanning
        // 2. Failure is non-retryable (Spec 4.5)
        // 3. Haven't exceeded max replan attempts
        return task.AllowReplan
            && !failureContext.Retryable
            && task.ReplanAttempts < DefaultMaxReplanAttempts;
    }

    public async Task<OrchestratorTask> ReplanAsync(
        OrchestratorTask task,
        ExecutionStep failedStep,
        FailureContext failureContext,
        CancellationToken ct = default)
    {
        // Get completed steps (all before the failed step)
        var completedSteps = task.Steps
            .Where(s => s.Index < failedStep.Index)
            .ToList()
            .AsReadOnly();

        // Build replan prompt with full context
        var replanPrompt = ReplanPromptBuilder.BuildReplanPrompt(
            task,
            completedSteps,
            failedStep,
            failureContext);

        // Invoke Planner CLI with replan prompt (same model as original planning)
        var revisedPlanJson = await _cliSessionManager.InvokeAsync(
            task.Planner,
            replanPrompt,
            ct);

        // Validate revised plan JSON
        PlanDto? revisedPlan = null;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            revisedPlan = JsonSerializer.Deserialize<PlanDto>(revisedPlanJson, options);
            if (revisedPlan?.Steps == null || revisedPlan.Steps.Count == 0)
                throw new InvalidOperationException("Revised plan has no steps");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Planner produced invalid JSON during re-planning: {ex.Message}", ex);
        }

        // Build revised steps marked as from replan
        var revisedSteps = new List<ExecutionStep>();

        foreach (var revisedStep in revisedPlan.Steps)
        {
            var newStep = new ExecutionStep
            {
                Index = revisedStep.Index,
                Type = Enum.Parse<StepType>(revisedStep.Type ?? "Shell"),
                Description = revisedStep.Description ?? string.Empty,
                Command = revisedStep.Command,
                Prompt = revisedStep.Prompt,
                IsFromReplan = true  // Mark this step as from revised plan
            };
            revisedSteps.Add(newStep);
        }

        // Append revised steps to task without state transition
        task.AppendRevisionPlanSteps(revisedSteps);

        // Update task replan counter
        task.ReplanAttempts++;
        task.CurrentStepIndex = failedStep.Index;  // Resume from the failed step index with new plan

        return task;
    }

    // DTO for plan JSON deserialization (matches Planner output contract)
    private class PlanDto
    {
        public string? PlanVersion { get; set; }
        public string? TaskId { get; set; }
        public List<StepDto>? Steps { get; set; }
    }

    private class StepDto
    {
        public int Index { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public string? Command { get; set; }
        public string? Prompt { get; set; }
    }
}
