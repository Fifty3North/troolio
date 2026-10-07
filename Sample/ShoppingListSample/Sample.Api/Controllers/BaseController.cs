using System.Security.Claims;
using Troolio.Core;
using Troolio.Core.Client;

namespace Sample.Api.Controllers;

public abstract class BaseController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    protected readonly ITroolioClient _troolioClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="troolioClient">injected actor system client</param>
    public BaseController(ITroolioClient troolioClient)
    {
        _troolioClient = troolioClient;
    }

    /// <summary>
    /// Helper to generate a Metadata object for use with the command
    /// </summary>
    /// <param name="userId">The unique Id of the user</param>
    /// <param name="deviceId">The unique Id of the device</param>
    /// <returns>User metadata object with a unique correlation id</returns>
    protected Metadata GetUserMetadata(Guid userId, Guid deviceId)
    {
        var verifiedUser = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var correlationId = Guid.TryParse(Request.Headers["correlationId"], out var suppliedCorrelation)
            && suppliedCorrelation != Guid.Empty ? suppliedCorrelation : Guid.NewGuid();
        if (deviceId == Guid.Empty && Guid.TryParse(Request.Headers["deviceId"], out var suppliedDevice))
            deviceId = suppliedDevice;
        Response.Headers["correlationId"] = correlationId.ToString();
        return new Metadata(correlationId, verifiedUser, deviceId);
    }
}
