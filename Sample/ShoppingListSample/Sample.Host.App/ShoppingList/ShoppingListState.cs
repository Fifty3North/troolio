using Orleans;
using Sample.Shared.Enums;
using System.Collections.Immutable;
using Troolio.Core.State;

namespace Sample.Host.App.ShoppingList;

[GenerateSerializer]
public record ShoppingListState(Guid Author, ImmutableList<ShoppingListItemState> Items, string Title, ImmutableList<Guid> Collaborators): IActorState;
[GenerateSerializer]
public record ShoppingListItemState(Guid Id, string Name, ItemState Status, uint Quantity);
