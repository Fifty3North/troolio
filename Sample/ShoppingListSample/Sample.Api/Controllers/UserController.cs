using Microsoft.AspNetCore.Mvc;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Queries;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Client;

namespace Sample.Api.Controllers;

[ApiController]
[Route("[Controller]")]
public class UserController : BaseController
{
    #region Helpers ...

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="troolioClient">injected actor system client</param>
    public UserController(ITroolioClient troolioClient) : base(troolioClient) { }

    /// <summary>
    /// Command execution giver full headers and a specific actor, this is intended for internal use.
    /// </summary>
    /// <param name="userHeaders">Metadata object, collation id needs to be unique per command</param>
    /// <param name="actorId">The id of the actor to execute the command against</param>
    /// <param name="command">The command to be executed using the metadata passed in the call</param>
    /// <returns></returns>
    private async Task<IActionResult> ExecuteCommand(Metadata userHeaders, string actorId, Command<IUserActor> command)
    {
        try
        {
            await _troolioClient.Tell(actorId, command with { Headers = userHeaders });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok();
    }

    /// <summary>
    /// Command execution given a user id, device id and a specific actor
    /// </summary>
    /// <param name="userId">The unique Id of the user</param>
    /// <param name="deviceId">The unique Id of the device</param>
    /// <param name="actorId">The id of the actor to execute the command against</param>
    /// <param name="command">The command to be executed using the metadata generated from the passed in user details</param>
    /// <returns></returns>
    private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, Guid actorId, Command<IUserActor> command)
    {
        Metadata userHeaders = GetUserMetadata(userId, deviceId);

        return await ExecuteCommand(userHeaders, actorId.ToString(), command);
    }

    /// <summary>
    /// Command execution given a user id, device id.  The command will be executed against a singleton for use in collections
    /// </summary>
    /// <param name="userId">The unique Id of the user</param>
    /// <param name="deviceId">The unique Id of the device</param>
    /// <param name="command">The command to be executed using the metadata generated from the passed in user details</param>
    /// <returns></returns>
    private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, Command<IUserActor> command)
    {
        Metadata userHeaders = GetUserMetadata(userId, deviceId);

        return await ExecuteCommand(userHeaders, Constants.SingletonActorId, command);
    }

    #endregion



    [HttpGet]
    [Route("{UserId}/MyShoppingLists")]
    public async Task<IActionResult> MyShoppingLists([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid UserId)
    {
        ImmutableList<ShoppingListQueryResult> result;

        try
        {
            result = await _troolioClient.Ask(UserId.ToString(), new MyShoppingLists());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok(result);
    }

}