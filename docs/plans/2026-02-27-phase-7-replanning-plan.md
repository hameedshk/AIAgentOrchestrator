# Phase 7: Re-Planning Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Implement intelligent re-planning mechanism allowing the Planner to revise execution plans when Executor encounters non-retryable failures.

**Architecture:** Phase 7 adds re-planning to the execution flow by introducing a configurable `allowReplan` flag on tasks, creating an `IReplanningOrchestrator` that constructs replan prompts with failure context, validates revised plans, appends remaining steps to the task, and enforces max replan attempts (default 3). The decision to trigger re-planning is made in the Execution Engine where Phase 5 failure classification results are already checked.

**Tech Stack:** C# 13, .NET 10, xUnit, FluentAssertions, JSON serialization, existing CliSessionManager

---

## Task 1: Add allowReplan Field to OrchestratorTask Entity

**Files:**
- Modify: `src/AIOrchestrator.Domain/Entities/OrchestratorTask.cs`

**Step 1: Read the current OrchestratorTask entity to understand structure**

Read the file to see existing fields, properties, and how they're initialized.

**Step 2: Add allowReplan boolean property**

Add after existing properties:
```csharp
/// <summary>
/// Spec Section 4.5: When true, triggers Planner re-invocation on non-retryable Executor failures.
/// </summary>
public bool AllowReplan { get; set; } = false;
```

**Step 3: Add replanAttempts tracking property**

Add after allowReplan:
```csharp
/// <summary>
/// Tracks how many times this task has been re-planned (reset per original task).
/// Used to enforce max replan attempts (default 3).
/// </summary>
public int ReplanAttempts { get; set; } = 0;
```

**Step 4: Build to verify**

Run: `dotnet build src/AIOrchestrator.Domain/AIOrchestrator.Domain.csproj`

Expected: Build succeeds

**Step 5: Commit**

```bash
git add src/AIOrchestrator.Domain/Entities/OrchestratorTask.cs
git commit -m "feat: add allowReplan and replanAttempts properties to OrchestratorTask (Phase 7 Task 1)"
```

---

## Task 2: Add isFromReplan Field to ExecutionStep Entity

**Files:**
- Modify: `src/AIOrchestrator.Domain/Entities/ExecutionStep.cs`

**Step 1: Read the current ExecutionStep entity**

Understand existing step structure and properties.

**Step 2: Add isFromReplan boolean property**

Add to track steps that came from revised plan (for debugging and history):
```csharp
/// <summary>
/// Marks steps that came from a revised plan via re-planning.
/// Used for debugging and understanding execution history.
/// </summary>
public bool IsFromReplan { get; set; } = false;
```

**Step 3: Build to verify**

Run: `dotnet build src/AIOrchestrator.Domain/AIOrchestrator.Domain.csproj`

Expected: Build succeeds

**Step 4: Commit**

```bash
git add src/AIOrchestrator.Domain/Entities/ExecutionStep.cs
git commit -m "feat: add isFromReplan property to ExecutionStep for replan tracking (Phase 7 Task 2)"
```

---

## Task 3: Create IReplanningOrchestrator Interface

**Files:**
- Create: `src/AIOrchestrator.App/Replanning/IReplanningOrchestrator.cs`

**Step 1: Create directory and interface file**

```csharp
using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.App.Replanning;

/// <summary>
/// Orchestrates the re-planning flow when Executor encounters non-retryable failures.
/// Spec Section 4.5: Calls Planner again with original task, completed steps, failed step, and failure context.
/// Planner produces a revised plan for remaining steps only.
/// </summary>
public interface IReplanningOrchestrator
{
    /// <summary>
    /// Execute re-planning when a step fails non-retryably.
    /// </summary>
    /// <param name="task">Task being executed</param>
    /// <param name="failedStep">The step that failed</param>
    /// <param name="failureContext">Failure details from Phase 5 classification</param>
    /// <returns>Updated task with revised plan steps appended</returns>
    Task<OrchestratorTask> ReplanAsync(
        OrchestratorTask task,
        ExecutionStep failedStep,
        FailureContext failureContext,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a task is eligible for re-planning.
    /// </summary>
    bool CanReplan(OrchestratorTask task, FailureContext failureContext);
}
```

