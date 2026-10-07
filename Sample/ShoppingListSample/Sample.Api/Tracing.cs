using Troolio.Core;
using Troolio.Core.Client;
namespace Sample.Api;
public sealed class ApiTracing(ITroolioClient client)
{
    public Task DisableTracing() => client.Tell(Constants.SingletonActorId, new DisableTracing());
    public Task EnableTracing(TraceLevel level = TraceLevel.Error) => client.Tell(Constants.SingletonActorId, new EnableTracing(level));
    public Task<IList<MessageLog>> Flush() => client.Ask(Constants.SingletonActorId, new Flush());
}
