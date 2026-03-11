using Sample.Shared.ActorInterfaces;
using Sample.Shared.Enums;
using Orleans;
using System.Collections.Immutable;
using Troolio.Core;

namespace Sample.Shared.Queries;

[GenerateSerializer]
public record MyShoppingLists : Query<IUserActor, ImmutableList<ShoppingListQueryResult>>;

[GenerateSerializer]
public record ShoppingListDetails : Query<IShoppingListActor, ShoppingListQueryResult>;

[GenerateSerializer]
public record ShoppingListQueryResult(Guid Id, string Title, IEnumerable<ShoppingItemQueryItem> Items, ImmutableList<Guid> Collaborators);

[GenerateSerializer]
public record ShoppingItemQueryItem(Guid Id, string Name, ItemState Status, uint Quantity);

[GenerateSerializer]
public record GetJoinCode(Guid UserId) : Query<IShoppingListActor, string>;
