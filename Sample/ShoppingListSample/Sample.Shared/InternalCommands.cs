using Sample.Shared.ActorInterfaces;
using Orleans;
using Troolio.Core;

namespace Sample.Shared.InternalCommands;

[GenerateSerializer]
public record AddShoppingList(Metadata Headers, Guid ListId)
    : InternalCommand<IAllShoppingListsActor>(Headers);

[GenerateSerializer]
public record JoinList(Metadata Headers)
    : InternalCommand<IShoppingListActor>(Headers);

[GenerateSerializer]
public record RecordListId(Metadata Headers, Guid ListId)
    : InternalCommand<IUserActor>(Headers);

[GenerateSerializer]
public record SendEmailNotification(Metadata Headers, string Email, string Description)
    : InternalCommand<IEmailActor>(Headers);
