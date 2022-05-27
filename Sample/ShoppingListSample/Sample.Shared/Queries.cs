﻿using Orleankka.Meta;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Enums;
using System.Collections.Immutable;

namespace Sample.Shared.Queries;

public record MyShoppingLists : Query<IUserActor, ImmutableList<ShoppingListQueryResult>>;
public record ShoppingListDetails : Query<IShoppingListActor, ShoppingListQueryResult>;
public record ShoppingListQueryResult(Guid Id, string Title, IEnumerable<ShoppingItemQueryItem> Items, ImmutableList<Guid> Collaborators);
public record ShoppingItemQueryItem(Guid Id, string Name, ItemState Status, uint Quantity);