namespace Troolio.Core.Projection.Commands
{
    public record Flush(bool Force) : IMessage;
}
