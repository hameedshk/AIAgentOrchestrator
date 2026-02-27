# Scheduler Architecture (Phase 8)

## Overview

The Scheduler manages task queue orchestration with priority-based dispatch, resource awareness, and crash-safe persistence for multi-project concurrent execution.

## Core Components

### 1. IScheduler Interface
Public contract for task scheduling:
- `EnqueueAsync(OrchestratorTask task)` - Add task to queue
- `DispatchAsync(cpuAvailable, memoryAvailableMb, maxProcesses)` - Get next eligible task
- `MarkRunningAsync(projectId)` - Mark project as executing
- `MarkCompleteAsync(projectId)` - Mark project as done

### 2. Scheduler (In-Memory Queue)
Base implementation with:
- Priority queue (High > Normal > Low)
- Priority aging (boost after 5 minutes)
- Per-project mutual exclusion
- Thread-safe operations

### 3. PersistentScheduler
Extends Scheduler with:
- Atomic state persistence
- Crash recovery
- Task queue recovery on startup

### 4. FileSystemSchedulerStateRepository
Atomic writes with temp-file pattern for crash safety.

## Priority Aging Algorithm

Tasks waiting > 5 minutes get priority boosted by one level:
- Low → Normal
- Normal → High

Prevents starvation of long-running low-priority tasks.

## Project Isolation

Enforces one active task per project:
- DispatchAsync skips tasks from running projects
- MarkRunningAsync records project as executing
- MarkCompleteAsync frees project

## State Persistence

SchedulerStateDto persists:
- Task queue (ordered list of task IDs)
- Running projects (set of project IDs)
- Last updated timestamp

Atomic writes use temp-file + replace pattern.

## Testing Strategy

- Unit tests: Priority ordering, mutual exclusion, aging
- Integration tests: Multi-project dispatch, project isolation
- Repository tests: Persistence layer atomicity

## Integration Points

- **Phase 3 (CLI Runner)**: Resource limits
- **Phase 4 (State Machine)**: Task.Enqueue() → Scheduler.EnqueueAsync()
- **Phase 6 (Recovery)**: Startup → Scheduler.LoadAsync()
- **Phase 9 (Engine)**: Engine loop → Scheduler.DispatchAsync()

## Dependency Injection

Register via `SchedulerServiceCollectionExtensions.AddScheduler()`:

```csharp
services.AddScheduler(schedulerStateDir);
```

Returns singleton `IScheduler` instance with loaded state.
