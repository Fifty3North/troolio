using Sample.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Creatable;

namespace Sample.Shared.Commands;

public record AddItemToListPayload(string Description, ushort Quantity);
public record AddItemToList(Metadata Headers, AddItemToListPayload payload) 
    : Command<IShoppingListActor>(Headers);

public record CreateNewListPayload(string Title);
public record CreateNewList(Metadata Headers, CreateNewListPayload payload) 
    : Command<IShoppingListActor>(Headers), ICreateActorCommand;

public record CrossItemOffListPayload(Guid ItemId);
public record CrossItemOffList(Metadata Headers, CrossItemOffListPayload payload) 
    : Command<IShoppingListActor>(Headers);

public record RemoveItemFromListPayload(Guid ItemId);
public record RemoveItemFromList(Metadata Headers, RemoveItemFromListPayload payload) 
    : Command<IShoppingListActor>(Headers);

public record JoinListUsingCodePayload(string Code);
public record JoinListUsingCode(Metadata Headers, JoinListUsingCodePayload payload) 
    : Command<IAllShoppingListsActor>(Headers);