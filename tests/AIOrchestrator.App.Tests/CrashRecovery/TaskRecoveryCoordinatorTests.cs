using FluentAssertions;
using AIOrchestrator.App.CrashRecovery;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.CrashRecovery;

public class TaskRecoveryCoordinatorTests
{
    private readonly ITaskRecoveryCoordinator _coordinator = new TaskRecoveryCoordinator();

    [Fact]
    public void Coordinator_identifies_executing_tasks()
    {
        var executingTask = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task"
        };
        executingTask.Enqueue();
        executingTask.StartPlanning();
        executingTask.ApprovePlan("1", []);
        executingTask.StartExecuting();

        var completedTask = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Completed Task"
        };
        completedTask.Complete();

        var tasks = new[] { executingTask, completedTask };
        var recovering = _coordinator.IdentifyRecoveringTasks(tasks);

        recovering.Should().HaveCount(1);
        recovering[0].Id.Should().Be(executingTask.Id);
    }

    [Fact]
    public void Coordinator_identifies_planning_tasks()
    {
        var planningTask = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Planning Task"
        };
        planningTask.Enqueue();
        planningTask.StartPlanning();

        var tasks = new[] { planningTask };
        var recovering = _coordinator.IdentifyRecoveringTasks(tasks);

        recovering.Should().HaveCount(1);
        recovering[0].State.Should().Be(TaskState.Planning);
    }

    [Fact]
    public async Task Coordinator_resets_in_progress_steps()
    {
        var completedStep = new ExecutionStep { Index = 0, Type = StepType.Shell };
        completedStep.MarkStarted();
        completedStep.MarkCompleted("success");

        var runningStep = new ExecutionStep { Index = 1, Type = StepType.Shell };
        runningStep.MarkStarted();

        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Task with running step"
        };
        task.Enqueue();
        task.StartPlanning();
        task.ApprovePlan("1", [completedStep, runningStep]);
        task.StartExecuting();
        task.CurrentStepIndex = 0;

        var recovered = await _coordinator.RecoverTaskAsync(task);

        recovered.Steps[1].Status.Should().Be(StepStatus.Pending, "running step should be reset");
        recovered.CurrentStepIndex.Should().Be(1, "should resume from next step");
    }

    [Fact]
    public async Task Coordinator_sets_state_to_executing()
    {
        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Task"
        };
        task.Enqueue();
        task.StartPlanning();

        var recovered = await _coordinator.RecoverTaskAsync(task);

        recovered.State.Should().Be(TaskState.Executing);
    }

    [Fact]
    public async Task Coordinator_handles_no_completed_steps()
    {
        var runningStep = new ExecutionStep { Index = 0, Type = StepType.Shell };
        runningStep.MarkStarted();

        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Task with no completed steps"
        };
        task.Enqueue();
        task.StartPlanning();
        task.ApprovePlan("1", [runningStep]);
        task.StartExecuting();

        var recovered = await _coordinator.RecoverTaskAsync(task);

        recovered.CurrentStepIndex.Should().Be(0, "should start from step 0 if none completed");
    }
}
