namespace AIOrchestrator.CliRunner.Configuration;

public sealed class CliRunnerOptions
{
    public const string SectionName = "CliRunner";

    public List<ModelBinaryConfig> Models { get; init; } = [];
    public int DefaultSilenceTimeoutSeconds { get; init; } = 120;

    public string GetBinaryPath(string modelName)
    {
        var model = Models.FirstOrDefault(m =>
            m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No CLI binary configured for model '{modelName}'.");
        return model.BinaryPath;
    }

    public int GetSilenceTimeout(string modelName)
    {
        var model = Models.FirstOrDefault(m =>
            m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        return model?.SilenceTimeoutSeconds ?? DefaultSilenceTimeoutSeconds;
    }
}
