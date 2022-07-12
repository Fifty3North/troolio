using Troolio.Core;
using Troolio.Core.Client;

namespace Sample.Api;

public class ApiTracing
{
    private ITroolioClient? _client;

    public async Task EnableTracing(ITroolioClient client)
    {
        _client = client;

        if(_client is not null)
        {
            try
            {
                Console.WriteLine("enabling tracing");
                await _client.Tell(Constants.SingletonActorId, new EnableTracing(TraceLevel.Error));

                Console.WriteLine("Tracing enabled... ");
            }
            catch (Exception ex)
            {
                _client = null;
                Console.WriteLine($"Tracing could not be enabled ({ex.Message})");
            }
        }
    }

    public async Task StartTracingToFile(string filename)
    {
        if (_client is not null)
        {
            try
            {
                using (StreamWriter headerFile = new(filename, true))
                {
                    var header = "Action;Status;Item;Id;Version;Silo;MessageType;TransactionId;CorrelationId;CausationId;MessageId;UserId;DeviceId;MessageBody;Elapsed";
                    Console.WriteLine(header);
                    await headerFile.WriteLineAsync(header);
                }

                while (_client is not null)
                {
                    var tracingLog = await _client.Ask(Constants.SingletonActorId, new Flush());
                    using (StreamWriter file = new(filename, true))
                    {
                        foreach (var line in tracingLog)
                        {
                            var debug = $"{line.Action.ToString().ToUpper()};{line.Status.ToString().ToUpper()};{line.Stream};{line.Id};{line.Version};{line.SiloId};{line.Message?.GetType().FullName};{line.Message?.Headers.TransactionId};{line.Message?.Headers.CorrelationId};{line.Message?.Headers.CausationId};{line.Message?.Headers.MessageId};{line.Message?.Headers.UserId};{line.Message?.Headers.DeviceId};{line.Message?.ToString()};{line.Elapsed}";
                            Console.WriteLine(debug);
                            await file.WriteLineAsync(debug);
                        }
                    }

                    await Task.Delay(10000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                using (StreamWriter file = new(filename, true))
                {
                    Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
                }
            }
        }
    }

    public async Task DisableTracing()
    {
        if (_client is not null)
        {
            try
            {
                Console.WriteLine("disabling tracing");
                await _client.Tell(Constants.SingletonActorId, new DisableTracing());

                Console.WriteLine("Tracing disabled... ");
                _client = null;
            }
            catch (Exception ex)
            {
                _client = null;
                Console.WriteLine($"Error disabling tracing ({ex.Message})");
            }
        }
    }
}
