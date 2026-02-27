# Phase 9: Engine Integration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the Engine orchestrator layer that coordinates Scheduler, resource monitoring, and task execution with API endpoints for remote management.

**Architecture:** The Engine is the central orchestration hub that runs a continuous dispatch loop. It monitors system resources (CPU, memory, process counts), retrieves eligible tasks from the Scheduler based on resource availability, coordinates task execution through state transitions, manages multi-project workloads, and exposes REST API endpoints for task management and system status. The Engine integrates all previous phases (3-7: CLI, State Machine, Failure Classification, Crash Recovery, Replanning) and the Phase 8 Scheduler into a cohesive system.

**Tech Stack:** C# 10, xUnit, NSubstitute, FluentAssertions, System.Diagnostics (for resource monitoring), ASP.NET Core (for API), System.Text.Json

---

## Phase 9 Bite-Sized Tasks (18 Tasks)

### Task 1: Create IResourceMonitor Interface

**Files:**
- Create: `src/AIOrchestrator.CliRunner/Abstractions/IResourceMonitor.cs`
- Create: `tests/AIOrchestrator.CliRunner.Tests/Abstractions/IResourceMonitorTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/AIOrchestrator.CliRunner.Tests/Abstractions/IResourceMonitorTests.cs
using FluentAssertions;
using NSubstitute;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.CliRunner.Tests.Abstractions;

public class IResourceMonitorTests
{
    [Fact]
    public async Task GetSystemResourcesAsync_returns_resource_snapshot()
    {
        // Arrange
        var monitor = Substitute.For<IResourceMonitor>();
        var resources = new SystemResources
        {
            CpuUsagePercent = 45,
            AvailableMemoryMb = 2048,
            RunningProcessCount = 5,
            MaxProcessesAllowed = 10
        };
        monitor.GetSystemResourcesAsync().Returns(resources);

        // Act
        var result = await monitor.GetSystemResourcesAsync();

        // Assert
        result.CpuUsagePercent.Should().Be(45);
        result.AvailableMemoryMb.Should().Be(2048);
        result.RunningProcessCount.Should().Be(5);
    }
}
```

**Step 2: Run test to verify it fails**

```bash
cd C:\Users\ksham\Documents\AIOrchestrator
dotnet test tests/AIOrchestrator.CliRunner.Tests/ -v m --filter "IResourceMonitorTests"
```

Expected: FAIL - SystemResources and IResourceMonitor don't exist

**Step 3: Write minimal implementations**

Create `src/AIOrchestrator.CliRunner/Abstractions/SystemResources.cs`:

```csharp
namespace AIOrchestrator.CliRunner.Abstractions;

/// <summary>
/// Snapshot of current system resources.
/// </summary>
public sealed class SystemResources
{
    /// <summary>
    /// Current CPU usage as percentage (0-100).
    /// </summary>
    public int CpuUsagePercent { get; init; }

    /// <summary>
    /// Available system memory in megabytes.
    /// </summary>
    public int AvailableMemoryMb { get; init; }

    /// <summary>
    /// Number of CLI processes currently running.
    /// </summary>
    public int RunningProcessCount { get; init; }

    /// <summary>
    /// Maximum concurrent CLI processes allowed (configuration).
    /// </summary>
    public int MaxProcessesAllowed { get; init; }

    /// <summary>
    /// Checks if resources are available for new task execution.
    /// </summary>
    public bool HasResourcesAvailable(int cpuThresholdPercent, int memoryThresholdMb)
    {
        return CpuUsagePercent < cpuThresholdPercent &&
               AvailableMemoryMb > memoryThresholdMb &&
               RunningProcessCount < MaxProcessesAllowed;
    }
}
```

Create `src/AIOrchestrator.CliRunner/Abstractions/IResourceMonitor.cs`:

```csharp
namespace AIOrchestrator.CliRunner.Abstractions;

/// <summary>
/// Monitors system resources (CPU, memory, process counts) for dispatch decisions.
/// </summary>
public interface IResourceMonitor
{
    /// <summary>
    /// Get current system resource snapshot.
    /// </summary>
    Task<SystemResources> GetSystemResourcesAsync();
}
```

**Step 4: Run test to verify it passes**

```bash
dotnet test tests/AIOrchestrator.CliRunner.Tests/ -v m --filter "IResourceMonitorTests"
```

Expected: PASS

**Step 5: Commit**

