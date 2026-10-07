using KurrentDB.Client;
using Microsoft.EntityFrameworkCore;
using Orleankka;
using Orleankka.Cluster;
using Orleans.Hosting;
using Troolio.Core;
using Troolio.Core.Serialization;
using Troolio.Stores;

public sealed class TodoRuntime(IConfiguration configuration) : IHostedService, IAsyncDisposable
{
    private IHost? host;
    public IStore Store => host!.Services.GetRequiredService<IStore>();
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var builder = Host.CreateDefaultBuilder().ConfigureAppConfiguration((_, options) => options.AddConfiguration(configuration));
        Action<IServiceCollection> services = collection =>
        {
            collection.AddDbContext<TodoReadModelDbContext>(options => options.UseSqlite(
                configuration.GetConnectionString("ReadModels") ?? "Data Source=todo-readmodels.db"));
            if (configuration["Todo:EventStore"] == "EventStore")
                collection.AddSingleton(new KurrentDBClient(KurrentDBClientSettings.Create(
                    configuration["EventStore:ConnectionString"] ?? "esdb://localhost:2113?tls=false")));
            else if (configuration["Todo:EventStore"] != "InMemory")
                collection.AddSingleton<IStore>(sp => new FileSystemStore(sp.GetRequiredService<IEventSerializer>(),
                    Path.GetFullPath(configuration["Todo:EventFolder"] ?? "todo-events")));
        };
        var assemblies = new[] { typeof(TodoActor).Assembly };
        var mode = configuration["Todo:EventStore"] ?? "FileSystem";
        if (mode is not ("FileSystem" or "InMemory" or "EventStore"))
            throw new InvalidOperationException("Todo:EventStore must be FileSystem, InMemory or EventStore.");
        host = mode == "EventStore"
            ? builder.TroolioServer<ESStore, JsonEventSerializer>("Todo", assemblies, services, null, null, ConfigureClustering)
            : builder.TroolioServer<InMemoryStore, JsonEventSerializer>("Todo", assemblies, services, null, null, ConfigureClustering);
        await host.StartAsync(cancellationToken);
    }
    private void ConfigureClustering(HostBuilderContext _, ISiloBuilder silo)
    {
        var siloPort = configuration.GetValue("Todo:SiloPort", 19220);
        var gatewayPort = configuration.GetValue("Todo:GatewayPort", 19221);
        silo.UseLocalhostClustering(siloPort, gatewayPort, serviceId: "todo", clusterId: "todo")
            .UseInMemoryReminderService().AddMemoryGrainStorageAsDefault().AddMemoryGrainStorage("PubSubStore")
            .AddBroadcastChannel(Constants.OrchestrationStreamPrefix, options => options.FireAndForgetDelivery = false)
            .AddMemoryStreams(Constants.ProjectionStreamPrefix, _ => { }).UseOrleankka();
    }
    public Task Tell(Guid id, CreateTodoCommand command) => host!.Services.GetRequiredService<IActorSystem>()
        .TypedActorOf<ITodoActor>(id.ToString()).Tell(command);
    public Task Tell(Guid id, CompleteTodoCommand command) => host!.Services.GetRequiredService<IActorSystem>()
        .TypedActorOf<ITodoActor>(id.ToString()).Tell(command);
    public Task<TodoState> ReadState(Guid id) => host!.Services.GetRequiredService<IActorSystem>()
        .TypedActorOf<ITodoActor>(id.ToString()).Ask<TodoState>(new GetTodoState());
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (host is not null) await host.StopAsync(cancellationToken);
    }
    public async ValueTask DisposeAsync()
    {
        if (host is IAsyncDisposable asyncHost) await asyncHost.DisposeAsync();
        else host?.Dispose();
        host = null;
    }
}
