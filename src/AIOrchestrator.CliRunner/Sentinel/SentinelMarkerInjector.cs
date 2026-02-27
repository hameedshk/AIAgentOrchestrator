namespace AIOrchestrator.CliRunner.Sentinel;

public static class SentinelMarkerInjector
{
    public static (string Prompt, string Marker) Inject(string originalPrompt)
    {
        string marker = $"__ORCHESTRATOR_DONE_{Guid.NewGuid():N}__";
        return ($"{originalPrompt}\necho {marker}", marker);
    }
}
