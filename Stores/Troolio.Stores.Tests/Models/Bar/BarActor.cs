using Microsoft.Extensions.Configuration;
using Troolio.Core;

namespace Troolio.Stores.Tests.Models.Bar
{
    public record UpdateName(string Name, Metadata Headers) : Command<IBarActor>(Headers);

    public record NameUpdated(string Name, Metadata Headers) : Event(Headers);


    public interface IBarActor : IActor { }

    public class BarActor : EventSourcedActor, IBarActor
    {
        private string? _name;

        public BarActor(IStore store, IConfiguration configuration) : base(store, configuration)
        {
        }

        public IEnumerable<Event> Handle(UpdateName command)
        {
            if (command.Name != _name)
            {
                yield return new NameUpdated(command.Name, command.Headers);
            }
        }

        public void On(NameUpdated ev)
        {
            _name = ev.Name;
        }
    }
}
