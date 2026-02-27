using System.Text.RegularExpressions;

namespace AIOrchestrator.Planner.Extraction;

public static class JsonBlockExtractor
{
    private static readonly Regex FencePattern =
        new(@"```json\s*\n([\s\S]*?)\n```", RegexOptions.Compiled);

    public static string? Extract(string rawOutput)
    {
        // 1. Try fenced block
        var fenceMatch = FencePattern.Match(rawOutput);
        if (fenceMatch.Success)
            return fenceMatch.Groups[1].Value.Trim();

        // 2. Fall back: find first '{' and last '}' that contain valid JSON structure
        var start = rawOutput.IndexOf('{');
        var end = rawOutput.LastIndexOf('}');
        if (start >= 0 && end > start)
            return rawOutput[start..(end + 1)];

        return null;
    }
}
