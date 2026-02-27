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
