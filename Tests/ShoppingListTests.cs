using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Sample.Database.Model;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Commands;
using Sample.Shared.InternalCommands;
using Sample.Shared.Events;
using Sample.Host.App.ShoppingList;
using Sample.Shared.Queries;
using Sample.Shared.ReadModels;
using Troolio.Core;
using Troolio.Core.Client;
using Troolio.Core.ReadModels;
using Troolio.Tests.Setup;

namespace Troolio.Tests;

[TestFixture, NonParallelizable, Category("ShoppingRuntime")]
public sealed class ShoppingListTests
{
    private static readonly Guid Alice = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Bob = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Carol = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static Metadata Headers(Guid user) => new(Guid.NewGuid(), user, Guid.NewGuid());
    private static ITroolioClient Runtime => ActorSystemServer.Client;
    private static async Task Wait(Func<Task<bool>> condition)
    {
        var until = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < until) { if (await condition()) return; await Task.Delay(50); }
        Assert.Fail("Live projection/orchestration did not complete within 20 seconds.");
    }
    private static async Task<Guid> Create()
    {
        var id = Guid.NewGuid();
        await Runtime.Tell(id.ToString(), new CreateNewList(Headers(Alice), new("Groceries")));
        await Wait(async () => !string.IsNullOrEmpty((await Runtime.Get<ShoppingListReadModel>(id.ToString())).JoinCode));
        return id;
    }

    [Test]
    public async Task ActorAndLiveRelationalProjectionsSupportCollaborationAndDuplicateDelivery()
    {
        var id = await Create();
        var code = await Runtime.Ask(id.ToString(), new GetJoinCode(Alice));
        var index = new AllShoppingListsActor(ActorSystemServer.Store, new ConfigurationBuilder().Build());
        index.On(new ShoppingListAdded(id, code, Headers(Alice)));
        Assert.That(index.Handle(new AddShoppingList(Headers(Alice), id, code)), Is.Empty);
        var itemId = Guid.NewGuid();
        await Runtime.Tell(id.ToString(), new AddItemToList(Headers(Alice), new(itemId, "Milk", 2)));
        await Runtime.Tell(Constants.SingletonActorId, new JoinListUsingCode(Headers(Bob), new(code)));
        await Wait(async () => (await Runtime.Ask(id.ToString(), new ShoppingListDetails(Alice))).Collaborators.Contains(Bob));
        await Runtime.Tell(Constants.SingletonActorId, new JoinListUsingCode(Headers(Bob), new(code)));
        await Task.Delay(200);
        await Runtime.Tell(id.ToString(), new CrossItemOffList(Headers(Bob), new(itemId)));
        await Wait(async () =>
        {
            await using var db = new ShoppingListsDbContext(new DbContextOptionsBuilder<ShoppingListsDbContext>().UseSqlite(ActorSystemServer.ConnectionString).Options);
            return await db.ShoppingListMembers.CountAsync(x => x.ShoppingListId == id) == 2
                && await db.ShoppingListItems.AnyAsync(x => x.Id == itemId && x.Status == Sample.Shared.Enums.ItemState.CrossedOff);
        });
        var model = await Runtime.Get<ShoppingListReadModel>(id.ToString());
        Assert.That(model.Authorized(Headers(Bob)), Is.True);
        Assert.That(model.Authorized(Headers(Carol)), Is.False);
        Assert.That(model.Collaborators.Count(x => x == Bob), Is.EqualTo(1));
        // Repeated linked source events must not reset newer state or duplicate an item.
        var linkedStream = $"ShoppingListReadModel-{id}";
        var (_, linkHead) = await ActorSystemServer.Store.ReadLastEvent(linkedStream);
        await ActorSystemServer.Store.Append(linkedStream, linkHead,
            [new LinkEvent(Guid.NewGuid(), 2, $"ShoppingListActor-{id}"), new LinkEvent(Guid.NewGuid(), 3, $"ShoppingListActor-{id}")]);
        var repeated = await Runtime.Get<ShoppingListReadModel>(id.ToString());
        Assert.That(repeated.Items.Count(), Is.EqualTo(1));
        Assert.That(repeated.Items.Single().CrossedOff, Is.True);
        Assert.That(repeated.Collaborators.Count(x => x == Bob), Is.EqualTo(1));

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => Runtime.Ask(id.ToString(), new ShoppingListDetails(Carol)));
        Assert.ThrowsAsync<UnauthorizedAccessException>(() => Runtime.Tell(id.ToString(), new AddItemToList(Headers(Carol), new(Guid.NewGuid(), "Stolen", 1))));
    }

    [Test]
    public async Task ApiRequiresVerifiedIdentityAndServesBoundedSqlCatalog()
    {
        var id = Guid.NewGuid();
        await using var factory = new ShoppingApiFactory();
        using var client = factory.CreateClient();
        Assert.That((await client.GetAsync($"/ShoppingList/{id}/ShoppingListDetails")).StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "demo-alice");
        Assert.That((await client.PostAsJsonAsync($"/ShoppingList/{id}/CreateNewList", new { title = "HTTP groceries" })).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That((await client.PostAsJsonAsync($"/ShoppingList/{id}/AddItemToList", new { itemId = Guid.NewGuid(), description = "HTTP milk", quantity = 1 })).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        await Wait(async () => (await client.GetFromJsonAsync<ShoppingListQueryResult[]>($"/User/{Alice}/MyShoppingLists"))!.Any(x => x.Id == id));
        await Wait(async () => (await client.GetFromJsonAsync<ShoppingListReadModel>($"/ShoppingList/{id}/ShoppingListReadModel"))!.Items.Any());
        var details = await client.GetFromJsonAsync<ShoppingListQueryResult>($"/ShoppingList/{id}/ShoppingListDetails");
        Assert.That(details!.Title, Is.EqualTo("HTTP groceries"));
        Assert.That((await client.GetAsync($"/User/{Bob}/MyShoppingLists")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        client.DefaultRequestHeaders.Add("userId", Bob.ToString());
        Assert.That((await client.GetAsync($"/ShoppingList/{id}/ShoppingListDetails")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        client.DefaultRequestHeaders.Remove("userId");
        Assert.That((await client.GetAsync($"/User/{Alice}/MyShoppingLists?take=101")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "demo-carol");
        Assert.That((await client.GetAsync($"/ShoppingList/{id}/ShoppingListReadModel")).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task OwnActorReplayDoesNotRepeatOrchestrationOrProjectionWrites()
    {
        var id = await Create();
        var itemId = Guid.NewGuid();
        await Runtime.Tell(id.ToString(), new AddItemToList(Headers(Alice), new(itemId, "Replay safe", 1)));
        await Wait(async () =>
        {
            await using var db = new ShoppingListsDbContext(new DbContextOptionsBuilder<ShoppingListsDbContext>().UseSqlite(ActorSystemServer.ConnectionString).Options);
            return await db.ShoppingListItems.AnyAsync(x => x.Id == itemId);
        });
        var ownStream = $"ShoppingListActor-{id}";
        var indexStream = $"AllShoppingListsActor-{Constants.SingletonActorId}";
        var ownEvents = (await ActorSystemServer.Store.ReadStream(ownStream)).Length;
        var indexEvents = (await ActorSystemServer.Store.ReadStream(indexStream)).Length;
        var unrelated = $"ShoppingListActor-{Guid.NewGuid()}";
        await ActorSystemServer.Store.Append(unrelated, 0,
            Enumerable.Range(0, 10000).Select(index => (IEvent)new ItemAddedToList(Guid.NewGuid(), $"Invalid without creation {index}", 1, Headers(Carol))).ToArray());
        await ActorSystemServer.Restart();
        Assert.That(ActorSystemServer.Store.Reads.GetValueOrDefault(unrelated), Is.Zero, "Startup must not scan unrelated streams.");
        var restored = await Runtime.Ask(id.ToString(), new ShoppingListDetails(Alice));
        Assert.That(restored.Items.Single().Id, Is.EqualTo(itemId));
        Assert.That((await ActorSystemServer.Store.ReadStream(ownStream)).Length, Is.EqualTo(ownEvents));
        Assert.That((await ActorSystemServer.Store.ReadStream(indexStream)).Length, Is.EqualTo(indexEvents));
        await using var check = new ShoppingListsDbContext(new DbContextOptionsBuilder<ShoppingListsDbContext>().UseSqlite(ActorSystemServer.ConnectionString).Options);
        Assert.That(await check.ShoppingListMembers.CountAsync(x => x.ShoppingListId == id), Is.EqualTo(1));
        Assert.That(await check.ShoppingListItems.CountAsync(x => x.Id == itemId), Is.EqualTo(1));
    }

    private sealed class ShoppingApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Shopping:ReadModels:Path"] = ActorSystemServer.DatabasePath }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ITroolioClient>();
                services.AddSingleton(ActorSystemServer.Client);
            });
        }
    }
}
