using Orleans;
namespace Troolio.Core.Projection.Commands;

[GenerateSerializer]
public sealed record Flush([property: Id(0)] bool Force) : IMessage;
