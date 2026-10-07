using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Troolio.Core;
using Troolio.Core.Client;
using Troolio.Core.State;
using Troolio.Stores;

// A console journey through real Orleans actors, using only public NuGet packages.
using var host = Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .TroolioServer("Calculator", [typeof(ICalculator).Assembly], services =>
        services.AddSingleton<ITroolioClient>(_ => new TroolioClient(
            [typeof(ICalculator).Assembly], "Calculator",
            new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory))));
await host.StartAsync();
try
{
    var client = host.Services.GetRequiredService<ITroolioClient>();
    var actorId = Guid.NewGuid().ToString();
    var headers = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    await client.Tell(actorId, new RecordInteger(headers, 42));
    await client.Tell(actorId, new RecordInteger(headers, 19));
    var sum = await client.Ask(actorId, new Sum());
    Console.WriteLine($"42 + 19 = {sum}");
    if (sum != 61) throw new InvalidOperationException("The calculator returned an unexpected result.");
}
finally
{
    await host.StopAsync();
}

public interface ICalculator : IActor { }

[GenerateSerializer]
public record RecordInteger(Metadata Headers, [property: Id(0)] int Value)
    : Command<ICalculator>(Headers);

[GenerateSerializer]
public record IntegerRecorded(Metadata Headers, [property: Id(0)] int Value) : Event(Headers);

[GenerateSerializer]
public record Sum : Query<ICalculator, int>;

[GenerateSerializer]
public record CalculatorState([property: Id(0)] int Total) : IActorState;

public sealed class Calculator : EventSourcedActor<CalculatorState>, ICalculator
{
    public Calculator(IStore store, IConfiguration configuration) : base(store, configuration)
        => State = new(0);

    public IEnumerable<Event> Handle(RecordInteger command)
        => [new IntegerRecorded(command.Headers, command.Value)];

    // Replay uses the same pure state transition as live events.
    public void On(IntegerRecorded @event) => State = State with { Total = checked(State.Total + @event.Value) };
    public int Handle(Sum _) => State.Total;
}