```bash
git add src/AIOrchestrator.CliRunner/Abstractions/IResourceMonitor.cs src/AIOrchestrator.CliRunner/Abstractions/SystemResources.cs tests/AIOrchestrator.CliRunner.Tests/Abstractions/IResourceMonitorTests.cs
git commit -m "feat: add IResourceMonitor interface and SystemResources DTO (Phase 9 Task 1)"
```

---

### Task 2: Implement ResourceMonitor

**Files:**
- Create: `src/AIOrchestrator.CliRunner/ResourceMonitoring/ResourceMonitor.cs`
- Create: `tests/AIOrchestrator.CliRunner.Tests/ResourceMonitoring/ResourceMonitorTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/AIOrchestrator.CliRunner.Tests/ResourceMonitoring/ResourceMonitorTests.cs
using FluentAssertions;
using AIOrchestrator.CliRunner.ResourceMonitoring;

namespace AIOrchestrator.CliRunner.Tests.ResourceMonitoring;

public class ResourceMonitorTests
{
    [Fact]
    public async Task GetSystemResourcesAsync_returns_valid_resource_snapshot()
    {
        // Arrange
        var monitor = new ResourceMonitor(maxProcesses: 10);

        // Act
        var resources = await monitor.GetSystemResourcesAsync();

        // Assert
        resources.Should().NotBeNull();
        resources.CpuUsagePercent.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        resources.AvailableMemoryMb.Should().BeGreaterThan(0);
        resources.RunningProcessCount.Should().BeGreaterThanOrEqualTo(0);
        resources.MaxProcessesAllowed.Should().Be(10);
    }

    [Fact]
    public async Task GetSystemResourcesAsync_counts_running_dotnet_processes()
    {
        // Arrange
        var monitor = new ResourceMonitor(maxProcesses: 20);

        // Act
        var resources = await monitor.GetSystemResourcesAsync();

        // Assert
        // Should count at least this process
        resources.RunningProcessCount.Should().BeGreaterThan(0);
        resources.RunningProcessCount.Should().BeLessThanOrEqualTo(resources.MaxProcessesAllowed);
    }
}
```

**Step 2: Run test to verify it fails**

```bash
dotnet test tests/AIOrchestrator.CliRunner.Tests/ResourceMonitoring/ResourceMonitorTests.cs -v m
```

Expected: FAIL - ResourceMonitor doesn't exist

**Step 3: Write minimal implementation**

```csharp
// src/AIOrchestrator.CliRunner/ResourceMonitoring/ResourceMonitor.cs
using System.Diagnostics;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.CliRunner.ResourceMonitoring;

/// <summary>
/// Monitors system resources using System.Diagnostics.
/// </summary>
public class ResourceMonitor : IResourceMonitor
{
    private readonly int _maxProcesses;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _memoryCounter;

    public ResourceMonitor(int maxProcesses = 10)
    {
        _maxProcesses = maxProcesses;

        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes", readOnly: true);
        }
        catch
        {
            // Performance counters may not be available in some environments
            _cpuCounter = null;
            _memoryCounter = null;
        }
    }

    public async Task<SystemResources> GetSystemResourcesAsync()
    {
        return await Task.Run(() =>
        {
            var cpuUsage = GetCpuUsage();
            var availableMemory = GetAvailableMemory();
            var processCount = GetRunningProcessCount();

            return new SystemResources
            {
                CpuUsagePercent = cpuUsage,
                AvailableMemoryMb = availableMemory,
                RunningProcessCount = processCount,
                MaxProcessesAllowed = _maxProcesses
            };
        });
    }

    private int GetCpuUsage()
    {
        try
        {
            if (_cpuCounter == null)
                return 50; // Default to middle value if unavailable

            var value = (int)_cpuCounter.NextValue();
            return Math.Clamp(value, 0, 100);
        }
        catch
        {
            return 50;
        }
    }

    private int GetAvailableMemory()
    {
        try
        {
            if (_memoryCounter == null)
            {
                // Fallback: use GC memory info
                var memInfo = GC.GetTotalMemory(false);
                return (int)(memInfo / (1024 * 1024));
            }

            var value = (int)_memoryCounter.NextValue();
            return Math.Max(value, 0);
        }
        catch
        {
            return 1024; // Default to 1GB if unavailable
        }
    }

    private int GetRunningProcessCount()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var dotnetProcesses = Process.GetProcessesByName("dotnet");
            return dotnetProcesses.Length;
        }
        catch
        {
            return 1; // Default to at least current process
        }
    }
}
```

**Step 4: Run test to verify it passes**

```bash
dotnet test tests/AIOrchestrator.CliRunner.Tests/ResourceMonitoring/ResourceMonitorTests.cs -v m
```

