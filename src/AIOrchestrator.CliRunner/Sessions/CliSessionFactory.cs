using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.CliRunner.Configuration;

namespace AIOrchestrator.CliRunner.Sessions;

public sealed class CliSessionFactory(CliRunnerOptions options) : ICliSessionFactory
{
    public ICliSession Create(string modelName)
    {
        var binaryPath = options.GetBinaryPath(modelName);
        var silenceTimeout = options.GetSilenceTimeout(modelName);

        var config = options.Models.FirstOrDefault(m =>
            m.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Model '{modelName}' not found in configuration.");

        return new CliSession(config);
    }
}
