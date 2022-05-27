using Sample.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Creatable;

namespace Sample.Shared.Commands;

public record AddItemToListPayload(string Description, ushort Quantity);
public record AddItemToList(Metadata Headers, AddItemToListPayload payload) 
    : TroolioPublicCommand<IShoppingListActor>(Headers);

public record CreateNewListPayload(string Title);
public record CreateNewList(Metadata Headers, CreateNewListPayload payload) 
    : TroolioPublicCommand<IShoppingListActor>(Headers), ICreateActorCommand;

public record CrossItemOffListPayload(Guid ItemId);
public record CrossItemOffList(Metadata Headers, CrossItemOffListPayload payload) 
    : TroolioPublicCommand<IShoppingListActor>(Headers);

public record RemoveItemFromListPayload(Guid ItemId);
public record RemoveItemFromList(Metadata Headers, RemoveItemFromListPayload payload) 
    : TroolioPublicCommand<IShoppingListActor>(Headers);

public record JoinListUsingCodePayload(string Code);
public record JoinListUsingCode(Metadata Headers, JoinListUsingCodePayload payload) 
    : TroolioPublicCommand<IAllShoppingListsActor>(Headers);