**Step 2: Build to verify**

Run: `dotnet build src/AIOrchestrator.App/AIOrchestrator.App.csproj`

Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/AIOrchestrator.App/Replanning/IReplanningOrchestrator.cs
git commit -m "feat: add IReplanningOrchestrator interface for re-planning orchestration (Phase 7 Task 3)"
```

---

## Task 4: Create ReplanPromptBuilder Utility

**Files:**
- Create: `src/AIOrchestrator.App/Replanning/ReplanPromptBuilder.cs`

**Step 1: Create replan prompt builder**

```csharp
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
**Failure Type:** {failureContext.FailureType}
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
      ""description"": ""...""",
      ""command"": ""..."" // for Shell steps
      ""prompt"": ""..."" // for Agent steps
    }}
  ]
}}";

        return prompt;
    }
}
```

**Step 2: Build to verify**

Run: `dotnet build src/AIOrchestrator.App/AIOrchestrator.App.csproj`

Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/AIOrchestrator.App/Replanning/ReplanPromptBuilder.cs
git commit -m "feat: add ReplanPromptBuilder for constructing replan prompts with failure context (Phase 7 Task 4)"
```

---

## Task 5: Implement ReplanningOrchestrator

**Files:**
- Create: `src/AIOrchestrator.App/Replanning/ReplanningOrchestrator.cs`

**Step 1: Create implementation**

```csharp
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
            revisedPlan = JsonSerializer.Deserialize<PlanDto>(revisedPlanJson);
            if (revisedPlan?.Steps == null || revisedPlan.Steps.Count == 0)
                throw new InvalidOperationException("Revised plan has no steps");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Planner produced invalid JSON during re-planning: {ex.Message}", ex);
        }

        // Append revised steps to task (marked as from replan)
        foreach (var revisedStep in revisedPlan.Steps)
        {
            var newStep = new ExecutionStep
            {
                Index = revisedStep.Index,
                Type = revisedStep.Type,
                Description = revisedStep.Description,
                Command = revisedStep.Command,
                Prompt = revisedStep.Prompt,
                Status = StepStatus.Pending,
                IsFromReplan = true  // Mark this step as from revised plan
            };
            task.Steps.Add(newStep);
        }

        // Update task replan counter and state
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
```

**Step 2: Build to verify**

Run: `dotnet build src/AIOrchestrator.App/AIOrchestrator.App.csproj`

Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/AIOrchestrator.App/Replanning/ReplanningOrchestrator.cs
git commit -m "feat: implement ReplanningOrchestrator for intelligent plan revision (Phase 7 Task 5)"
```

---

## Task 6: Write ReplanningOrchestrator Unit Tests

**Files:**
- Create: `tests/AIOrchestrator.App.Tests/Replanning/ReplanningOrchestratorTests.cs`

**Step 1: Create test file**

```csharp
using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.Replanning;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.App.Tests.Replanning;

public class ReplanningOrchestratorTests
{
    [Fact]
    public void CanReplan_returns_true_when_conditions_met()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            AllowReplan = true,
            ReplanAttempts = 0,
            Steps = new()
        };

        var failureContext = new FailureContext
        {
            FailureType = FailureType.RuntimeException,
            RawOutput = "Error",
            Retryable = false  // Non-retryable
        };

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        bool canReplan = orchestrator.CanReplan(task, failureContext);

        // Assert
        canReplan.Should().BeTrue();
    }

    [Fact]
    public void CanReplan_returns_false_when_allowReplan_false()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            AllowReplan = false,  // Not allowed
            ReplanAttempts = 0,
            Steps = new()
        };

        var failureContext = new FailureContext
        {
            FailureType = FailureType.RuntimeException,
            RawOutput = "Error",
            Retryable = false
        };

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        bool canReplan = orchestrator.CanReplan(task, failureContext);

        // Assert
        canReplan.Should().BeFalse();
    }

    [Fact]
    public void CanReplan_returns_false_when_failure_retryable()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            AllowReplan = true,
            ReplanAttempts = 0,
            Steps = new()
        };

        var failureContext = new FailureContext
        {
            FailureType = FailureType.CompileError,
            RawOutput = "Error",
            Retryable = true  // Retryable - should not replan
        };

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        bool canReplan = orchestrator.CanReplan(task, failureContext);

        // Assert
        canReplan.Should().BeFalse();
    }

    [Fact]
    public void CanReplan_returns_false_when_max_attempts_exceeded()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            AllowReplan = true,
            ReplanAttempts = 3,  // Already at max
            Steps = new()
        };

        var failureContext = new FailureContext
        {
            FailureType = FailureType.RuntimeException,
            RawOutput = "Error",
            Retryable = false
        };

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        bool canReplan = orchestrator.CanReplan(task, failureContext);

        // Assert
        canReplan.Should().BeFalse();
    }

    [Fact]
    public async Task ReplanAsync_appends_revised_steps_with_isFromReplan_flag()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test description",
            Planner = ModelType.Claude,
            AllowReplan = true,
            ReplanAttempts = 0,
            Steps = new()
            {
                new ExecutionStep { Index = 0, Type = StepType.Shell, Status = StepStatus.Completed },
                new ExecutionStep { Index = 1, Type = StepType.Shell, Status = StepStatus.Running }
            }
        };

        var failedStep = task.Steps[1];

        var failureContext = new FailureContext
        {
            FailureType = FailureType.RuntimeException,
            RawOutput = "Build failed",
            Retryable = false
        };

        var revisedPlanJson = @"{
            ""planVersion"": ""1"",
            ""taskId"": """ + task.Id + @""",
            ""steps"": [
                {
                    ""index"": 1,
                    ""type"": ""Shell"",
                    ""description"": ""Try alternative approach"",
                    ""command"": ""dotnet build --configuration Release""
                }
            ]
        }";

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        cliSessionManager.InvokeAsync(
            Arg.Any<ModelType>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(revisedPlanJson);

        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var updatedTask = await orchestrator.ReplanAsync(task, failedStep, failureContext);

        // Assert
        updatedTask.ReplanAttempts.Should().Be(1);
        updatedTask.Steps.Should().HaveCount(2);  // Original step 1 still there, new revised step added
        updatedTask.Steps[1].IsFromReplan.Should().BeTrue();  // New step marked as from replan
        updatedTask.CurrentStepIndex.Should().Be(1);  // Resume from failed step
    }

    [Fact]
    public async Task ReplanAsync_throws_on_invalid_json()
    {
        // Arrange
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Planner = ModelType.Claude,
            AllowReplan = true,
            Steps = new()
            {
                new ExecutionStep { Index = 0, Type = StepType.Shell, Status = StepStatus.Completed },
                new ExecutionStep { Index = 1, Type = StepType.Shell, Status = StepStatus.Running }
            }
        };

        var failedStep = task.Steps[1];
        var failureContext = new FailureContext { Retryable = false };

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        cliSessionManager.InvokeAsync(
            Arg.Any<ModelType>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns("invalid json {{{");

        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act & Assert
        await orchestrator.ReplanAsync(task, failedStep, failureContext)
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid JSON*");
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test tests/AIOrchestrator.App.Tests/Replanning/ReplanningOrchestratorTests.cs -v`

Expected: All 5 tests PASS

**Step 3: Commit**

```bash
git add tests/AIOrchestrator.App.Tests/Replanning/ReplanningOrchestratorTests.cs
git commit -m "test: add ReplanningOrchestrator unit tests (Phase 7 Task 6)"
```

---

## Task 7: Write ReplanPromptBuilder Unit Tests

**Files:**
- Create: `tests/AIOrchestrator.App.Tests/Replanning/ReplanPromptBuilderTests.cs`

**Step 1: Create test file**

```csharp
using FluentAssertions;
using AIOrchestrator.App.Replanning;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.Replanning;

public class ReplanPromptBuilderTests
{
    [Fact]
    public void BuildReplanPrompt_includes_original_task_info()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new OrchestratorTask
        {
            Id = taskId,
            Title = "Fix Authentication",
            Description = "Implement OAuth2 authentication",
            Steps = new()
        };

        var completedSteps = new List<ExecutionStep>().AsReadOnly();
        var failedStep = new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "Install packages" };
        var failureContext = new FailureContext { FailureType = FailureType.DependencyMissing, RawOutput = "npm not found" };

        // Act
        var prompt = ReplanPromptBuilder.BuildReplanPrompt(task, completedSteps, failedStep, failureContext);

        // Assert
        prompt.Should().Contain(task.Title);
        prompt.Should().Contain(taskId.ToString());
        prompt.Should().Contain(task.Description);
    }

    [Fact]
    public void BuildReplanPrompt_includes_failure_context()
    {
        // Arrange
        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", Steps = new() };
        var completedSteps = new List<ExecutionStep>().AsReadOnly();
        var failedStep = new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "Build" };
        var failureContext = new FailureContext
        {
            FailureType = FailureType.RuntimeException,
            RawOutput = "OutOfMemoryException: heap size exceeded"
        };

        // Act
        var prompt = ReplanPromptBuilder.BuildReplanPrompt(task, completedSteps, failedStep, failureContext);

        // Assert
        prompt.Should().Contain("RuntimeException");
        prompt.Should().Contain("OutOfMemoryException");
        prompt.Should().Contain("heap size exceeded");
    }

    [Fact]
    public void BuildReplanPrompt_includes_completed_steps_summary()
    {
        // Arrange
        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", Steps = new() };
        var completedSteps = new List<ExecutionStep>
        {
            new() { Index = 0, Type = StepType.Shell, Description = "Clone repo" },
            new() { Index = 1, Type = StepType.Agent, Description = "Analyze structure" }
        }.AsReadOnly();
        var failedStep = new ExecutionStep { Index = 2, Type = StepType.Shell, Description = "Build" };
        var failureContext = new FailureContext { Retryable = false };

        // Act
        var prompt = ReplanPromptBuilder.BuildReplanPrompt(task, completedSteps, failedStep, failureContext);

        // Assert
        prompt.Should().Contain("Steps Completed Successfully: 2");
        prompt.Should().Contain("Clone repo");
        prompt.Should().Contain("Analyze structure");
        prompt.Should().Contain("COMPLETED");
    }

    [Fact]
    public void BuildReplanPrompt_includes_json_format_instruction()
    {
        // Arrange
        var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", Steps = new() };
        var completedSteps = new List<ExecutionStep>().AsReadOnly();
        var failedStep = new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "Test" };
        var failureContext = new FailureContext { Retryable = false };

        // Act
        var prompt = ReplanPromptBuilder.BuildReplanPrompt(task, completedSteps, failedStep, failureContext);

        // Assert
        prompt.Should().Contain("planVersion");
        prompt.Should().Contain("taskId");
        prompt.Should().Contain("steps");
        prompt.Should().Contain("Shell | Agent");
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test tests/AIOrchestrator.App.Tests/Replanning/ReplanPromptBuilderTests.cs -v`

Expected: All 4 tests PASS

**Step 3: Commit**

```bash
git add tests/AIOrchestrator.App.Tests/Replanning/ReplanPromptBuilderTests.cs
git commit -m "test: add ReplanPromptBuilder unit tests (Phase 7 Task 7)"
```

---

## Task 8: Write End-to-End Re-Planning Integration Tests

**Files:**
- Create: `tests/AIOrchestrator.App.Tests/Replanning/ReplanningIntegrationTests.cs`

**Step 1: Create integration test file**

```csharp
using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.Replanning;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.App.Tests.Replanning;

public class ReplanningIntegrationTests
{
    [Fact]
    public async Task FullReplan_flow_from_failure_to_revised_plan_execution()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new OrchestratorTask
        {
            Id = taskId,
            Title = "Deploy Application",
            Planner = ModelType.Claude,
            Executor = ModelType.Codex,
            AllowReplan = true,
            ReplanAttempts = 0,
            Steps = new()
            {
                new ExecutionStep { Index = 0, Type = StepType.Shell, Description = "Build", Status = StepStatus.Completed },
                new ExecutionStep { Index = 1, Type = StepType.Shell, Description = "Test", Status = StepStatus.Running }
            }
        };

        var failedStep = task.Steps[1];
        var failureContext = new FailureContext
        {
            FailureType = FailureType.TestFailure,
            RawOutput = "Test suite failed: 3 failures detected",
            Retryable = false,
            PlannerModel = ModelType.Claude,
            ExecutorModel = ModelType.Codex
        };

        var revisedPlanJson = @"{
            ""planVersion"": ""1"",
            ""taskId"": """ + taskId + @""",
            ""steps"": [
                {
                    ""index"": 1,
                    ""type"": ""Agent"",
                    ""description"": ""Fix failing tests"",
                    ""prompt"": ""The tests failed. Analyze the failures and fix them.""
                },
                {
                    ""index"": 2,
                    ""type"": ""Shell"",
                    ""description"": ""Deploy"",
                    ""command"": ""kubectl apply -f deploy.yaml""
                }
            ]
        }";

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        cliSessionManager.InvokeAsync(
            Arg.Any<ModelType>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(revisedPlanJson);

        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var updatedTask = await orchestrator.ReplanAsync(task, failedStep, failureContext);

        // Assert - Verify revised plan was appended
        updatedTask.ReplanAttempts.Should().Be(1);
        updatedTask.Steps.Should().HaveCount(3);  // Original 2 + 1 new revised step (they consolidated to 1)

        // Verify revised steps are marked
        updatedTask.Steps.Where(s => s.IsFromReplan).Should().HaveCount(2);

        // Verify resume point
        updatedTask.CurrentStepIndex.Should().Be(1);

        // Verify Planner was invoked with failure context
        await cliSessionManager.Received(1).InvokeAsync(
            ModelType.Claude,
            Arg.Is<string>(p => p.Contains("Test suite failed") && p.Contains("Non-retryable")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recursive_replanning_works_with_second_failure()
    {
        // Arrange - Task has already been re-planned once
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            Planner = ModelType.Codex,
            AllowReplan = true,
            ReplanAttempts = 1,  // Already replanned once
            Steps = new()
            {
                new ExecutionStep { Index = 0, Type = StepType.Shell, Status = StepStatus.Completed },
                new ExecutionStep { Index = 1, Type = StepType.Shell, Status = StepStatus.Completed, IsFromReplan = true },
                new ExecutionStep { Index = 2, Type = StepType.Shell, Status = StepStatus.Running, IsFromReplan = true }
            }
        };

        var failedStep = task.Steps[2];
        var failureContext = new FailureContext { Retryable = false };

        var secondRevisedPlan = @"{
            ""planVersion"": ""1"",
            ""taskId"": """ + task.Id + @""",
            ""steps"": [
                {
                    ""index"": 2,
                    ""type"": ""Agent"",
                    ""description"": ""Alternative approach"",
                    ""prompt"": ""Try a different strategy""
                }
            ]
        }";

        var cliSessionManager = Substitute.For<ICliSessionManager>();
        cliSessionManager.InvokeAsync(
            Arg.Any<ModelType>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(secondRevisedPlan);

        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        var updatedTask = await orchestrator.ReplanAsync(task, failedStep, failureContext);

        // Assert - Second replan should work
        updatedTask.ReplanAttempts.Should().Be(2);
        updatedTask.Steps.Should().HaveCount(4);
    }

    [Fact]
    public void Replan_eligibility_prevents_excessive_replans()
    {
        // Arrange - Task already re-planned max times
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Task",
            AllowReplan = true,
            ReplanAttempts = 3,  // At maximum
            Steps = new()
        };

        var failureContext = new FailureContext { Retryable = false };
        var cliSessionManager = Substitute.For<ICliSessionManager>();
        var orchestrator = new ReplanningOrchestrator(cliSessionManager);

        // Act
        bool canReplan = orchestrator.CanReplan(task, failureContext);

        // Assert - Should not allow further replanning
        canReplan.Should().BeFalse();
    }
}
```

**Step 2: Run tests to verify they pass**

Run: `dotnet test tests/AIOrchestrator.App.Tests/Replanning/ReplanningIntegrationTests.cs -v`

Expected: All 3 tests PASS

**Step 3: Commit**

```bash
git add tests/AIOrchestrator.App.Tests/Replanning/ReplanningIntegrationTests.cs
git commit -m "test: add Re-Planning end-to-end integration tests (Phase 7 Task 8)"
```

---

## Task 9: Configure Dependency Injection for Re-Planning

**Files:**
- Create: `src/AIOrchestrator.App/DependencyInjection/ReplanningServiceCollectionExtensions.cs`

**Step 1: Create DI extension**

```csharp
using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.Replanning;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.App.DependencyInjection;

/// <summary>
/// Service collection extension for Phase 7 re-planning services.
/// </summary>
public static class ReplanningServiceCollectionExtensions
{
    /// <summary>
    /// Register re-planning services.
    /// </summary>
    public static IServiceCollection AddReplanning(this IServiceCollection services)
    {
        services.AddSingleton<IReplanningOrchestrator>(sp =>
            new ReplanningOrchestrator(
                sp.GetRequiredService<ICliSessionManager>()));

        return services;
    }
}
```

**Step 2: Build to verify**

Run: `dotnet build src/AIOrchestrator.App/AIOrchestrator.App.csproj`

Expected: Build succeeds

**Step 3: Commit**

```bash
git add src/AIOrchestrator.App/DependencyInjection/ReplanningServiceCollectionExtensions.cs
git commit -m "feat: add DI extension for re-planning services (Phase 7 Task 9)"
```

---

## Task 10: Create Re-Planning Architecture Documentation

**Files:**
- Create: `docs/architecture/REPLANNING.md`

**Step 1: Create documentation**

```markdown
# Re-Planning Architecture (Phase 7)

## Overview

Phase 7 implements intelligent re-planning, allowing the Planner to revise execution strategies when the Executor encounters non-retryable failures. This provides adaptive error recovery without manual intervention.

## Core Concept

**Re-Planning Flow:**
1. Executor encounters non-retryable failure (Phase 5)
2. If `task.AllowReplan = true`: Orchestrator escalates back to Planner
3. Planner receives: original task, completed steps, failed step, failure context
4. Planner produces revised plan (remaining steps only)
5. Revised steps appended to task (marked as `IsFromReplan = true`)
6. Execution resumes from failed step with new plan
7. If revised plan fails: may re-plan again (up to 3 times by default)

## Components

### IReplanningOrchestrator / ReplanningOrchestrator

**Purpose:** Orchestrates re-planning when non-retryable failures occur.

**Methods:**
- `CanReplan(task, failureContext)` → bool
  - Checks if re-planning is eligible (allowReplan=true, non-retryable, under attempt limit)

- `ReplanAsync(task, failedStep, failureContext, ct)` → Task<OrchestratorTask>
  - Invokes Planner CLI with failure context
  - Validates revised plan JSON (same format as initial planning)
  - Appends revised steps with `IsFromReplan = true`
  - Increments `ReplanAttempts` counter
  - Returns updated task ready for retry

**Safeguards:**
- Max replan attempts enforced (default 3, configurable)
- Failure context must indicate non-retryable
- Task must have `AllowReplan = true`

### ReplanPromptBuilder

**Purpose:** Constructs structured replan prompts with full failure context.

**Input:**
- Original task (title, description, ID)
- Completed steps summary
- Failed step details
- Failure context (type, error message, raw output)

**Output:**
- Natural language prompt explaining the situation
- JSON format specification for revised plan
- Clear instruction to provide remaining steps only

## Data Model Extensions

**OrchestratorTask additions:**
- `AllowReplan: bool` (default false) — Enable re-planning for this task
- `ReplanAttempts: int` — Tracks how many times task has been re-planned

**ExecutionStep additions:**
- `IsFromReplan: bool` (default false) — Marks steps from revised plan

## Integration with Other Phases

**Phase 3 (Planner Session):**
- Reuses same CLI invocation pattern
- Planner model determined by `task.Planner`

**Phase 4 (CLI Sessions):**
- Uses existing `ICliSessionManager` for Planner invocation
- Same completion detection and rehydration protocols

**Phase 5 (Failure Classification):**
- Checks `failureContext.Retryable` to determine if re-planning should trigger
- Non-retryable failures are candidates for re-planning

**Execution Engine:**
- Decision to trigger re-planning made after Phase 5 failure classification
- Checks `CanReplan()` before invoking re-planning

## Example Scenario

**Original Plan:**
```
Step 0: Build application (Shell)
Step 1: Run tests (Shell)
Step 2: Deploy (Shell)
```

**Execution Progress:**
- Step 0: ✅ Build succeeds
- Step 1: ❌ Tests fail with `CompileError` — non-retryable

**Trigger:** Non-retryable, `allowReplan=true` → invoke Planner

**Planner Input:**
```
Task: "Deploy Application"
Completed Steps:
  - Step 0: Build application — COMPLETED

Failed Step:
  - Step 1: Run tests
  - Failure: CompileError
  - Error: "Main.cs:42: undefined method 'Authenticate()'"

Please provide a revised plan for Steps 1 onwards.
```

**Revised Plan (from Planner):**
```json
{
  "steps": [
    {
      "index": 1,
      "type": "Agent",
      "description": "Fix compilation error in authentication",
      "prompt": "The test failed due to undefined method. Fix it."
    },
    {
      "index": 2,
      "type": "Shell",
      "description": "Deploy",
      "command": "kubectl apply -f deploy.yaml"
    }
  ]
}
```

**Execution Resumes:**
- Step 1 (revised): Agent fixes the method
- Step 2 (revised): Deploy succeeds

## Testing Strategy

**Unit Tests:**
- `CanReplan()` eligibility checks (all conditions)
- `ReplanAsync()` with valid/invalid plan JSON
- Prompt construction with context
- Replan attempt counting

**Integration Tests:**
- Full replan flow (failure → replan → revised execution)
- Recursive replanning (second failure in revised plan)
- Max attempt enforcement
- Both model pairings (Claude/Codex as Planner)

## Non-Functional Guarantees

✅ Re-planning only triggered on non-retryable failures
✅ Max replan attempts enforced (prevents infinite loops)
✅ Revised plan steps maintain original indices
✅ Step history preserved (original + revised visible)
✅ Planner model consistency (same model for re-planning)
✅ Full failure context provided to Planner
✅ JSON validation ensures revised plan integrity

## Configuration

From orchestrator.config.json (future phases):
```json
{
  "replanning": {
    "enableReplanning": true,
    "maxReplanAttempts": 3,
    "allowReplanByDefault": false
  }
}
```

## Out of Scope (V1)

- Parallel plan exploration (trying multiple revised plans)
- Plan caching/history for learning
- ML-based re-plan optimization
- User feedback integration for replanning decisions
```

**Step 2: Build to verify (no build needed for markdown)**

No build required.

**Step 3: Commit**

```bash
git add docs/architecture/REPLANNING.md
git commit -m "docs: add Phase 7 Re-Planning architecture guide (Phase 7 Task 10)"
```

---

## Task 11: Run Full Test Suite and Verify Build

**Step 1: Run all re-planning tests**

Run: `dotnet test tests/AIOrchestrator.App.Tests/Replanning/ -v`

Expected: All test cases PASS (12 total: 5 + 4 + 3)

**Step 2: Run full solution build**

Run: `dotnet build`

Expected: Build succeeds with zero warnings

**Step 3: Run all tests across entire solution**

Run: `dotnet test`

Expected: All tests pass (no regressions in other phases)

**Step 4: Verify git status**

Run: `git status`

Expected: Clean working directory, all changes committed

---

## Summary

**Phase 7 Complete:** Re-Planning with intelligent failure recovery:

✅ Task entity extended with `allowReplan` and `replanAttempts` fields
✅ ExecutionStep extended with `isFromReplan` tracking
✅ IReplanningOrchestrator interface and implementation
✅ ReplanPromptBuilder for context-aware prompts
✅ Recursive re-planning with max attempt safeguards
✅ 12 comprehensive test cases (5 + 4 + 3)
✅ Full DI integration
✅ Architecture documentation complete
✅ Integration ready with Execution Engine (Phase 8+)

**Commits:** 11 total (one per task)
**Tests:** 12 test cases across 3 test files
**Implementation:** ~500 lines code + ~600 lines tests

**Next Phase:** Phase 8 - Scheduler (priority queue, resource checks, multi-project dispatch)