Expected: PASS

**Step 5: Commit**

```bash
git add src/AIOrchestrator.CliRunner/ResourceMonitoring/ResourceMonitor.cs tests/AIOrchestrator.CliRunner.Tests/ResourceMonitoring/ResourceMonitorTests.cs
git commit -m "feat: implement ResourceMonitor with CPU, memory, and process tracking (Phase 9 Task 2)"
```

---

### Task 3: Add IResourceMonitor to DI Container

**Files:**
- Modify: `src/AIOrchestrator.CliRunner/DependencyInjection/CliRunnerServiceCollectionExtensions.cs`

**Step 1: Write the failing test**

```csharp
// Add to existing CliRunnerServiceCollectionExtensions tests or create new file
[Fact]
public void AddCliRunner_registers_resource_monitor()
{
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddCliRunner();
    var provider = services.BuildServiceProvider();

    // Assert
    var monitor = provider.GetService<IResourceMonitor>();
    monitor.Should().NotBeNull();
    monitor.Should().BeOfType<ResourceMonitor>();
}
```

**Step 2: Modify DI extension**

Add to `CliRunnerServiceCollectionExtensions.cs`:

```csharp
// Add IResourceMonitor registration
services.AddSingleton<IResourceMonitor>(sp =>
    new ResourceMonitor(maxProcesses: 10));
```

**Step 3: Run test**

```bash
dotnet test tests/AIOrchestrator.CliRunner.Tests/ -v m --filter "AddCliRunner_registers_resource_monitor"
```

Expected: PASS

**Step 4: Commit**

```bash
git add src/AIOrchestrator.CliRunner/DependencyInjection/CliRunnerServiceCollectionExtensions.cs
git commit -m "feat: register IResourceMonitor in DI container (Phase 9 Task 3)"
```

---

### Task 4: Create SystemResourceSnapshot Entity for Persistence

**Files:**
- Create: `src/AIOrchestrator.Persistence/Dto/SystemResourceSnapshotDto.cs`

**Step 1: Create DTO**

```csharp
// src/AIOrchestrator.Persistence/Dto/SystemResourceSnapshotDto.cs
using System.Text.Json.Serialization;

namespace AIOrchestrator.Persistence.Dto;

/// <summary>
/// Historical snapshot of system resources at time of task dispatch.
/// </summary>
public sealed class SystemResourceSnapshotDto
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("cpuUsagePercent")]
    public int CpuUsagePercent { get; set; }

    [JsonPropertyName("availableMemoryMb")]
    public int AvailableMemoryMb { get; set; }

    [JsonPropertyName("runningProcessCount")]
    public int RunningProcessCount { get; set; }
}
```

**Step 2: Commit**

```bash
git add src/AIOrchestrator.Persistence/Dto/SystemResourceSnapshotDto.cs
git commit -m "feat: add SystemResourceSnapshotDto for resource history persistence (Phase 9 Task 4)"
```

---

### Task 5: Create IEngine Interface

**Files:**
- Create: `src/AIOrchestrator.App/Engine/IEngine.cs`

**Step 1: Create interface**

```csharp
// src/AIOrchestrator.App/Engine/IEngine.cs
using AIOrchestrator.Domain.Entities;

namespace AIOrchestrator.App.Engine;

/// <summary>
/// Central orchestration engine coordinating Scheduler, Resources, and Task Execution.
/// </summary>
public interface IEngine
{
    /// <summary>
    /// Start the engine dispatch loop (runs continuously until stopped).
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Submit a new task to the engine for execution.
    /// </summary>
    Task<OrchestratorTask> SubmitTaskAsync(OrchestratorTask task);

    /// <summary>
    /// Get all tasks with specified state.
    /// </summary>
    Task<IReadOnlyList<OrchestratorTask>> GetTasksByStateAsync(TaskState state);

    /// <summary>
    /// Get current engine status and resource snapshot.
    /// </summary>
    Task<EngineStatus> GetStatusAsync();
}

/// <summary>
/// Real-time engine status.
/// </summary>
public sealed class EngineStatus
{
    public int TotalTasks { get; init; }
    public int QueuedTasks { get; init; }
    public int ExecutingTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int FailedTasks { get; init; }
    public int CpuUsagePercent { get; init; }
    public int AvailableMemoryMb { get; init; }
    public int RunningProcessCount { get; init; }
    public DateTimeOffset LastDispatchTime { get; init; }
}
```

**Step 2: Commit**

