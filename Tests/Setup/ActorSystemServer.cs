using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.Configuration;
using Orleankka;
using Sample.Database.Model;
using Sample.Host.App.ShoppingList;
using Sample.Host.App;
using Sample.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Client;
using Troolio.Core.ReadModels;
using Troolio.MessageQueue;

namespace Troolio.Tests.Setup;

public static class ActorSystemServer
{
    private static IHost? host;
    public static TrackingStore Store { get; } = new();
    public static string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"troolio-shopping-{Guid.NewGuid():N}.db");
    public static string ConnectionString => $"Data Source={DatabasePath};Default Timeout=30";
    public static ITroolioClient Client { get; private set; } = null!;
    public static async Task Start()
    {
        host = Host.CreateDefaultBuilder().ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Shopping:Clustering:Storage"] = "Local", ["snapshotThreshold"] = "0" }))
            .TroolioServer("Shopping", [typeof(IShoppingListActor).Assembly, typeof(ShoppingListActor).Assembly], services =>
            {
                services.AddSingleton<Troolio.Stores.IStore>(Store);
                services.AddDbContext<ShoppingListsDbContext>(options => options.UseSqlite(ConnectionString));
                services.AddSingleton<IMessageQueueProvider, InMemoryMessageQueueProvider>();
            }, silo =>
            {
                silo.ConfigureServices(services => services.AddShoppingReadModels());
                silo.UseLocalhostClustering(19120, 19121, new IPEndPoint(IPAddress.Loopback, 19120));
                silo.Configure<EndpointOptions>(options => { options.SiloPort = 19120; options.GatewayPort = 19121; });
            });
        using (var scope = host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ShoppingListsDbContext>().Database.EnsureCreatedAsync();
        await host.StartAsync();
        Client = new InProcessClient(host.Services.GetRequiredService<IActorSystem>(), host.Services.GetRequiredService<IGrainFactory>());
    }
    public static async Task Restart()
    {
        if (host is not null) { await host.StopAsync(); host.Dispose(); }
        await Start();
    }
    public static async Task Shutdown()
    {
        if (host is not null) { await host.StopAsync(); host.Dispose(); }
        foreach (var path in new[] { DatabasePath, DatabasePath + "-wal", DatabasePath + "-shm" })
            if (File.Exists(path)) File.Delete(path);
    }
    private sealed class InProcessClient(IActorSystem system, IGrainFactory grains) : ITroolioClient
    {
        public Task Tell<TActor>(string id, ActorMessage<TActor> message) where TActor : IActor
            => system.TypedActorOf<TActor>(id).Tell(message);
        public Task<TResult> Ask<TActor, TResult>(string id, ActorMessage<TActor, TResult> message) where TActor : IActor
            => system.TypedActorOf<TActor>(id).Ask<TResult>(message);
        public Task<TReadModel> Get<TReadModel>(string id) where TReadModel : ITroolioReadModel
            => grains.GetGrain<IReadModelResolver<TReadModel>>(Constants.SingletonActorId).Get(id);
    }
}

public sealed class TrackingStore : Troolio.Stores.IStore
{
    private readonly Troolio.Stores.InMemoryStore inner = new(new Troolio.Core.Serialization.JsonEventSerializer(Microsoft.Extensions.Logging.Abstractions.NullLogger<Troolio.Core.Serialization.JsonEventSerializer>.Instance));
    public System.Collections.Concurrent.ConcurrentDictionary<string, int> Reads { get; } = new();
    private void Read(string stream) => Reads.AddOrUpdate(stream, 1, (_, count) => count + 1);
    public Task Clear() => inner.Clear();
    public Task<ulong> Append(string stream, ulong version, ICollection<IEvent> events) => inner.Append(stream, version, events);
    public Task<IEvent[]> ReadStream(string stream) { Read(stream); return inner.ReadStream(stream); }
    public Task<IEvent[]> ReadStreamFromEvent(string stream, ulong version) { Read(stream); return inner.ReadStreamFromEvent(stream, version); }
    public Task<(IEvent? Event, ulong Version)> ReadLastEvent(string stream) { Read(stream); return inner.ReadLastEvent(stream); }
    public Task<IEvent?> ReadStreamEvent(string stream, ulong version) { Read(stream); return inner.ReadStreamEvent(stream, version); }
}
