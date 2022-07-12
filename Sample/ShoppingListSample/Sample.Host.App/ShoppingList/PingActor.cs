using Microsoft.Extensions.Configuration;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troolio.Core;
using Troolio.Core.State;
using Troolio.Stores;

namespace Sample.Host.App.ShoppingList
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
