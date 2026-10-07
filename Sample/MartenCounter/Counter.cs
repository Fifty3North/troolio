using Microsoft.Extensions.Configuration;
using Orleans;
using Troolio.Core;
using Troolio.Core.State;
using Troolio.Stores;

namespace MartenCounter;

public interface ICounterActor : IActor { }
[GenerateSerializer]
public sealed record IncrementCounter(Metadata Headers, [property: Id(0)] Guid OperationId,
    [property: Id(1)] long Amount) : Command<ICounterActor>(Headers);
[GenerateSerializer]
public sealed record GetCount : Query<ICounterActor, CounterState>;
[GenerateSerializer]
public sealed record CounterIncremented(Metadata Headers, [property: Id(0)] Guid OperationId,
    [property: Id(1)] long Amount) : Event(Headers);
[GenerateSerializer]
public sealed record CounterState([property: Id(0)] long Count, [property: Id(1)] Guid LastOperationId,
    [property: Id(2)] long LastAmount) : IActorState
{
    public static CounterState Empty => new(0, Guid.Empty, 0);
    public CounterState Apply(CounterIncremented e) => new(checked(Count + e.Amount), e.OperationId, e.Amount);
}
public sealed class CounterActor : EventSourcedActor<CounterState>, ICounterActor
{
    public CounterActor(IStore store, IConfiguration configuration) : base(store, configuration) => State = CounterState.Empty;
    public IEnumerable<Event> Handle(IncrementCounter command)
    {
        if (command.Headers.UserId == Guid.Empty || command.OperationId == Guid.Empty || command.Amount is < 1 or > 100)
            throw new ArgumentException("Provide a demo user, a nonempty operation ID and an increment from 1 to 100.");
        if (State.LastOperationId == command.OperationId)
        {
            if (State.LastAmount != command.Amount) throw new InvalidOperationException("This operation ID already has a different increment.");
            return [];
        }
        _ = checked(State.Count + command.Amount);
        return [new CounterIncremented(command.Headers, command.OperationId, command.Amount)];
    }
    public void On(CounterIncremented e) => State = State.Apply(e);
    public CounterState Handle(GetCount _) => State;
}
