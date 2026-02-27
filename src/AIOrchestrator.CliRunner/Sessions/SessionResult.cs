namespace AIOrchestrator.CliRunner.Sessions;

public sealed record SessionResult(string Output, int ExitCode, bool TimedOut, bool SentinelDetected)
{
    public bool IsSuccess => SentinelDetected && !TimedOut;
}
