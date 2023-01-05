using Troolio.Core;
using Troolio.Core.Client;

namespace Sample.Api;

public class ApiTracing
{
    private readonly ITroolioClient _client;

    public ApiTracing(ITroolioClient client)
    {
        _client = client;
    }

    public async Task DisableTracing()
    {
        try
        {
            Console.WriteLine("disabling tracing");
            await _client.Tell(Constants.SingletonActorId, new DisableTracing());

            Console.WriteLine("Tracing disabled... ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disabling tracing ({ex.Message})");
        }
    }

    public async Task EnableTracing(TraceLevel Level = TraceLevel.Error)
    {
        try
        {
            Console.WriteLine("enabling tracing");
            await _client.Tell(Constants.SingletonActorId, new EnableTracing(Level));

            Console.WriteLine("Tracing enabled... ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tracing could not be enabled ({ex.Message})");
        }
    }

    public async Task<IList<MessageLog>> Flush()
    {
        try
        {
            return await _client.Ask(Constants.SingletonActorId, new Flush());
        }
        catch
        {
            throw;
        }
    }
}
