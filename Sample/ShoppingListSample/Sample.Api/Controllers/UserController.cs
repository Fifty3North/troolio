using Microsoft.AspNetCore.Mvc;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Client;

using Sample.Shared.Queries;
using Sample.Shared.ActorInterfaces;

namespace Sample.Api.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class UserController : BaseController
    {
        #region BoilerPlate ...
        public UserController(ITroolioClient troolioClient) : base(troolioClient) { }

        private async Task<IActionResult> ExecuteCommand(Metadata userHeaders, string actorId, TroolioPublicCommand<IUserActor> command)
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
        private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, Guid actorId, TroolioPublicCommand<IUserActor> command)
        {
            Metadata userHeaders = GetUserMetadata(userId, deviceId);

            return await ExecuteCommand(userHeaders, actorId.ToString(), command);
        }
        private async Task<IActionResult> ExecuteCommand(Guid userId, Guid deviceId, TroolioPublicCommand<IUserActor> command)
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
}