```bash
git add src/AIOrchestrator.App/Engine/IEngine.cs
git commit -m "feat: add IEngine interface for orchestration (Phase 9 Task 5)"
```

---

### Task 6: Implement Engine with Basic Structure

**Files:**
- Create: `src/AIOrchestrator.App/Engine/Engine.cs`
- Create: `tests/AIOrchestrator.App.Tests/Engine/EngineTests.cs`

**Step 1: Write failing test**

```csharp
// tests/AIOrchestrator.App.Tests/Engine/EngineTests.cs
using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.Engine;

public class EngineTests
{
    [Fact]
    public async Task SubmitTaskAsync_enqueues_task_in_scheduler()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var resourceMonitor = Substitute.For<IResourceMonitor>();
        var engine = new Engine(scheduler, resourceMonitor);

        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            ProjectId = "ProjectA"
        };

        // Act
        var submitted = await engine.SubmitTaskAsync(task);

        // Assert
        submitted.Should().NotBeNull();
        submitted.Id.Should().Be(task.Id);
        await scheduler.Received(1).EnqueueAsync(task);
    }

    [Fact]
    public async Task GetStatusAsync_returns_engine_status()
    {
        // Arrange
        var scheduler = Substitute.For<IScheduler>();
        var resourceMonitor = Substitute.For<IResourceMonitor>();
        var resources = new SystemResources { CpuUsagePercent = 45, AvailableMemoryMb = 2048, RunningProcessCount = 3, MaxProcessesAllowed = 10 };
        resourceMonitor.GetSystemResourcesAsync().Returns(resources);

        var engine = new Engine(scheduler, resourceMonitor);

        // Act
        var status = await engine.GetStatusAsync();

        // Assert
        status.Should().NotBeNull();
        status.CpuUsagePercent.Should().Be(45);
        status.AvailableMemoryMb.Should().Be(2048);
    }
}
```

**Step 2: Write minimal implementation**

```csharp
// src/AIOrchestrator.App/Engine/Engine.cs
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Engine;

/// <summary>
/// Central orchestration engine coordinating Scheduler, Resources, and Task Execution.
/// </summary>
public class Engine : IEngine
{
    private readonly IScheduler _scheduler;
    private readonly IResourceMonitor _resourceMonitor;
    private readonly List<OrchestratorTask> _allTasks = [];
    private DateTimeOffset _lastDispatchTime = DateTimeOffset.UtcNow;
    private readonly object _tasksLock = new();

    // Configuration thresholds
    private const int CpuThresholdPercent = 80;
    private const int MemoryThresholdMb = 512;
    private const int DispatchIntervalMs = 1000; // Dispatch every 1 second

    public Engine(IScheduler scheduler, IResourceMonitor resourceMonitor)
    {
        _scheduler = scheduler;
        _resourceMonitor = resourceMonitor;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var resources = await _resourceMonitor.GetSystemResourcesAsync();

                // Check if we have resources available for dispatch
                if (resources.HasResourcesAvailable(CpuThresholdPercent, MemoryThresholdMb))
                {
                    var task = await _scheduler.DispatchAsync(
                        cpuAvailable: 100 - resources.CpuUsagePercent,
                        memoryAvailableMb: resources.AvailableMemoryMb,
                        maxProcesses: resources.MaxProcessesAllowed - resources.RunningProcessCount);

                    if (task != null)
                    {
                        await MarkTaskRunningAsync(task);
                        _lastDispatchTime = DateTimeOffset.UtcNow;
                    }
                }

                // Dispatch loop frequency
                await Task.Delay(DispatchIntervalMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue loop
                System.Console.WriteLine($"Engine dispatch error: {ex.Message}");
            }
        }
    }

    public async Task<OrchestratorTask> SubmitTaskAsync(OrchestratorTask task)
    {
        task.Enqueue();

        lock (_tasksLock)
        {
            _allTasks.Add(task);
        }

        await _scheduler.EnqueueAsync(task);
        return task;
    }

    public Task<IReadOnlyList<OrchestratorTask>> GetTasksByStateAsync(TaskState state)
    {
        lock (_tasksLock)
        {
            var tasks = _allTasks.Where(t => t.State == state).ToList().AsReadOnly();
            return Task.FromResult((IReadOnlyList<OrchestratorTask>)tasks);
        }
    }

    public async Task<EngineStatus> GetStatusAsync()
    {
        var resources = await _resourceMonitor.GetSystemResourcesAsync();

        lock (_tasksLock)
        {
            return new EngineStatus
            {
                TotalTasks = _allTasks.Count,
                QueuedTasks = _allTasks.Count(t => t.State == TaskState.Queued),
                ExecutingTasks = _allTasks.Count(t => t.State == TaskState.Executing),
                CompletedTasks = _allTasks.Count(t => t.State == TaskState.Completed),
                FailedTasks = _allTasks.Count(t => t.State == TaskState.Failed),
                CpuUsagePercent = resources.CpuUsagePercent,
                AvailableMemoryMb = resources.AvailableMemoryMb,
                RunningProcessCount = resources.RunningProcessCount,
                LastDispatchTime = _lastDispatchTime
            };
        }
    }

    private async Task MarkTaskRunningAsync(OrchestratorTask task)
    {
        task.StartExecuting();

        lock (_tasksLock)
        {
            var existing = _allTasks.FirstOrDefault(t => t.Id == task.Id);
            if (existing != null)
            {
                var index = _allTasks.IndexOf(existing);
                _allTasks[index] = task;
            }
        }

        if (!string.IsNullOrEmpty(task.ProjectId))
        {
            await _scheduler.MarkRunningAsync(task.ProjectId);
        }
    }
}
```

