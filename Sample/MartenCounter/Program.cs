using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleankka;
using Orleankka.Cluster;
using Orleans.Hosting;
using Troolio.Core;
using Troolio.Core.Serialization;
using Troolio.Stores;
using MartenCounter;

var builder = Host.CreateDefaultBuilder(args);
using var host = builder.TroolioServer<MartenStore, JsonEventSerializer>("Counter", [typeof(CounterActor).Assembly], services =>
{
    services.AddSingleton<IDocumentStore>(provider =>
    {
        var settings = provider.GetRequiredService<IConfiguration>();
        var connection = settings.GetConnectionString("Marten")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Marten to an existing owned PostgreSQL database.");
        return DocumentStore.For(options => MartenStore.Configure(options, connection,
            createSchema: settings.GetValue("Marten:CreateSchema", false)));
    });
    // Explicit factory avoids the adapter's alternative configuration constructor.
    services.AddSingleton<IStore>(provider => new MartenStore(provider.GetRequiredService<IDocumentStore>()));
}, null, null, (context, silo) =>
{
    silo.UseLocalhostClustering(context.Configuration.GetValue("Counter:SiloPort", 19320),
            context.Configuration.GetValue("Counter:GatewayPort", 19321), serviceId: "marten-counter", clusterId: "marten-counter")
        .UseInMemoryReminderService().AddMemoryGrainStorageAsDefault().AddMemoryGrainStorage("PubSubStore")
        .AddBroadcastChannel(Constants.OrchestrationStreamPrefix, options => options.FireAndForgetDelivery = false)
        .AddMemoryStreams(Constants.ProjectionStreamPrefix, _ => { }).UseOrleankka();
});
var settings = host.Services.GetRequiredService<IConfiguration>();
if (settings.GetValue("Marten:CreateSchema", false))
    await host.Services.GetRequiredService<IDocumentStore>().Storage.ApplyAllConfiguredChangesToDatabaseAsync();
var counterId = Guid.Parse(settings["Counter:Id"] ?? "44444444-4444-4444-4444-444444444444");
var userId = Guid.Parse(settings["Counter:UserId"] ?? "11111111-1111-1111-1111-111111111111");
var amount = settings.GetValue("Counter:Increment", 1L);
var operationId = Guid.TryParse(settings["Counter:OperationId"], out var value) ? value : Guid.NewGuid();
var headers = new Metadata(Guid.NewGuid(), userId, Guid.Empty);
await host.StartAsync();
try
{
    var actor = host.Services.GetRequiredService<IActorSystem>().TypedActorOf<ICounterActor>(counterId.ToString());
    var before = await actor.Ask<CounterState>(new GetCount());
    var command = new IncrementCounter(headers, operationId, amount);
    await actor.Tell(command);
    await actor.Tell(command); // Exact immediate retry must not increment twice.
    var after = await actor.Ask<CounterState>(new GetCount());
    var expected = before.LastOperationId == operationId ? before.Count : checked(before.Count + amount);
    if (after.Count != expected) throw new InvalidOperationException("The counter's persisted retry contract failed.");
    Console.WriteLine($"Counter {counterId}: {before.Count} -> {after.Count}; exact retry kept {after.Count}.");
    Console.WriteLine($"Operation {operationId}. Run again with the same Counter:Id to replay this counter and increment again.");
}
finally { await host.StopAsync(); }
