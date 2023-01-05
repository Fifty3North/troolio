using Microsoft.Extensions.Configuration;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Commands;
using Troolio.Core;
using Troolio.Core.State;
using Troolio.Stores;

namespace ShoppingList.Host.App
{
    public record Pong(Metadata Headers) : Event(Headers);

    public record PingState(int Count) : IActorState { }
    internal class PingActor : EventSourcedActor<PingState>, IPingActor
    {
        public PingActor(IStore _store, IConfiguration configuration) : base(_store, configuration)
        {
            State = new PingState(0);
        }

        public IEnumerable<Event> Handle(Ping command)
        {
            return new[] { new Pong(command.Headers) };
        }

        public void On(Pong ev) { State = State with { Count = State.Count + 1 }; }
    }
}
