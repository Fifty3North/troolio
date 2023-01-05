using System.Collections.Immutable;
using Troolio.Core.State;

namespace Sample.Host.App.ShoppingList;

public record AllShoppingListsState(ImmutableDictionary<string, Guid> Lists) : IActorState;
