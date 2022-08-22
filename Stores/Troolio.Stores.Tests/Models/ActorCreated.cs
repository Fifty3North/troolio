using Troolio.Core;

namespace Troolio.Stores.Tests.Models
{
    public record ActorCreated(DateTime DateCreated, Metadata Headers) : Event(Headers);
}
