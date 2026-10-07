using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sample.Database.Model;
using Sample.Shared.Queries;
using Troolio.Core.Client;

namespace Sample.Api.Controllers;

[ApiController]
[Route("User")]
public sealed class UserController(ITroolioClient client, ShoppingListsDbContext db) : BaseController(client)
{
    [HttpGet("{UserId:guid}/MyShoppingLists")]
    public async Task<IActionResult> MyShoppingLists(Guid userId, Guid deviceId, [FromRoute] Guid UserId,
        [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var caller = GetUserMetadata(userId, deviceId).UserId;
        if (caller != UserId) return Forbid();
        if (skip < 0 || take < 1 || take > 100) return BadRequest("Use skip >= 0 and take between 1 and 100.");
        var lists = await db.ShoppingLists.AsNoTracking().Where(list => db.ShoppingListMembers.Any(member => member.UserId == caller && member.ShoppingListId == list.Id))
            .OrderBy(list => list.Title).ThenBy(list => list.Id).Skip(skip).Take(take).ToArrayAsync();
        var ids = lists.Select(x => x.Id).ToArray();
        var items = await db.ShoppingListItems.AsNoTracking().Where(x => ids.Contains(x.ShoppingListId)).ToArrayAsync();
        var members = await db.ShoppingListMembers.AsNoTracking().Where(x => ids.Contains(x.ShoppingListId)).ToArrayAsync();
        return Ok(lists.Select(list => new ShoppingListQueryResult(list.Id, list.Title,
            items.Where(x => x.ShoppingListId == list.Id).Select(x => new ShoppingItemQueryItem(x.Id, x.Name, x.Status, x.Quantity)).ToArray(),
            members.Where(x => x.ShoppingListId == list.Id && x.UserId != list.AuthorId).Select(x => x.UserId).ToImmutableList(), list.AuthorId == caller ? list.JoinCode : null)));
    }
}
