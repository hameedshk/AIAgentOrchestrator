namespace AIOrchestrator.CliRunner.Sentinel;

public sealed class SentinelDetector(string marker)
{
    public bool Detected { get; private set; }

    public bool CheckLine(string line)
    {
        if (line.Contains(marker))
        {
            Detected = true;
            return true;
        }
        return false;
    }
}
