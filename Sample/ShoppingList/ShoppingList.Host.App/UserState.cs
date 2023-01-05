using System.Collections.Immutable;
using Troolio.Core.State;

namespace ShoppingList.Host.App;

public record UserState(ImmutableList<Guid> Lists): IActorState;