using System.Collections.Immutable;
using Troolio.Core.State;

namespace ShoppingList.Host.App
{
    public record ShoppingListState(Guid AuthorId, ImmutableList<Shared.Models.ShoppingListItemState> Items, string Title, ImmutableList<Guid> Collaborators) : IActorState;
}