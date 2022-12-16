using System.Collections.Immutable;
using Troolio.Core.State;

namespace ShoppingList.Host.App;

public record AllShoppingListsState(ImmutableDictionary<string, Guid> Lists) : IActorState;