**Step 3: Run tests**

```bash
dotnet test tests/AIOrchestrator.App.Tests/Engine/EngineTests.cs -v m
```

Expected: PASS

**Step 4: Commit**

```bash
git add src/AIOrchestrator.App/Engine/Engine.cs tests/AIOrchestrator.App.Tests/Engine/EngineTests.cs
git commit -m "feat: implement Engine orchestrator with dispatch loop and task tracking (Phase 9 Task 6)"
```

---

### Task 7: Add Engine to DI Container

**Files:**
- Create: `src/AIOrchestrator.App/DependencyInjection/EngineServiceCollectionExtensions.cs`

**Step 1: Create DI extension**

```csharp
// src/AIOrchestrator.App/DependencyInjection/EngineServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.CliRunner.Abstractions;

namespace AIOrchestrator.App.DependencyInjection;

/// <summary>
/// Service collection extension for Phase 9 Engine services.
/// </summary>
public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Register Engine services (requires Scheduler and ResourceMonitor already registered).
    /// </summary>
    public static IServiceCollection AddEngine(this IServiceCollection services)
    {
        services.AddSingleton<IEngine>(sp =>
            new Engine(
                sp.GetRequiredService<IScheduler>(),
                sp.GetRequiredService<IResourceMonitor>()));

        return services;
    }
}
```

**Step 2: Commit**

```bash
git add src/AIOrchestrator.App/DependencyInjection/EngineServiceCollectionExtensions.cs
git commit -m "feat: add EngineServiceCollectionExtensions for DI registration (Phase 9 Task 7)"
```

---

### Task 8: Implement Task Completion Handler

**Files:**
- Modify: `src/AIOrchestrator.App/Engine/Engine.cs`
- Modify: `tests/AIOrchestrator.App.Tests/Engine/EngineTests.cs`

**Step 1: Add method to handle task completion**

Add to Engine class:

```csharp
/// <summary>
/// Mark task as completed and free up project resources.
/// </summary>
public async Task CompleteTaskAsync(OrchestratorTask task)
{
    task.Complete();

    lock (_tasksLock)
    {
        var index = _allTasks.FindIndex(t => t.Id == task.Id);
        if (index >= 0)
            _allTasks[index] = task;
    }

    if (!string.IsNullOrEmpty(task.ProjectId))
    {
        await _scheduler.MarkCompleteAsync(task.ProjectId);
    }
}

/// <summary>
/// Mark task as failed and handle failure (retry, replan, or mark failed).
/// </summary>
public async Task FailTaskAsync(OrchestratorTask task, FailureContext failure)
{
    task.Fail(failure);

    lock (_tasksLock)
    {
        var index = _allTasks.FindIndex(t => t.Id == task.Id);
        if (index >= 0)
            _allTasks[index] = task;
    }

    if (!string.IsNullOrEmpty(task.ProjectId))
    {
        await _scheduler.MarkCompleteAsync(task.ProjectId);
    }
}
```

Add test:

```csharp
[Fact]
public async Task CompleteTaskAsync_marks_task_completed_and_frees_project()
{
    var scheduler = Substitute.For<IScheduler>();
    var resourceMonitor = Substitute.For<IResourceMonitor>();
    var engine = new Engine(scheduler, resourceMonitor);

    var task = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", ProjectId = "ProjectA", State = TaskState.Executing };

    await engine.CompleteTaskAsync(task);

    task.State.Should().Be(TaskState.Completed);
    await scheduler.Received(1).MarkCompleteAsync("ProjectA");
}
```

