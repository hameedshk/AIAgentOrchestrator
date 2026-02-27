using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.CrashRecovery;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;
using AIOrchestrator.Persistence.Abstractions;

namespace AIOrchestrator.App.Tests.CrashRecovery;

public class CrashRecoveryManagerIntegrationTests
{
    [Fact]
    public async Task Recovery_recovers_executing_task()
    {
        var taskId = Guid.NewGuid();
        var completedStep = new ExecutionStep { Index = 0, Type = StepType.Shell };
        completedStep.MarkStarted();
        completedStep.MarkCompleted("success");

        var runningStep = new ExecutionStep { Index = 1, Type = StepType.Shell };
        runningStep.MarkStarted();

        var executingTask = new OrchestratorTask
        {
            Id = taskId,
            Title = "Interrupted Task",
            Planner = ModelType.Claude,
            Executor = ModelType.Codex
        };
        executingTask.Enqueue();
        executingTask.StartPlanning();
        executingTask.ApprovePlan("1", [completedStep, runningStep]);
        executingTask.StartExecuting();
        executingTask.CurrentStepIndex = 1;

        var taskRepository = Substitute.For<ITaskRepository>();
        taskRepository.LoadAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { executingTask });

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var crashDetector = new CrashLoopDetector(tempDir);
        var recoveryCoordinator = new TaskRecoveryCoordinator();
        var logger = new RecoveryEventLogger(tempDir);

        var manager = new CrashRecoveryManager(taskRepository, crashDetector, recoveryCoordinator, logger);
        int recovered = await manager.RecoverAsync();

        recovered.Should().Be(1);
        manager.IsInSafeMode.Should().BeFalse();
        await taskRepository.Received(1).SaveAsync(Arg.Any<OrchestratorTask>(), Arg.Any<CancellationToken>());

        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task Recovery_enters_safe_mode_on_restart_loop()
    {
        var taskRepository = Substitute.For<ITaskRepository>();
        taskRepository.LoadAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<OrchestratorTask>());

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var crashDetector = new CrashLoopDetector(tempDir);
        for (int i = 0; i < 4; i++) crashDetector.RecordRestart();

        var recoveryCoordinator = new TaskRecoveryCoordinator();
        var logger = new RecoveryEventLogger(tempDir);

        var manager = new CrashRecoveryManager(taskRepository, crashDetector, recoveryCoordinator, logger);
        int recovered = await manager.RecoverAsync();

        recovered.Should().Be(0, "tasks not resumed in safe mode");
        manager.IsInSafeMode.Should().BeTrue();

        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task Recovery_logs_events_with_model_identity()
    {
        var taskId = Guid.NewGuid();
        var executingTask = new OrchestratorTask
        {
            Id = taskId,
            Title = "Test Task",
            Planner = ModelType.Claude,
            Executor = ModelType.Codex
        };
        executingTask.Enqueue();
        executingTask.StartPlanning();
        executingTask.ApprovePlan("1", []);
        executingTask.StartExecuting();

        var taskRepository = Substitute.For<ITaskRepository>();
        taskRepository.LoadAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { executingTask });

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var crashDetector = new CrashLoopDetector(tempDir);
        var recoveryCoordinator = new TaskRecoveryCoordinator();
        var logger = new RecoveryEventLogger(tempDir);

        var manager = new CrashRecoveryManager(taskRepository, crashDetector, recoveryCoordinator, logger);
        await manager.RecoverAsync();

        var logPath = Path.Combine(tempDir, "logs", "system.log");
        File.Exists(logPath).Should().BeTrue("recovery events should be logged");
        var logContent = File.ReadAllText(logPath);
        logContent.Should().Contain("TaskRecovered");
        logContent.Should().Contain("Codex");

        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task Recovery_resets_crash_counter_on_clean_shutdown()
    {
        var taskRepository = Substitute.For<ITaskRepository>();
        taskRepository.LoadAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<OrchestratorTask>());

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var crashDetector = new CrashLoopDetector(tempDir);
        crashDetector.RecordRestart();
        crashDetector.RecordRestart();

        var recoveryCoordinator = new TaskRecoveryCoordinator();
        var logger = new RecoveryEventLogger(tempDir);

        var manager = new CrashRecoveryManager(taskRepository, crashDetector, recoveryCoordinator, logger);
        manager.ResetCrashCounter();

        crashDetector.RestartCount.Should().Be(0);

        Directory.Delete(tempDir, recursive: true);
    }
}
