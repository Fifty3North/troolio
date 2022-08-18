using Troolio.Core;
using Troolio.Core.ReadModels;

namespace Troolio.Stores.Tests.Models
{
    internal record FooReadModel(string? Name, uint Size) : TroolioReadModel
    {
        public FooReadModel On(EventEnvelope<NameUpdated> ev)
            => this with { Name = ev.Event.Name };

        public FooReadModel On(EventEnvelope<SizeUpdated> ev)
            => this with { Size = ev.Event.Size };
    }
}
