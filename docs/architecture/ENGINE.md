# Engine Architecture (Phase 9)

## Overview

The Engine is the central orchestration hub that coordinates task dispatch, resource monitoring, and multi-project execution.

## Core Components

### IResourceMonitor
Monitors system resources (CPU, memory, process counts) for dispatch decisions.

### ResourceMonitor
Implements IResourceMonitor using System.Diagnostics performance counters.

### IEngine
Public contract for task submission, dispatch loop, and status queries.

### Engine
Central orchestrator that:
- Runs continuous dispatch loop
- Monitors resources every 1 second
- Dispatches tasks from Scheduler based on resource availability
- Tracks task lifecycle
- Manages project execution with mutual exclusion

## Execution Flow

1. Submit task via API → Engine.SubmitTaskAsync()
2. Task → Queued state
3. Engine loop polls Scheduler every 1 second
4. Check resource availability
5. Resources available? → Dispatch task
6. Task → Executing state
7. Engine marks project running
8. Task completes or fails
9. Engine frees project resources

## Resource Thresholds

- CPU Threshold: 80%
- Memory Threshold: 512 MB available
- Process Threshold: Max - Current running

## API Endpoints

- `POST /api/tasks` - Submit task
- `GET /api/tasks?state=Queued` - List tasks by state
- `GET /api/tasks/{id}` - Get specific task
- `GET /api/tasks/status` - Engine status

## Integration Points

- **Phase 8 Scheduler**: Task dispatch and project isolation
- **Phase 3 CLI Runner**: Resource monitoring
- **Phase 4 State Machine**: Task transitions
- **Phase 5 Failure Classification**: Failure handling
- **Phase 6 Crash Recovery**: State recovery
- **Phase 7 Replanning**: Replan on failure
