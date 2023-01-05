using Microsoft.AspNetCore.Mvc;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Commands;
using ShoppingList.Shared.WriteModels;
using ShoppingList.Shared.Queries;
using ShoppingList.Shared.ReadModels;
using Troolio.Core;
using Troolio.Core.Client;

namespace ShoppingList.Api.Controllers;

[ApiController]
[Route("[Controller]")]
public class ShoppingListController : BaseController
{
    #region Helpers ...

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="troolioClient">injected actor system client</param>
    public ShoppingListController(ITroolioClient troolioClient) : base(troolioClient) 
    { 
    }

    /// <summary>
    /// Command execution giver full headers and a specific actor, this is intended for internal use.
    /// </summary>
    /// <param name="userHeaders">Metadata object, collation id needs to be unique per command</param>
    /// <param name="actorId">The id of the actor to execute the command against</param>
    /// <param name="command">The command to be executed using the metadata passed in the call</param>
    /// <returns></returns>
    private async Task<IActionResult> ExecuteCommand(Metadata userHeaders, string actorId, Command<IShoppingListActor> command)
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
    private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, Guid actorId, Command<IShoppingListActor> command)
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
    private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, Command<IShoppingListActor> command)
    {
        Metadata userHeaders = GetUserMetadata(userId, deviceId);

        return await ExecuteCommand(userHeaders, Constants.SingletonActorId, command);
    }

    #endregion

    [HttpPost]
    [Route("ping")]
    public async Task<IActionResult> Ping([FromHeader] Guid userId, [FromHeader] Guid deviceId)
    {
        await _troolioClient.Tell(Guid.NewGuid().ToString(), new Ping(new Metadata(Guid.NewGuid(), userId, deviceId)));
        return Ok();
    }


    [HttpPost]
    [Route("{ShoppingListId}/AddItemToList")]
    public async Task<IActionResult> AddItemToList([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId, ItemToAdd payload)
    {
        return await ExecuteCommand(userId, deviceId, ShoppingListId, new AddItemToList(new Metadata(Guid.NewGuid(), userId, deviceId), userId, payload.ItemId, payload.Description, payload.Quantity));
    }

    [HttpPost]
    [Route("{ShoppingListId}/CreateNewList")]
    public async Task<IActionResult> CreateNewList([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId, [FromBody] string title)
    {
        return await ExecuteCommand(userId, deviceId, ShoppingListId, new CreateNewList(new Metadata(Guid.NewGuid(), userId, deviceId), userId,  title));
    }

    [HttpPost]
    [Route("{ShoppingListId}/CrossItemOffList")]
    public async Task<IActionResult> CrossItemOffList([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId, [FromBody] Guid ItemId)
    {
        return await ExecuteCommand(userId, deviceId, ShoppingListId, new CrossItemOffList(new Metadata(Guid.NewGuid(), userId, deviceId), userId, ItemId));
    }

    [HttpPost]
    [Route("{ShoppingListId}/RemoveItemFromList")]
    public async Task<IActionResult> RemoveItemFromList([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId, Guid ItemId)
    {
        return await ExecuteCommand(userId, deviceId, ShoppingListId, new RemoveItemFromList(new Metadata(Guid.NewGuid(), userId, deviceId), userId, ItemId));
    }

    [HttpGet]
    [Route("{ShoppingListId}/ShoppingListDetails")]
    public async Task<IActionResult> ShoppingListDetails([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId)
    {
        ShoppingListQueryResult result;

        try
        {
            result = await _troolioClient.Ask(ShoppingListId.ToString(), new ShoppingListDetails());
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        return Ok(result);
    }

    [HttpGet]
    [Route("{ShoppingListId}/ShoppingListReadModel")]
    public async Task<ActionResult<ShoppingListReadModel>> ShoppingListReadModel([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId)
    {
        ShoppingListReadModel result;

        try
        {
            result = await _troolioClient.Get<ShoppingListReadModel>(ShoppingListId.ToString());

            if (result.Title == null)
            {
                throw new ApplicationException("Shopping list has not been created");
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        if (result.Authorized(new Metadata(Guid.Empty, userId, deviceId)))
        {
            return Ok(result);
        } 
        else
        {
            return Unauthorized();
        }
    }
}
