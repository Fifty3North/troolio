using Orleans;
using Troolio.Core;
using Troolio.Core.State;
namespace Troolio.Extensions.Tests.First
{
    [GenerateSerializer]
    public sealed record Created([property: Id(0)] string Value, Metadata Headers) : Event(Headers);
}
namespace Troolio.Extensions.Tests.Second
{
    [GenerateSerializer]
    public sealed record Created([property: Id(0)] uint Value, Metadata Headers) : Event(Headers);
}
namespace Troolio.Extensions.Tests
{
    [GenerateSerializer]
    public sealed record SnapshotState([property: Id(0)] int Count) : IActorState;
}
