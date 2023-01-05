using Sample.Shared.ActorInterfaces;
using Troolio.Core;

namespace Sample.Shared.InternalCommands;
public record AddShoppingList(Metadata Headers, Guid ListId) 
    : InternalCommand<IAllShoppingListsActor>(Headers);

public record JoinList(Metadata Headers) 
    : InternalCommand<IShoppingListActor>(Headers);

public record RecordListId(Metadata Headers, Guid ListId) 
    : InternalCommand<IUserActor>(Headers);

public record SendEmailNotification(Metadata Headers, string Email, string Description)
    : InternalCommand<IEmailActor>(Headers);