using ShoppingList.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Creatable;

namespace ShoppingList.Shared.Commands;

public record AddItemToList(Metadata Headers, Guid ItemId, string Description, ushort Quantity) : Command<IShoppingListActor>(Headers);
public record CreateNewList(Metadata Headers, Guid UserId, string Title) : Command<IShoppingListActor>(Headers), ICreateActorCommand;
public record CrossItemOffList(Metadata Headers, Guid ItemId) : Command<IShoppingListActor>(Headers);
public record RemoveItemFromList(Metadata Headers, Guid ItemId) : Command<IShoppingListActor>(Headers);
public record JoinListUsingCode(Metadata Headers, string Code) : Command<IAllShoppingListsActor>(Headers);

public record Ping(Metadata Headers) : Command<IPingActor>(Headers);