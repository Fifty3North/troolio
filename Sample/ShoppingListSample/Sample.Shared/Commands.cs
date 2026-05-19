using Sample.Shared.ActorInterfaces;
using Orleans;
using Troolio.Core;
using Troolio.Core.Creatable;

namespace Sample.Shared.Commands;

[GenerateSerializer]
public record AddItemToListPayload(Guid ItemId, string Description, ushort Quantity);

[GenerateSerializer]
public record AddItemToList(Metadata Headers, AddItemToListPayload Payload)
    : Command<IShoppingListActor>(Headers);

[GenerateSerializer]
public record CreateNewListPayload(string Title);

[GenerateSerializer]
public record CreateNewList(Metadata Headers, CreateNewListPayload Payload)
    : Command<IShoppingListActor>(Headers), ICreateActorCommand;

[GenerateSerializer]
public record CrossItemOffListPayload(Guid ItemId);

[GenerateSerializer]
public record CrossItemOffList(Metadata Headers, CrossItemOffListPayload Payload)
    : Command<IShoppingListActor>(Headers);

[GenerateSerializer]
public record RemoveItemFromListPayload(Guid ItemId);

[GenerateSerializer]
public record RemoveItemFromList(Metadata Headers, RemoveItemFromListPayload Payload)
    : Command<IShoppingListActor>(Headers);

[GenerateSerializer]
public record JoinListUsingCodePayload(string Code);

[GenerateSerializer]
public record JoinListUsingCode(Metadata Headers, JoinListUsingCodePayload Payload)
    : Command<IAllShoppingListsActor>(Headers);

[GenerateSerializer]
public record Ping(Metadata Headers) : Command<IPingActor>(Headers);
