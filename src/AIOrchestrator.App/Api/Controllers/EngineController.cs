using AIOrchestrator.App.Api.RequestModels;
using AIOrchestrator.App.Api.ResponseModels;
using AIOrchestrator.App.Engine;
using Microsoft.AspNetCore.Mvc;

namespace AIOrchestrator.App.Api.Controllers;

[ApiController]
[Route("api/engine")]
public sealed class EngineController : ControllerBase
{
    private readonly EngineModeStore _engineModeStore;

    public EngineController(EngineModeStore engineModeStore)
    {
        _engineModeStore = engineModeStore;
    }

    [HttpPost("set-mode")]
    public ActionResult<SetExecutionModeResponse> SetMode([FromBody] SetExecutionModeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Mode))
        {
            return BadRequest(new { error = "Mode is required." });
        }

        if (!Enum.TryParse<ExecutionMode>(request.Mode, ignoreCase: true, out var mode))
        {
            return BadRequest(new { error = "Invalid mode. Allowed values: Safe, SemiAuto, FullAuto." });
        }

        _engineModeStore.SetMode(mode);
        return Ok(new SetExecutionModeResponse { Mode = mode.ToString() });
    }
}