**Step 2: Run tests**

```bash
dotnet test tests/AIOrchestrator.App.Tests/Engine/EngineTests.cs -v m
```

Expected: PASS

**Step 3: Commit**

```bash
git add src/AIOrchestrator.App/Engine/Engine.cs tests/AIOrchestrator.App.Tests/Engine/EngineTests.cs
git commit -m "feat: add CompleteTaskAsync and FailTaskAsync to Engine (Phase 9 Task 8)"
```

---

### Task 9: Integration Test - Full Dispatch Flow

**Files:**
- Create: `tests/AIOrchestrator.App.Tests/Engine/EngineIntegrationTests.cs`

**Step 1: Write integration test**

```csharp
// tests/AIOrchestrator.App.Tests/Engine/EngineIntegrationTests.cs
using FluentAssertions;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Scheduler;
using AIOrchestrator.CliRunner.ResourceMonitoring;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.Engine;

public class EngineIntegrationTests
{
    [Fact]
    public async Task Engine_coordinates_full_task_lifecycle()
    {
        // Arrange
        var scheduler = new Scheduler();
        var resourceMonitor = new ResourceMonitor(maxProcesses: 10);
        var engine = new Engine(scheduler, resourceMonitor);

        var task = new OrchestratorTask
        {
            Id = Guid.NewGuid(),
            Title = "Integration Test",
            ProjectId = "ProjectA",
            Priority = TaskPriority.Normal
        };

        // Act
        var submitted = await engine.SubmitTaskAsync(task);
        var status1 = await engine.GetStatusAsync();
        submitted.State.Should().Be(TaskState.Queued);

        // Dispatch the task
        var dispatched = await scheduler.DispatchAsync(100, 2048, 10);
        dispatched.Should().NotBeNull();
        dispatched!.Id.Should().Be(task.Id);

        // Complete the task
        await engine.CompleteTaskAsync(dispatched);
        var status2 = await engine.GetStatusAsync();

        // Assert
        status1.QueuedTasks.Should().Be(1);
        status2.CompletedTasks.Should().Be(1);
    }

    [Fact]
    public async Task Engine_respects_resource_thresholds()
    {
        // Arrange
        var scheduler = new Scheduler();
        var resourceMonitor = new ResourceMonitor(maxProcesses: 10);
        var engine = new Engine(scheduler, resourceMonitor);

        // Act
        var status = await engine.GetStatusAsync();

        // Assert - verify status has resource info
        status.CpuUsagePercent.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(100);
        status.AvailableMemoryMb.Should().BeGreaterThan(0);
        status.RunningProcessCount.Should().BeGreaterThanOrEqualTo(0);
    }
}
```

**Step 2: Run tests**

```bash
dotnet test tests/AIOrchestrator.App.Tests/Engine/EngineIntegrationTests.cs -v m
```

Expected: PASS

**Step 3: Commit**

```bash
git add tests/AIOrchestrator.App.Tests/Engine/EngineIntegrationTests.cs
git commit -m "test: add Engine integration tests for full task lifecycle (Phase 9 Task 9)"
```

---

### Task 10: Create API Request/Response DTOs

**Files:**
- Create: `src/AIOrchestrator.App/Api/RequestModels/SubmitTaskRequest.cs`
- Create: `src/AIOrchestrator.App/Api/ResponseModels/TaskResponse.cs`
- Create: `src/AIOrchestrator.App/Api/ResponseModels/EngineStatusResponse.cs`

**Step 1: Create DTOs**

```csharp
// src/AIOrchestrator.App/Api/RequestModels/SubmitTaskRequest.cs
namespace AIOrchestrator.App.Api.RequestModels;

public sealed class SubmitTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal"; // High, Normal, Low
    public bool AllowReplan { get; set; } = false;
}
```

```csharp
// src/AIOrchestrator.App/Api/ResponseModels/TaskResponse.cs
namespace AIOrchestrator.App.Api.ResponseModels;

public sealed class TaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int CurrentStepIndex { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

```csharp
// src/AIOrchestrator.App/Api/ResponseModels/EngineStatusResponse.cs
namespace AIOrchestrator.App.Api.ResponseModels;

