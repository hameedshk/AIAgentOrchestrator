namespace AIOrchestrator.Domain.Enums;

public enum TaskState
{
    Created, Queued, Planning, AwaitingPlanApproval,
    Executing, AwaitingUserFix, Retrying, Paused,
    Completed, Failed, Halted, Cancelled
}
