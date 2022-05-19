using NUnit.Framework;
using Orleankka;
using Orleankka.Client;
using Sample.Shared.ShoppingList;
using System.Collections.Immutable;
using Troolio.Core;

namespace Troolio.Tests;

internal class ShoppingListTests
{
    private IClientActorSystem _actorSystem;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _actorSystem = await Troolio.Tests.Setup.ActorSystemServer.ConnectClient();
    }

    [OneTimeTearDown]
    public async Task Shutdown()
    {
        await Troolio.Tests.Setup.ActorSystemServer.DisconnectClient();
    }

    #region Helpers ...
    private async Task<ImmutableList<ShoppingListQueryResult>> GetUserShoppingLists(Metadata user)
    {
        return await _actorSystem.TypedActorOf<IUserActor>(user.UserId.ToString())
            .Ask(new MyShoppingLists());
    }
    private async Task<string> HavingAddedAnItemToAShoppingList(ActorRef<IShoppingListActor> actor, Metadata user)
    {
        string itemDescription = Guid.NewGuid().ToString();
        await actor.Tell(new AddItemToList(user with { CorrelationId = Guid.NewGuid() }, new AddItemToListPayload(itemDescription, 1)));

        return itemDescription;
    }
    private async Task<(Guid Id, ActorRef<IShoppingListActor> ActorRef)> HavingCreatedShoppingListForUser(Metadata user)
    {
        Guid shoppingListId = Guid.NewGuid();
        ActorRef<IShoppingListActor> actor = _actorSystem.TypedActorOf<IShoppingListActor>(shoppingListId.ToString());
        await actor
            .Tell(new CreateNewList(user with { CorrelationId = Guid.NewGuid() }, new CreateNewListPayload(shoppingListId.ToString())));
        return (shoppingListId, actor);
    }
    #endregion

    [Test]
    public async Task NewList()
    {
        Metadata headers = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await HavingCreatedShoppingListForUser(headers);

        var lists = await GetUserShoppingLists(headers);
        Assert.IsNotNull(lists);
        Assert.AreEqual(1, lists.Count);
    }

    [Test]
    public async Task AddItemToList()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        (Guid shoppingListId, ActorRef<IShoppingListActor> shoppingListActor) = await HavingCreatedShoppingListForUser(user);
        string itemDescription = await HavingAddedAnItemToAShoppingList(shoppingListActor, user);

        var listDetails = await shoppingListActor.Ask(new ShoppingListDetails());
        Assert.AreEqual(1, listDetails.Items.Count());
    }

    [Test]
    public async Task CrossItemOffList()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        (Guid shoppingListId, ActorRef<IShoppingListActor> shoppingListActor) = await HavingCreatedShoppingListForUser(user);
        string itemDescription = await HavingAddedAnItemToAShoppingList(shoppingListActor, user);
        ShoppingListQueryResult list = await shoppingListActor.Ask(new ShoppingListDetails());
        var item = list.Items.Where(i => i.Name == itemDescription).FirstOrDefault();

        await shoppingListActor.Tell(new CrossItemOffList(user, new CrossItemOffListPayload(item.Id)));

        list = await shoppingListActor.Ask(new ShoppingListDetails());
        item = list.Items.Where(i => i.Name == itemDescription).FirstOrDefault();
        Assert.IsNotNull(item);
        Assert.AreEqual(ShoppingItemQueryItemState.CrossedOff, item.Status);
    }

    [Test]
    public async Task RemoveItemFromList()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        (Guid shoppingListId, ActorRef<IShoppingListActor> shoppingListActor) = await HavingCreatedShoppingListForUser(user);
        string itemDescription = await HavingAddedAnItemToAShoppingList(shoppingListActor, user);
        ShoppingListQueryResult list = await shoppingListActor.Ask(new ShoppingListDetails());
        var item = list.Items.Where(i => i.Name == itemDescription).FirstOrDefault();

        await shoppingListActor.Tell(new RemoveItemFromList(user, new RemoveItemFromListPayload(item.Id)));

        list = await shoppingListActor.Ask(new ShoppingListDetails());
        item = list.Items.Where(i => i.Name == itemDescription).FirstOrDefault();
        Assert.IsNull(item);
    }

}
