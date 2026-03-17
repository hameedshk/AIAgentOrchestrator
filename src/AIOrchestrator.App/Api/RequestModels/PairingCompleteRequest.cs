namespace AIOrchestrator.App.Api.RequestModels;

public sealed class PairingCompleteRequest
{
    public string DeviceName { get; set; } = string.Empty;
    public string PairingToken { get; set; } = string.Empty;
}
