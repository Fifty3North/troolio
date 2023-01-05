using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Enums;
using System.Collections.Immutable;
using Troolio.Core;

namespace ShoppingList.Shared.Queries;

public record MyShoppingLists : Query<IUserActor, ImmutableList<ShoppingListQueryResult>>;
public record ShoppingListDetails : Query<IShoppingListActor, ShoppingListQueryResult>;
public record ShoppingListQueryResult(Guid Id, string Title, IEnumerable<ShoppingItemQueryItem> Items, ImmutableList<Guid> Collaborators);
public record ShoppingItemQueryItem(Guid Id, string Name, ItemState Status, uint Quantity);
public record GetJoinCode(Guid UserId) : Query<IShoppingListActor, string>;