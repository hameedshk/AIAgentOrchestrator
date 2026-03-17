namespace AIOrchestrator.App.Api.ResponseModels;

public sealed class PairingRequestResponse
{
    public string DeviceName { get; set; } = string.Empty;
    public string PairingToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
}
