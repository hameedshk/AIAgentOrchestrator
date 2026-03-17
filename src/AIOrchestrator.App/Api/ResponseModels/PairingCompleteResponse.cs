namespace AIOrchestrator.App.Api.ResponseModels;

public sealed class PairingCompleteResponse
{
    public string DeviceName { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
}
