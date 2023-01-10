using System.Collections.Immutable;
using Troolio.Core.State;

namespace ShoppingList.Host.App;

public record AllShoppingListsState(ImmutableList<Shared.Models.AllShoppingListsActorStateItem> Lists) : IActorState;
