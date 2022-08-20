using Microsoft.Extensions.Configuration;
using Troolio.Core;

namespace Troolio.Stores.Tests.Models.Foo
{
    public record UpdateName(string Name, Metadata Headers) : Command<IFooActor>(Headers);
    public record UpdateSize(uint Size, Metadata Headers) : Command<IFooActor>(Headers);

    public record NameUpdated(string Name, Metadata Headers) : Event(Headers);
    public record SizeUpdated(uint Size, Metadata Headers) : Event(Headers);


    public interface IFooActor : IActor { }

    public class FooActor : EventSourcedActor, IFooActor
    {
        private string? _name;
        private uint _size;

        public FooActor(IStore store, IConfiguration configuration) : base(store, configuration)
        {
        }

        public IEnumerable<Event> Handle(UpdateName command)
        {
            if (command.Name != _name)
            {
                yield return new NameUpdated(command.Name, command.Headers);
            }
        }

        public IEnumerable<Event> Handle(UpdateSize command)
        {
            if (command.Size != _size)
            {
                yield return new SizeUpdated(command.Size, command.Headers);
            }
        }

        public void On(NameUpdated ev)
        {
            _name = ev.Name;
        }

        public void On(SizeUpdated ev)
        {
            _size = ev.Size;
        }
    }
}
