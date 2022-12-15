using Microsoft.AspNetCore.Mvc;
using Troolio.Core;

namespace ShoppingListSample.Api.Controllers;

[ApiController]
[Route("[Controller]")]
public class TelemetryController : Controller
{
    private ApiTracing _tracing { get; }

    public TelemetryController(ApiTracing tracing)
    {
        _tracing = tracing;
    }





    [HttpPost]
    [Route("disablelogging")]
    public async Task<IActionResult> DisableLogging()
    {
        try
        {
            await _tracing.DisableTracing();

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }

    [HttpPost]
    [Route("enablelogging")]
    public async Task<IActionResult> EnableLogging(Troolio.Core.TraceLevel? logLevel = null)
    {
        try
        {
            if (!logLevel.HasValue)
            {
                await _tracing.EnableTracing();
            }
            else
            {
                await _tracing.EnableTracing(logLevel.Value);
            }

            return Ok();
        }
        catch(Exception ex)
        {
            return BadRequest(ex);
        }
    }

    [HttpGet]
    [Route("flush")]
    public async Task<ActionResult<IList<MessageLog>>> Flush()
    {
        try
        {
            IList<MessageLog> messages = await _tracing.Flush();

            return Ok(messages);
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }
}
