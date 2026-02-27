using System.Text.Json;
using System.Text.Json.Serialization;
using AIOrchestrator.CliRunner.Abstractions;
using AIOrchestrator.Planner.Abstractions;
using AIOrchestrator.Planner.Contract;
using AIOrchestrator.Planner.Extraction;

namespace AIOrchestrator.Planner.Sessions;

public sealed class PlannerSession(ICliSessionFactory factory, PlannerSessionOptions options)
    : IPlannerSession
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<PlanOutputContract> PlanAsync(Guid taskId, string taskDescription,
                                                     CancellationToken ct = default)
    {
        var validator = new PlanContractValidator();
        var attempts = 0;
        var maxAttempts = options.MaxPlannerRetries + 1;

        while (attempts < maxAttempts)
        {
            attempts++;

            try
            {
                // Create session and execute
                await using var session = factory.Create(options.ModelName);
                var prompt = BuildPrompt(taskId, taskDescription);
                var result = await session.ExecuteAsync(prompt, ct);

                // Extract JSON from output
                var json = JsonBlockExtractor.Extract(result.Output);
                if (json == null)
                {
                    // No JSON found, retry
                    if (attempts >= maxAttempts)
                        throw new PlannerOutputException(
                            $"Could not extract JSON from planner output after {attempts} attempts.",
                            attempts);
                    continue;
                }

                // Deserialize
                PlanOutputContract? contract;
                try
                {
                    contract = JsonSerializer.Deserialize<PlanOutputContract>(json, JsonOptions);
                }
                catch (JsonException ex)
                {
                    // Deserialization failed, retry
                    if (attempts >= maxAttempts)
                        throw new PlannerOutputException(
                            $"Failed to deserialize plan JSON: {ex.Message}",
                            attempts);
                    continue;
                }

                if (contract == null)
                {
                    if (attempts >= maxAttempts)
                        throw new PlannerOutputException(
                            "Deserialized contract was null.",
                            attempts);
                    continue;
                }

                // Validate contract
                var validationResult = validator.Validate(contract);
                if (!validationResult.IsValid)
                {
                    if (attempts >= maxAttempts)
                        throw new PlannerOutputException(
                            $"Plan contract validation failed: {string.Join(", ", validationResult.Errors)}",
                            attempts);
                    continue;
                }

                // Success
                return contract;
            }
            catch (PlannerOutputException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempts >= maxAttempts)
                    throw new PlannerOutputException(
                        $"Planner session failed: {ex.Message}",
                        attempts);
            }
        }

        throw new PlannerOutputException(
            $"Planner failed to produce valid output after {maxAttempts} attempts.",
            maxAttempts);
    }

    private static string BuildPrompt(Guid taskId, string taskDescription)
    {
        return $$"""
            You are a planning agent. Your task is to create a detailed execution plan.

            Task ID: {{taskId}}
            Task Description: {{taskDescription}}

            Respond with ONLY a JSON object in this exact format:
            {
              "planVersion": "1",
              "taskId": "{{taskId}}",
              "steps": [
                {
                  "index": 0,
                  "type": "Shell" or "Agent",
                  "description": "brief description",
                  "command": "for Shell steps",
                  "prompt": "for Agent steps",
                  "expectedOutput": "optional expected output"
                }
              ]
            }

            Do not include any other text. Output ONLY the JSON.
            """;
    }
}
