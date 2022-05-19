using System.Collections.Immutable;
using Troolio.Core.State;

namespace Sample.Server.ShoppingList;

public record UserState(ImmutableList<Guid> Lists): IActorState;