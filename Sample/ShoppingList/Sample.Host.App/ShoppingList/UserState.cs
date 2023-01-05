using System.Collections.Immutable;
using Troolio.Core.State;

namespace Sample.Host.App.ShoppingList;

public record UserState(ImmutableList<Guid> Lists): IActorState;