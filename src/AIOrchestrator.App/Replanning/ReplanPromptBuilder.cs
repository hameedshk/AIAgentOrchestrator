using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.App.Replanning;

/// <summary>
/// Constructs structured replan prompts to send to the Planner.
/// Includes: original task, completed steps summary, failed step, failure context.
/// </summary>
public static class ReplanPromptBuilder
{
    public static string BuildReplanPrompt(
        OrchestratorTask task,
        IReadOnlyList<ExecutionStep> completedSteps,
        ExecutionStep failedStep,
        FailureContext failureContext)
    {
        var prompt = $@"## Task Re-Planning Request

**Original Task:** {task.Title}
**Task ID:** {task.Id}
**Original Objective:** {task.Description ?? "No description"}

---

## Execution Progress

**Steps Completed Successfully:** {completedSteps.Count}
";

        foreach (var step in completedSteps)
        {
            prompt += $"\n- Step {step.Index} ({step.Type}): {step.Description} — COMPLETED";
        }

        prompt += $@"

---

## Failure Context

**Failed Step:** Step {failedStep.Index} ({failedStep.Type})
**Description:** {failedStep.Description}
**Failure Type:** {failureContext.Type}
**Error Message:** {failureContext.RawOutput}

---

## Your Task

The above step failed with a non-retryable error. Your original plan is not suitable for this execution context.

Please provide a **revised plan for the remaining steps only** (starting from Step {failedStep.Index}).

Consider:
1. What went wrong in the failed step?
2. How should the approach change?
3. What are the remaining steps needed to complete the task?

Output your revised plan in the exact same JSON format as the original plan, containing only the remaining steps (index {failedStep.Index} onwards).

**Required JSON format:**
{{
  ""planVersion"": ""1"",
  ""taskId"": ""{task.Id}"",
  ""steps"": [
    {{
      ""index"": {failedStep.Index},
      ""type"": ""Shell | Agent"",
      ""description"": ""..."",
      ""command"": ""..."" // for Shell steps
      ""prompt"": ""..."" // for Agent steps
    }}
  ]
}}";

        return prompt;
    }
}
