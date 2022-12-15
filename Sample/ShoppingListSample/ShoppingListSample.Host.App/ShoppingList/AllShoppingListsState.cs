using System.Collections.Immutable;
using Troolio.Core.State;

namespace ShoppingListSample.Host.App.ShoppingList;

public record AllShoppingListsState(ImmutableDictionary<string, Guid> Lists) : IActorState;
