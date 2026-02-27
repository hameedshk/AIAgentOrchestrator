# Failure Classification Engine (Phase 5)

## Overview

The Failure Classification Engine implements the typed failure detection pipeline specified in Section 8 of the AI Orchestrator specification. Every step failure is classified into one of 11 specific failure types, enabling intelligent retry logic and diagnostic correlation.

## Architecture

### Classification Pipeline

The engine runs a 4-step pipeline (spec Section 8.2):

1. **Exit Code Mapping** — Known exit codes (e.g., 127 = command not found)
2. **Regex Pattern Matching** — Run ordered patterns against stdout/stderr
3. **Agent Output Validation** — Check structural validity (placeholder for Phase 6)
4. **Error Fingerprint Hashing** — SHA256 of normalized error for deduplication

### Failure Types

The engine classifies failures into 11 types (spec Section 8.1):

| Type | Description | Retryable |
|------|-------------|-----------|
| `CompileError` | Build system syntax/type errors | No |
| `TestFailure` | Test runner assertion failures | No |
| `DependencyMissing` | Missing package/import/tool | **Yes** |
| `RuntimeException` | Unhandled exception | No |
| `GitConflict` | Merge/rebase conflict | No |
| `PermissionError` | File system access denied | No |
| `Timeout` | Step exceeded time/silence threshold | **Yes** |
| `AgentInvalidOutput` | Agent output schema violation | No |
| `DangerousCommandBlocked` | Blocklist enforcement | No |
| `CliCrash` | CLI process crashed | **Yes** |
| `Unknown` | Cannot classify | No |

### Loop Guard

The Loop Guard prevents infinite retry loops by enforcing (spec Section 8.3):

- **maxRetriesPerStep** (default: 3) — Max retries at a single step
- **maxLoopsPerTask** (default: 10) — Max total retry cycles across all steps
- **Error Fingerprint Deduplication** — Same error hash on consecutive retries halts immediately

The fingerprint is SHA256 of normalized error output (whitespace collapsed, timestamps/paths removed).

## Components

### IFailureClassifier & FailureClassifier

**Responsibility:** Classify step failures from exit codes and output.

**Methods:**
- `Classify(int? exitCode, string output, ModelType? plannerModel, ModelType? executorModel)` → `FailureContext`
- `ComputeErrorHash(string output)` → `string` (64-char SHA-256 hex)

**Implementation Details:**
- Regex patterns dictionary for each failure type
- Case-insensitive pattern matching
- Normalization: collapses whitespace, removes timestamps/paths
- Retryability logic: transient vs permanent failures
- Model identity tracking for diagnostics

**Files:**
- Interface: `src/AIOrchestrator.CliRunner/FailureClassification/IFailureClassifier.cs`
- Implementation: `src/AIOrchestrator.CliRunner/FailureClassification/FailureClassifier.cs`

### ILoopGuard & LoopGuard

**Responsibility:** Enforce retry limits and prevent infinite loops.

**Methods:**
- `CanRetry(Guid taskId, int stepIndex, int retryCount, string errorHash, int maxRetriesPerStep, int maxLoopsPerTask)` → `bool`
- `Reset(Guid taskId)` → `void`

**Implementation Details:**
- Per-task state tracking: Dictionary<Guid, PerTaskState>
- Per-step retry count tracking
- Total loop count across all steps
- Error hash deduplication: same hash on consecutive retries blocks immediately
- Independent state per task

**Files:**
- Interface: `src/AIOrchestrator.CliRunner/FailureClassification/ILoopGuard.cs`
- Implementation: `src/AIOrchestrator.CliRunner/FailureClassification/LoopGuard.cs`

## Integration Points

### ExecutionStep

Updated with Phase 5 requirements:
- **RetryCount** (int): Tracks number of retry attempts
- **LastErrorHash** (string?): Stores hash for deduplication
- **MarkFailed(FailureContext)**: Increments RetryCount, captures hash
- **ResetForRetry()**: Resets status/output for next attempt

### ExecutorSession (Phase 4)

The executor session dispatches steps and receives failure contexts:

