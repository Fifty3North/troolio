using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Troolio.Core;
using Troolio.Core.Client;

Guid userId = Guid.NewGuid();
Guid deviceId = Guid.NewGuid();

Console.WriteLine("Running calculator demo ...\n");

Action<IServiceCollection> configureServices = (s) => {
    s.AddSingleton<ITroolioClient>((_) =>
        new TroolioClient(new[] { typeof(ICalculator).Assembly },
        "Calculator"));
};

var host = Host.CreateDefaultBuilder(args)
        .TroolioServer("Calculator", new[] { typeof(ICalculator).Assembly }, configureServices);

await host.StartAsync();

// get the client
var client = host.Services.GetRequiredService<ITroolioClient>();

Console.WriteLine("Adding 42 and 19");

await client.Tell(Constants.SingletonActorId, new RecordInteger(new Metadata(Guid.NewGuid(), userId, deviceId), 42));
await client.Tell(Constants.SingletonActorId, new RecordInteger(new Metadata(Guid.NewGuid(), userId, deviceId), 19));

var sum = await client.Ask(Constants.SingletonActorId, new Sum());
Console.WriteLine("Sum:" + sum);

Console.WriteLine("Press any key to quit");
Console.ReadKey();

public interface ICalculator : IActor { }
public record RecordInteger(Metadata Headers, int Value) : Command<ICalculator>(Headers);
public record IntegerRecorded(Metadata Headers, int Value) : Event(Headers);
public record Sum : Query<ICalculator, int>;

// Non-event sourced CQRS actor
public class Calculator : CqrsActor, ICalculator
{
    public Calculator(IConfiguration configuration) : base(configuration)
    {
    }

    private List<int> _values = new List<int>();
    public IEnumerable<Event> Handle(RecordInteger command) => new[] { new IntegerRecorded(command.Headers, command.Value) };
    public void On(IntegerRecorded ev) => _values.Add(ev.Value);
    public int Handle(Sum _) => _values.Sum();
}