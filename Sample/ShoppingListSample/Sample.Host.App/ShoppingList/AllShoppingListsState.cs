using Orleans;
using System.Collections.Immutable;
using Troolio.Core.State;

namespace Sample.Host.App.ShoppingList;

[GenerateSerializer]
public record AllShoppingListsState(ImmutableDictionary<string, Guid> Lists) : IActorState;
