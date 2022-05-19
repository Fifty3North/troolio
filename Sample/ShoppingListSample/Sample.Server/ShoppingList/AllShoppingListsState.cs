using System.Collections.Immutable;
using Troolio.Core.State;

namespace Sample.Server.ShoppingList;

public record AllShoppingListsState(ImmutableDictionary<string, Guid> Lists) : IActorState;
