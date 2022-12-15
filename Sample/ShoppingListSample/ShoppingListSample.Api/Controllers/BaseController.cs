using Troolio.Core;
using Troolio.Core.Client;

namespace ShoppingListSample.Api.Controllers;

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
        return new Metadata(Guid.NewGuid(), userId, deviceId);
    }
}
