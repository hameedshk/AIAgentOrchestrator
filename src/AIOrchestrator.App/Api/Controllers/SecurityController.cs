using System.Security.Cryptography;
using AIOrchestrator.App.Api.RequestModels;
using AIOrchestrator.App.Api.ResponseModels;
using AIOrchestrator.App.Security;
using Microsoft.AspNetCore.Mvc;

namespace AIOrchestrator.App.Api.Controllers;

[ApiController]
[Route("api/security")]
public sealed class SecurityController : ControllerBase
{
    private readonly DevicePairingService _devicePairingService;
    private readonly ITokenStore _tokenStore;

    public SecurityController(DevicePairingService devicePairingService, ITokenStore tokenStore)
    {
        _devicePairingService = devicePairingService;
        _tokenStore = tokenStore;
    }

    [HttpPost("pairing/request")]
    public ActionResult<PairingRequestResponse> RequestPairing([FromBody] PairingRequestRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DeviceName))
            return BadRequest(new { error = "DeviceName is required." });

        var deviceName = request.DeviceName.Trim();
        var pairingToken = _devicePairingService.GeneratePairingToken();
        _devicePairingService.StorePairingToken(pairingToken, deviceName);

        return Ok(new PairingRequestResponse
        {
            DeviceName = deviceName,
            PairingToken = pairingToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_devicePairingService.TokenExpirationMinutes)
        });
    }

    [HttpPost("pairing/complete")]
    public ActionResult<PairingCompleteResponse> CompletePairing([FromBody] PairingCompleteRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.DeviceName) ||
            string.IsNullOrWhiteSpace(request.PairingToken))
            return BadRequest(new { error = "DeviceName and PairingToken are required." });

        var deviceName = request.DeviceName.Trim();
        var token = request.PairingToken.Trim();
        if (!_devicePairingService.ValidatePairingToken(token, deviceName))
            return Unauthorized(new { error = "Invalid or expired pairing token." });

        var bearerToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        _tokenStore.StoreToken(bearerToken, deviceName);

        return Ok(new PairingCompleteResponse
        {
            DeviceName = deviceName,
            BearerToken = bearerToken
        });
    }
}
