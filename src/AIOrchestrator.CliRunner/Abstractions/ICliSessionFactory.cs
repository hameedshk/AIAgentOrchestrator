namespace AIOrchestrator.CliRunner.Abstractions;

public interface ICliSessionFactory
{
    ICliSession Create(string modelName);
}
