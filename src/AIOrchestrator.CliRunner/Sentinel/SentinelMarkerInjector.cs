namespace AIOrchestrator.CliRunner.Sentinel;

public static class SentinelMarkerInjector
{
    public static (string Prompt, string Marker) Inject(string originalPrompt)
    {
        string marker = $"__ORCHESTRATOR_DONE_{Guid.NewGuid():N}__";
        // Use & to separate commands (works in both cmd.exe and bash)
        return ($"{originalPrompt} & echo {marker}", marker);
    }
}
