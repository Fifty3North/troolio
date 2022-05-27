using Sample.Shared.ActorInterfaces;
using Troolio.Core;

namespace Sample.Shared.InternalCommands;
public record AddShoppingList(Metadata Headers, Guid ListId) 
    : TroolioInternalCommand<IAllShoppingListsActor>(Headers);

public record JoinList(Metadata Headers) 
    : TroolioInternalCommand<IShoppingListActor>(Headers);

public record RecordListId(Metadata Headers, Guid ListId) 
    : TroolioInternalCommand<IUserActor>(Headers);

public record SendEmailNotification(Metadata Headers, string email, string description)
    : TroolioInternalCommand<IEmailActor>(Headers);