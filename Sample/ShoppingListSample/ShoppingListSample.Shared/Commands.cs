using ShoppingListSample.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Creatable;

namespace ShoppingListSample.Shared.Commands;

public record AddItemToListPayload(Guid ItemId, string Description, ushort Quantity);
public record AddItemToList(Metadata Headers, AddItemToListPayload Payload) 
    : Command<IShoppingListActor>(Headers);

public record CreateNewListPayload(string Title);
public record CreateNewList(Metadata Headers, CreateNewListPayload Payload) 
    : Command<IShoppingListActor>(Headers), ICreateActorCommand;

public record CrossItemOffListPayload(Guid ItemId);
public record CrossItemOffList(Metadata Headers, CrossItemOffListPayload Payload) 
    : Command<IShoppingListActor>(Headers);

public record RemoveItemFromListPayload(Guid ItemId);
public record RemoveItemFromList(Metadata Headers, RemoveItemFromListPayload Payload) 
    : Command<IShoppingListActor>(Headers);

public record JoinListUsingCodePayload(string Code);
public record JoinListUsingCode(Metadata Headers, JoinListUsingCodePayload Payload) 
    : Command<IAllShoppingListsActor>(Headers);

public record Ping(Metadata Headers) : Command<IPingActor>(Headers);