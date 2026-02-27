# Crash Recovery and Resilience (Phase 6)

## Overview

Phase 6 implements deterministic crash recovery ensuring zero task loss across any engine crash scenario. The system automatically detects restarts, recovers interrupted tasks from their last successful checkpoint, prevents restart spirals via safe mode, and provides structured diagnostics.

## Core Guarantee

**Zero Task Loss:** Any task interrupted mid-execution recovers cleanly without data loss or duplicate execution, with automatic restart detection and safe mode entry on restart spirals.

## Components

### 1. ICrashLoopDetector / CrashLoopDetector

**Purpose:** Prevents engine restart spirals that would corrupt state.

**Behavior:**
- Tracks restart timestamps in persistent JSON file
- Safe mode triggered if >3 restarts within 5-minute window (Spec 9.4)
- Time window mechanism enables clean restart after issues resolve
- Automatically persists restart history across engine restarts

**Key Method:**
- `RecordRestart()` — Log restart with timestamp
- `ShouldEnterSafeMode()` — Check if >3 restarts in window
- `ResetCounter()` — Clear on clean shutdown

**Test Coverage:** 5 unit tests covering boundaries, persistence, reset

### 2. ITaskRecoveryCoordinator / TaskRecoveryCoordinator

**Purpose:** Restores individual tasks from crash state to executable condition.

**Behavior (Spec 9.3):**
1. Identify tasks in Executing/Planning state
2. Reset in-progress step to Pending (was Running)
3. Set task state back to Executing
4. Set CurrentStepIndex to next uncompleted step
5. Git restoration via Phase 4 rehydration protocol
6. CLI session rehydration via rehydration prompt

**Key Method:**
- `IdentifyRecoveringTasks()` — Filter Executing/Planning tasks
- `RecoverTaskAsync()` — Reset and prepare task for retry

**Test Coverage:** 5 unit tests covering state transitions, step reset, no-completed-steps edge case

### 3. IRecoveryEventLogger / RecoveryEventLogger

**Purpose:** Structured logging of recovery events with model identity tracking (Spec 15).

**Behavior:**
- Append-only JSON logs to `logs/system.log`
- Every entry includes `modelId` field for post-hoc analysis
- Events: EngineStartup, TaskRecovered, SafeModeEntered, CleanShutdown
- Model identity enables per-model failure rate analysis

**Log Entry Format:**
```json
{
  "timestamp": "2026-02-27T...",
  "eventType": "TaskRecovered",
  "taskId": "...",
  "taskTitle": "...",
  "recoveredToStepIndex": 2,
  "plannerModel": "Claude",
  "executorModel": "Codex",
  "modelId": "Codex"
}
```

**Test Coverage:** Integration tests verify event logging with model identity

### 4. ICrashRecoveryManager / CrashRecoveryManager

**Purpose:** Orchestrates the complete recovery flow on engine startup (Spec 9.3).

**Recovery Flow:**
1. Record restart with crash loop detector
2. Check if safe mode should be entered
3. If safe mode: emit event, return 0 (don't resume)
4. If normal: load all persisted tasks
5. Identify tasks in Executing/Planning state
6. For each recovering task:
   - Reset to last completed step
   - Persist recovered state
   - Log with model identities
7. Return count of recovered tasks

**Integration:**
- Works with Phase 1 ITaskRepository for task persistence
- Coordinates Phase 4 rehydration protocol for CLI restart
- Logs via Phase 5 failure classification context
- Called during engine startup before accepting new tasks

**Test Coverage:** 4 integration tests covering recovery, safe mode, logging, counter reset

## Atomic Persistence (Spec 9.2)

All state writes follow atomic protocol:
```csharp
string tempPath = path + ".tmp";
File.WriteAllText(tempPath, json);  // Write to temp
File.Replace(tempPath, path, null);  // Atomic replace
```

Benefits:
- No partial writes on crash
- Recovery always has valid state
- Crash during transition leaves previous state intact

## Safe Mode (Spec 9.4)

**Trigger:** >3 engine restarts within 5 minutes

**Behavior:**
- Engine loads and starts API server
- No tasks auto-resume (operator inspection required)
- Prevents runaway restart spiral corrupting state
- Restart counter resets on clean shutdown

**Example Scenario:**
1. Restart 1: task crashes engine
2. Restart 2: same task crashes again
3. Restart 3: third crash
4. Restart 4: safe mode entered, awaiting operator
5. Operator investigates, fixes root cause, manually resumes

## Integration Points

**Phase 1 (Persistence):** Loads/saves task state via ITaskRepository

**Phase 4 (CLI Sessions):** Rehydration protocol restarts CLI with context
- `RecoverTaskAsync()` prepares state
- `CliSessionManager` sends rehydration prompt
- Executor resumes from next uncompleted step

**Phase 5 (Failure Classification):** Failure context available during recovery logging

**Phase 8 (Scheduler):** On recovery, re-enqueues recovered tasks for normal dispatch

## Testing Strategy

**Unit Tests (9 cases):**
- CrashLoopDetector: boundary conditions, persistence, reset
- TaskRecoveryCoordinator: state transitions, step reset, edge cases
- Both test with temp directories, cleanup after each test

**Integration Tests (4 cases):**
- End-to-end recovery flow
- Safe mode entry preventing task resumption
- Event logging with model identity
- Counter reset on clean shutdown
- All use NSubstitute mocks for ITaskRepository

**Build & Regression (1 task):**
- Full solution build with zero warnings
- All tests across all phases pass
- No regressions in existing functionality

**Total: 14 test cases**

## Non-Functional Guarantees

✅ Zero task loss across any crash scenario
✅ Deterministic recovery given identical persisted state
✅ Safe mode prevents restart spirals
✅ Model identity logged for all recovery events
✅ Atomic writes ensure no partial state
✅ Time-windowed restart detection allows clean restart after fixes

## Configuration (Future)

From orchestrator.config.json (implemented in later phases):
```json
{
  "crashRecovery": {
    "maxRestartsBeforeSafeMode": 3,
    "timeWindowMinutes": 5,
    "dataDirectory": "data/"
  }
}
```