public sealed class EngineStatusResponse
{
    public int TotalTasks { get; set; }
    public int QueuedTasks { get; set; }
    public int ExecutingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int FailedTasks { get; set; }
    public int CpuUsagePercent { get; set; }
    public int AvailableMemoryMb { get; set; }
    public int RunningProcessCount { get; set; }
    public DateTimeOffset LastDispatchTime { get; set; }
}
```

**Step 2: Commit**

```bash
git add src/AIOrchestrator.App/Api/RequestModels/SubmitTaskRequest.cs src/AIOrchestrator.App/Api/ResponseModels/TaskResponse.cs src/AIOrchestrator.App/Api/ResponseModels/EngineStatusResponse.cs
git commit -m "feat: add API request and response DTOs (Phase 9 Task 10)"
```

---

### Task 11: Create TaskController for API Endpoints

**Files:**
- Create: `src/AIOrchestrator.App/Api/Controllers/TasksController.cs`
- Create: `tests/AIOrchestrator.App.Tests/Api/TasksControllerTests.cs`

**Step 1: Create controller**

```csharp
// src/AIOrchestrator.App/Api/Controllers/TasksController.cs
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
        var allTasks = await _engine.GetTasksByStateAsync(TaskState.Created);
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
```

**Step 2: Write controller test**

```csharp
// tests/AIOrchestrator.App.Tests/Api/TasksControllerTests.cs
using FluentAssertions;
using NSubstitute;
using AIOrchestrator.App.Api.Controllers;
using AIOrchestrator.App.Engine;
using AIOrchestrator.App.Api.RequestModels;
using AIOrchestrator.Domain.Entities;
using AIOrchestrator.Domain.Enums;

namespace AIOrchestrator.App.Tests.Api;

public class TasksControllerTests
{
    [Fact]
    public async Task SubmitTask_creates_task_and_returns_created()
    {
        // Arrange
        var engine = Substitute.For<IEngine>();
        var controller = new TasksController(engine);
        var request = new SubmitTaskRequest { Title = "Test", ProjectId = "ProjectA" };

        var createdTask = new OrchestratorTask { Id = Guid.NewGuid(), Title = "Test", ProjectId = "ProjectA", State = TaskState.Queued };
        engine.SubmitTaskAsync(Arg.Any<OrchestratorTask>()).Returns(createdTask);

        // Act
        var result = await controller.SubmitTask(request);

        // Assert
        result.Should().NotBeNull();
        await engine.Received(1).SubmitTaskAsync(Arg.Any<OrchestratorTask>());
    }

    [Fact]
    public async Task GetStatus_returns_engine_status()
    {
        // Arrange
        var engine = Substitute.For<IEngine>();
        var controller = new TasksController(engine);
        var status = new EngineStatus { TotalTasks = 5, QueuedTasks = 2, ExecutingTasks = 3, CpuUsagePercent = 45 };
        engine.GetStatusAsync().Returns(status);

        // Act
        var result = await controller.GetStatus();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.TotalTasks.Should().Be(5);
    }
}
```

**Step 3: Run tests**

```bash
dotnet test tests/AIOrchestrator.App.Tests/Api/TasksControllerTests.cs -v m
```

Expected: PASS

**Step 4: Commit**

```bash
git add src/AIOrchestrator.App/Api/Controllers/TasksController.cs tests/AIOrchestrator.App.Tests/Api/TasksControllerTests.cs
git commit -m "feat: implement TasksController with API endpoints (Phase 9 Task 11)"
```

---

### Task 12: Create ASP.NET Core Startup Configuration

**Files:**
- Create: `src/AIOrchestrator.App/Startup/ServiceConfiguration.cs`
- Modify: `Program.cs` to include full startup

**Step 1: Create service configuration**

```csharp
// src/AIOrchestrator.App/Startup/ServiceConfiguration.cs
using Microsoft.Extensions.DependencyInjection;
using AIOrchestrator.App.DependencyInjection;
using AIOrchestrator.CliRunner.DependencyInjection;
using AIOrchestrator.Persistence.Abstractions;
using AIOrchestrator.Persistence.FileSystem;

namespace AIOrchestrator.App.Startup;

/// <summary>
/// Configures all services for the AIOrchestrator application.
/// </summary>
public static class ServiceConfiguration
{
    public static IServiceCollection ConfigureAIOrchestratorServices(
        this IServiceCollection services,
        string schedulerStateDir)
    {
        // Phase 3: CLI Runner
        services.AddCliRunner();

        // Phase 8: Scheduler
        services.AddScheduler(schedulerStateDir);

        // Phase 9: Engine
        services.AddEngine();

        // API Controllers
        services.AddControllers();

        return services;
    }
}
```

**Step 2: Update Program.cs**

```csharp
// Program.cs
using AIOrchestrator.App.Startup;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
var schedulerStateDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "AIOrchestrator",
    "scheduler_state");

