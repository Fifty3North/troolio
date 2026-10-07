using Microsoft.AspNetCore.Mvc;
using Troolio.Core;

namespace Sample.Api.Controllers;

[ApiController]
[Route("Telemetry")]
public sealed class TelemetryController(ApiTracing tracing, ILogger<TelemetryController> logger) : ControllerBase
{
    [HttpPost("disablelogging")]
    public async Task<IActionResult> DisableLogging()
    {
        try { await tracing.DisableTracing(); return Ok(); }
        catch (Exception error) { return Failure(error); }
    }
    [HttpPost("enablelogging")]
    public async Task<IActionResult> EnableLogging(TraceLevel? logLevel = null)
    {
        try { await tracing.EnableTracing(logLevel ?? TraceLevel.Error); return Ok(); }
        catch (Exception error) { return Failure(error); }
    }
    [HttpGet("flush")]
    public async Task<ActionResult<IList<MessageLog>>> Flush()
    {
        try { return Ok(await tracing.Flush()); }
        catch (Exception error) { return Failure(error); }
    }
    private ObjectResult Failure(Exception error)
    {
        logger.LogError(error, "The tracing operation failed.");
        return Problem(title: "Tracing is temporarily unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
