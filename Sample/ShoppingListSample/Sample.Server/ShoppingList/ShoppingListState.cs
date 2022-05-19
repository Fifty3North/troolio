using System.Collections.Immutable;
using Troolio.Core.State;

namespace Sample.Server.ShoppingList;

public enum ItemState
{
    Pending = 0,
    CrossedOff = 1,
    Deleted = 2
}

public record ShoppingListState(Guid Author, ImmutableList<ShoppingListItemState> Items, string Title, ImmutableList<Guid> Collaborators): IActorState;
public record ShoppingListItemState(Guid Id, string Name, ItemState Status, uint Quantity);