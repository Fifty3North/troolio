using NUnit.Framework;
using Sample.Shared.Commands;
using Sample.Shared.Enums;
using Sample.Shared.Exceptions;
using Sample.Shared.Queries;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Client;

namespace Troolio.Tests;

internal class ShoppingListTests
{
    private ITroolioClient _client;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _client = await Troolio.Tests.Setup.ActorSystemServer.ConnectClient();
    }

    [OneTimeTearDown]
    public async Task Shutdown()
    {
        await Troolio.Tests.Setup.ActorSystemServer.DisconnectClient();
    }

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
        Guid shoppingListId = await HavingCreatedShoppingListForUser(user);
        await HavingAddedAnItemToAShoppingList(shoppingListId, user);

        var listDetails = await _client.Ask(shoppingListId.ToString(), new ShoppingListDetails());
        Assert.AreEqual(1, listDetails.Items.Count());
    }

    [Test]
    public async Task CrossItemOffList()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(user);
        var itemSummary = await HavingAddedAnItemToAShoppingList(shoppingListId, user);
        ShoppingListQueryResult list = await _client.Ask(shoppingListId.ToString(), new ShoppingListDetails());
        var item = list.Items.Where(i => i.Name == itemSummary.Description).FirstOrDefault();

        await _client.Tell(shoppingListId.ToString(), new CrossItemOffList(user, new CrossItemOffListPayload(item.Id)));

        list = await _client.Ask(shoppingListId.ToString(), new ShoppingListDetails());
        item = list.Items.Where(i => i.Name == itemSummary.Description).FirstOrDefault();
        Assert.IsNotNull(item);
        Assert.AreEqual(ItemState.CrossedOff, item.Status);
    }

    [Test]
    public async Task RemoveItemFromList()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(user);
        var itemSummary = await HavingAddedAnItemToAShoppingList(shoppingListId, user);
        ShoppingListQueryResult list = await _client.Ask(shoppingListId.ToString(), new ShoppingListDetails());
        var item = list.Items.Where(i => i.Name == itemSummary.Description).FirstOrDefault();

        await _client.Tell(shoppingListId.ToString(), new RemoveItemFromList(user, new RemoveItemFromListPayload(item.Id)));

        list = await _client.Ask(shoppingListId.ToString(), new ShoppingListDetails());
        item = list.Items.Where(i => i.Name == itemSummary.Description).FirstOrDefault();
        Assert.IsNull(item);
    }

    [Test]
    public async Task DuplicateItemThrowsItemAlreadyExistsException()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(user);
        await HavingAddedAnItemToAShoppingList(shoppingListId, user, "Milk");
        Assert.ThrowsAsync<ItemAlreadyExistsException>(async () => { await HavingAddedAnItemToAShoppingList(shoppingListId, user, "Milk"); });
    }
    
    [Test]
    public async Task AuthorCannotJoinListThrowsException()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(user);
        var joinCode = await _client.Ask(shoppingListId.ToString(), new GetJoinCode(user.UserId));
        
        Assert.ThrowsAsync<AuthorCannotJoinListException>(async () => { await _client.Tell(Constants.SingletonActorId, new JoinListUsingCode(user, new JoinListUsingCodePayload(joinCode))); });
    }

    [Test]
    public async Task JoinList()
    {
        Metadata author = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Metadata collaborator = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(author);
        var joinCode = await _client.Ask(shoppingListId.ToString(), new GetJoinCode(author.UserId));
        await _client.Tell(Constants.SingletonActorId, new JoinListUsingCode(collaborator, new JoinListUsingCodePayload(joinCode)));
        var list = await _client.Ask(shoppingListId.ToString(), new ShoppingListDetails());
        Assert.Contains(collaborator.UserId, list.Collaborators);
    }

    [Test]
    public async Task UserHasAlreadyJoinedListThrowsException()
    {
        Metadata author = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Metadata collaborator = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(author);
        var joinCode = await _client.Ask(shoppingListId.ToString(), new GetJoinCode(author.UserId));
        await _client.Tell(Constants.SingletonActorId, new JoinListUsingCode(collaborator, new JoinListUsingCodePayload(joinCode)));
        
        Assert.ThrowsAsync<UserHasAlreadyJoinedListException>(async () => {
            await _client.Tell(Constants.SingletonActorId, new JoinListUsingCode(collaborator, new JoinListUsingCodePayload(joinCode)));
        });
    }

    [Test]
    public async Task CollaboratorCannotRemoveItemFromListThrowsException()
    {
        // setup users
        Metadata author = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Metadata collaborator = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        
        // create list
        Guid shoppingListId = await HavingCreatedShoppingListForUser(author);
        
        // get join code
        var joinCode = await _client.Ask(shoppingListId.ToString(), new GetJoinCode(author.UserId));

        // join list
        await _client.Tell(Constants.SingletonActorId, new JoinListUsingCode(collaborator, new JoinListUsingCodePayload(joinCode)));
        
        // add item to list
        var itemSummary = await HavingAddedAnItemToAShoppingList(shoppingListId, author);
        
        // get item id
        var list = await _client.Ask(shoppingListId.ToString(), new ShoppingListDetails());
        var itemId = list.Items.Where(i => i.Name == itemSummary.Description).FirstOrDefault().Id;

        // try to remove item as a collaborator
        Assert.ThrowsAsync<CollaboratorCannotRemoveItemFromListException>(async () => { 
            await _client.Tell(shoppingListId.ToString(), new RemoveItemFromList(collaborator, new RemoveItemFromListPayload(itemId))); 
        });
    }

    [Test]
    public async Task InvalidJoinCodeThrowsException()
    {
        // setup users
        Metadata author = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Metadata collaborator = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // create list
        Guid shoppingListId = await HavingCreatedShoppingListForUser(author);

        // get join code
        var joinCode = await _client.Ask(shoppingListId.ToString(), new GetJoinCode(author.UserId));

        // try to join list with invalid code
        Assert.ThrowsAsync<InvalidJoinCodeException>(async () => {
            await _client.Tell(Constants.SingletonActorId, new JoinListUsingCode(collaborator, new JoinListUsingCodePayload("abcd")));
        });
    }

    [Test]
    public async Task ItemDoesNotExistThrowsExceptionWhenCrossedOff()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(user);
        await HavingAddedAnItemToAShoppingList(shoppingListId, user);

        Assert.ThrowsAsync<ItemDoesNotExistException>(async () => {
            await _client.Tell(shoppingListId.ToString(), new CrossItemOffList(user, new CrossItemOffListPayload(Guid.NewGuid())));
        });
    }

    [Test]
    public async Task ItemDoesNotExistThrowsExceptionWhenCrossedRemoved()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(user);
        await HavingAddedAnItemToAShoppingList(shoppingListId, user);

        Assert.ThrowsAsync<ItemDoesNotExistException>(async () => {
            await _client.Tell(shoppingListId.ToString(), new RemoveItemFromList(user, new RemoveItemFromListPayload(Guid.NewGuid())));
        });
    }

    [Test]
    public async Task ReadModelIsPopulated()
    {
        Metadata user = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Guid shoppingListId = await HavingCreatedShoppingListForUser(user);
        var item1 = await HavingAddedAnItemToAShoppingList(shoppingListId, user, "Milk");
        var item2 = await HavingAddedAnItemToAShoppingList(shoppingListId, user, "Onions");
        await HavingCrossedAnItemOffAShoppingList(shoppingListId, user, item1.Id);

        var readModel = await _client.Get<Sample.Shared.ReadModels.ShoppingListReadModel>(shoppingListId.ToString());
        
        Assert.AreEqual(2, readModel.Items.Count());
        Assert.AreEqual("Milk", readModel.Items.First().Description);
        Assert.IsTrue(readModel.Items.First().CrossedOff);
    }

    #region Helpers ...
    private async Task<ImmutableList<ShoppingListQueryResult>> GetUserShoppingLists(Metadata user)
    {
        return await _client.Ask(user.UserId.ToString(), new MyShoppingLists());
    }

    private async Task<(Guid Id, string Description)> HavingAddedAnItemToAShoppingList(Guid shoppingListId, Metadata user, string item = null)
    {
        Guid itemId = Guid.NewGuid();
        string itemDescription = item == null ? Guid.NewGuid().ToString() : item;
        await _client.Tell(shoppingListId.ToString(), new AddItemToList(user with { CorrelationId = Guid.NewGuid() }, new AddItemToListPayload(itemId, itemDescription, 1)));

        return (itemId, itemDescription);
    }

    private async Task HavingCrossedAnItemOffAShoppingList(Guid shoppingListId, Metadata user, Guid item)
    {
        await _client.Tell(shoppingListId.ToString(), new CrossItemOffList(user with { CorrelationId = Guid.NewGuid() }, new CrossItemOffListPayload(item)));
    }

    private async Task<Guid> HavingCreatedShoppingListForUser(Metadata user)
    {
        Guid shoppingListId = Guid.NewGuid();
        await _client.Tell(
            shoppingListId.ToString(),
            new CreateNewList(user with { CorrelationId = Guid.NewGuid() }, new CreateNewListPayload(shoppingListId.ToString())));

        return shoppingListId;
    }
    #endregion    
}
