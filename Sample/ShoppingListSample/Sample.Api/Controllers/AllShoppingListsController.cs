using Microsoft.AspNetCore.Mvc;
using Troolio.Core;
using Troolio.Core.Client;

using Sample.Shared.ShoppingList;

namespace Sample.Api.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class AllShoppingListsController : BaseController
    {
        #region BoilerPlate ...
        public AllShoppingListsController(ITroolioClient troolioClient) : base(troolioClient) { }

        private async Task<IActionResult> ExecuteCommand(Metadata userHeaders, string actorId, TroolioPublicCommand<IAllShoppingListsActor> command)
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
        private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, Guid actorId, TroolioPublicCommand<IAllShoppingListsActor> command)
        {
            Metadata userHeaders = GetUserMetadata(userId, deviceId);

            return await ExecuteCommand(userHeaders, actorId.ToString(), command);
        }
        private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, TroolioPublicCommand<IAllShoppingListsActor> command)
        {
            Metadata userHeaders = GetUserMetadata(userId, deviceId);

            return await ExecuteCommand(userHeaders, Constants.SingletonActorId, command);
        }
        #endregion



        [HttpPost]
        [Route("JoinListUsingCode")]
        public async Task<IActionResult> JoinListUsingCode([FromHeader] Guid userId, [FromHeader] Guid deviceId, [FromBody] JoinListUsingCodePayload payload)
        {
            return await ExecuteCommand(userId, deviceId, new JoinListUsingCode(new Metadata(Guid.NewGuid(), userId, deviceId), payload));
        }
    }
}
