using ShoppingList.Shared.Enums;
using System.Collections.Immutable;
using Troolio.Core.State;

namespace ShoppingList.Host.App.ShoppingList;

public record ShoppingListState(Guid Author, ImmutableList<ShoppingListItemState> Items, string Title, ImmutableList<Guid> Collaborators): IActorState;
public record ShoppingListItemState(Guid Id, string Name, ItemState Status, uint Quantity);