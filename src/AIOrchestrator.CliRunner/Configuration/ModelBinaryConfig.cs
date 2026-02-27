namespace AIOrchestrator.CliRunner.Configuration;

public sealed class ModelBinaryConfig
{
    public string ModelName { get; init; } = string.Empty;
    public string BinaryPath { get; init; } = string.Empty;
    public string[] DefaultArgs { get; init; } = [];
    public int SilenceTimeoutSeconds { get; init; } = 120;
}
