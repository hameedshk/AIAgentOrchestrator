using AIOrchestrator.App.Logging;
using Microsoft.AspNetCore.Mvc;

namespace AIOrchestrator.App.Api.Controllers;

[ApiController]
[Route("api/logs/audit")]
public sealed class AuditController : ControllerBase
{
    private readonly AuditLogger _auditLogger;

    public AuditController(AuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    [HttpGet("stream")]
    public async Task<ActionResult<IReadOnlyList<AuditLogEntry>>> Stream([FromQuery] int limit = 100)
    {
        var boundedLimit = Math.Clamp(limit, 1, 500);
        var entries = new List<AuditLogEntry>();

        await foreach (var entry in _auditLogger.StreamEntriesAsync(boundedLimit))
        {
            entries.Add(entry);
        }

        return Ok(entries);
    }
}