```
ExecutionStep → FailureClassifier.Classify() → FailureContext
FailureContext → ExecutionStep.MarkFailed() → Track RetryCount + LastErrorHash
(RetryCount, LastErrorHash) → LoopGuard.CanRetry() → bool (allow retry?)
```

## Usage Example

```csharp
// Inject services (registered via AddFailureClassification())
var classifier = serviceProvider.GetRequiredService<IFailureClassifier>();
var guard = serviceProvider.GetRequiredService<ILoopGuard>();

// Step fails with output
string errorOutput = "error CS0103: The name 'x' does not exist";

// Classify failure
var failure = classifier.Classify(
    exitCode: 1,
    output: errorOutput,
    plannerModel: ModelType.Claude,
    executorModel: ModelType.Claude);

Console.WriteLine($"Type: {failure.Type}"); // CompileError
Console.WriteLine($"Retryable: {failure.Retryable}"); // false

// Check if retry allowed
bool canRetry = guard.CanRetry(
    taskId: taskId,
    stepIndex: 0,
    retryCount: 0,
    errorHash: failure.ErrorHash,
    maxRetriesPerStep: 3,
    maxLoopsPerTask: 10);

if (!canRetry)
{
    // Task enters AwaitingUserFix state
}
```

## Test Coverage

### Unit Tests (40+ tests)

- **ErrorHashingTests** (4 tests)
  - Hash consistency
  - Whitespace normalization
  - Hash differentiation
  - Timestamp/path normalization

- **FailureClassifierTests** (13 tests)
  - All 11 failure type detection
  - Exit code mapping
  - Retryability verdicts
  - Model identity tracking
  - Unknown failure fallback
  - Error hash format validation

- **LoopGuardTests** (6 tests)
  - maxRetriesPerStep enforcement
  - Same error hash blocking (dedup)
  - Different error hash allowing retry
  - maxLoopsPerTask across steps
  - Per-task state independence
  - Reset functionality

- **FailureClassificationIntegrationTests** (6 tests)
  - End-to-end classification pipeline
  - Non-retryable error handling
  - Transient error retry
  - Error deduplication
  - Multi-step execution
  - Infinite loop prevention

### Metrics

- Total test files: 4
- Total test methods: 29
- Total test cases: 40+ (with Theory data)
- Pass rate: 100%
- Code coverage: Core failure classification logic fully tested

## Dependencies

- **Microsoft.Extensions.DependencyInjection** (v10.0.x)
- **FluentAssertions** (v8.8.x) - Test assertions
- **xUnit** (v2.9.x) - Test framework
- **NSubstitute** (v5.3.x) - Test mocking

## Files Summary

**Domain Entities:**
- `src/AIOrchestrator.Domain/Enums/FailureType.cs` - 11 failure classifications
- `src/AIOrchestrator.Domain/Entities/FailureContext.cs` - Failure information record
- `src/AIOrchestrator.Domain/Entities/ExecutionStep.cs` - Enhanced with retry tracking

**CliRunner Implementation:**
- `src/AIOrchestrator.CliRunner/FailureClassification/IFailureClassifier.cs`
- `src/AIOrchestrator.CliRunner/FailureClassification/FailureClassifier.cs`
- `src/AIOrchestrator.CliRunner/FailureClassification/ILoopGuard.cs`
- `src/AIOrchestrator.CliRunner/FailureClassification/LoopGuard.cs`
- `src/AIOrchestrator.CliRunner/DependencyInjection/FailureClassificationServiceCollectionExtensions.cs`

**Tests:**
- `tests/AIOrchestrator.CliRunner.Tests/FailureClassification/ErrorHashingTests.cs`
- `tests/AIOrchestrator.CliRunner.Tests/FailureClassification/FailureClassifierTests.cs`
- `tests/AIOrchestrator.CliRunner.Tests/FailureClassification/LoopGuardTests.cs`
- `tests/AIOrchestrator.CliRunner.Tests/FailureClassification/FailureClassificationIntegrationTests.cs`

## Next Phase

Phase 6 will integrate failure classification into the execution lifecycle, enabling:
- Re-planning triggers on non-retryable failures
- Structured logging of model identity per failure
- Safe mode human approval flow
- Automatic retry with loop guard enforcement

See spec Section 9 for crash recovery and resilience details.