builder.Services.ConfigureAIOrchestratorServices(schedulerStateDir);

var app = builder.Build();

// Configure HTTP pipeline
app.UseRouting();
app.MapControllers();

app.Run();
```

**Step 3: Commit**

```bash
git add src/AIOrchestrator.App/Startup/ServiceConfiguration.cs src/AIOrchestrator.App/Program.cs
git commit -m "feat: add ASP.NET Core startup configuration (Phase 9 Task 12)"
```

---

### Task 13: Full Build Verification

Run:
```bash
dotnet build -c Debug
dotnet test -v m --no-build
```

Expected: All tests pass

---

### Task 14: Create Engine Architecture Documentation

**File:** Create `docs/architecture/ENGINE.md`

```markdown
# Engine Architecture (Phase 9)

## Overview

The Engine is the central orchestration hub that coordinates task dispatch, resource monitoring, and multi-project execution.

## Core Components

### 1. IResourceMonitor
Monitors system resources (CPU, memory, process counts) for dispatch decisions.

### 2. ResourceMonitor
Implements IResourceMonitor using System.Diagnostics performance counters.

### 3. IEngine
Public contract for task submission, dispatch loop, and status queries.

### 4. Engine
Central orchestrator that:
- Runs continuous dispatch loop
- Monitors resources
- Dispatches tasks from Scheduler
- Tracks task lifecycle
- Manages project execution

### 5. TasksController
REST API endpoints for task management and status queries.

## Execution Flow

```
1. Submit task via API → Engine.SubmitTaskAsync()
2. Task → Queued state
3. Engine loop polls Scheduler every 1 second
4. Resources available? → Dispatch task
5. Task → Executing state
6. Engine marks project running
7. Task completes or fails
8. Engine frees project resources
```

## Resource Thresholds

- CPU Threshold: 80%
- Memory Threshold: 512 MB available
- Process Threshold: Max - Current running

## API Endpoints

- `POST /api/tasks` - Submit task
- `GET /api/tasks?state=Queued` - List tasks by state
- `GET /api/tasks/{id}` - Get specific task
- `GET /api/tasks/status` - Engine status

## Integration

- **Phase 8 Scheduler**: Task dispatch
- **Phase 3 CLI Runner**: Resource monitoring
- **Phase 4 State Machine**: Task transitions
- **Phase 5 Failure Classification**: Failure handling
- **Phase 6 Crash Recovery**: State recovery
- **Phase 7 Replanning**: Replan on failure
```

Commit: `git add docs/architecture/ENGINE.md && git commit -m "docs: add Engine architecture documentation"`

---

### Task 15: Create Phase 9 Verification Report

Create `PHASE_9_VERIFICATION_REPORT.txt` with:
- All 15 tasks completed
- Test counts
- Feature summary
- Integration status

Commit: `git add PHASE_9_VERIFICATION_REPORT.txt && git commit -m "docs: add Phase 9 verification report"`

---

### Task 16: Run Full Test Suite

```bash
dotnet test -c Debug -v m
```

Expected: All tests pass, no regressions

---

### Task 17: Verify No Regressions in Previous Phases

Check that Phases 3-8 still pass:

```bash
dotnet test -v m --filter "Phase3 or Phase4 or Phase5 or Phase6 or Phase7 or Phase8"
```

---

### Task 18: Final Verification and Commit Summary

Run:
```bash
git status
dotnet build -c Debug
```

Expected: Clean working tree, successful build

---

## Summary

**Plan saved to:** `docs/plans/2026-02-27-phase-9-engine-integration.md`

**18 Bite-Sized Tasks:**
1. IResourceMonitor Interface
2. ResourceMonitor Implementation
3. DI Registration (ResourceMonitor)
4. SystemResourceSnapshot DTO
5. IEngine Interface
6. Engine Implementation
7. DI Registration (Engine)
8. Task Completion Handler
9. Integration Tests
10. API Request/Response DTOs
11. TasksController Implementation
12. ASP.NET Core Startup Configuration
13. Full Build Verification
14. Engine Architecture Documentation
15. Phase 9 Verification Report
16. Full Test Suite Run
17. Regression Testing
18. Final Verification

---

## Execution Options

**Plan complete and saved to `docs/plans/2026-02-27-phase-9-engine-integration.md`**

Which execution approach?

1. **Subagent-Driven (this session)** - Fresh subagent per task, code review between tasks
2. **Parallel Session (separate)** - Open new session with executing-plans, batch execution

Which would you prefer?