using Microsoft.AspNetCore.Mvc;
using Orleankka.Meta;
using Troolio.Core;
using Troolio.Core.Client;

using Sample.Shared.Queries;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Commands;

namespace Sample.Api.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class ShoppingListController : BaseController
    {
        #region BoilerPlate ...
        public ShoppingListController(ITroolioClient troolioClient) : base(troolioClient) { }

        private async Task<IActionResult> ExecuteCommand(Metadata userHeaders, string actorId, TroolioPublicCommand<IShoppingListActor> command)
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
        private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, Guid actorId, TroolioPublicCommand<IShoppingListActor> command)
        {
            Metadata userHeaders = GetUserMetadata(userId, deviceId);

            return await ExecuteCommand(userHeaders, actorId.ToString(), command);
        }
        private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, TroolioPublicCommand<IShoppingListActor> command)
        {
            Metadata userHeaders = GetUserMetadata(userId, deviceId);

            return await ExecuteCommand(userHeaders, Constants.SingletonActorId, command);
        }
        #endregion



        [HttpPost]
        [Route("{ShoppingListId}/AddItemToList")]
        public async Task<IActionResult> AddItemToList([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId, AddItemToListPayload payload)
        {
            return await ExecuteCommand(userId, deviceId, ShoppingListId, new AddItemToList(new Metadata(Guid.NewGuid(), userId, deviceId), payload));
        }
        [HttpPost]
        [Route("{ShoppingListId}/CreateNewList")]
        public async Task<IActionResult> CreateNewList([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId, CreateNewListPayload payload)
        {
            return await ExecuteCommand(userId, deviceId, ShoppingListId, new CreateNewList(new Metadata(Guid.NewGuid(), userId, deviceId),  payload));
        }
        [HttpPost]
        [Route("{ShoppingListId}/CrossItemOffList")]
        public async Task<IActionResult> CrossItemOffList([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId, CrossItemOffListPayload payload)
        {
            return await ExecuteCommand(userId, deviceId, ShoppingListId, new CrossItemOffList(new Metadata(Guid.NewGuid(), userId, deviceId), payload));
        }
        [HttpPost]
        [Route("{ShoppingListId}/RemoveItemFromList")]
        public async Task<IActionResult> RemoveItemFromList([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromRoute] Guid ShoppingListId, RemoveItemFromListPayload payload)
        {
            return await ExecuteCommand(userId, deviceId, ShoppingListId, new RemoveItemFromList(new Metadata(Guid.NewGuid(), userId, deviceId), payload));
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

    }
}
