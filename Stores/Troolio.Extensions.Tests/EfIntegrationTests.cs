using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Omu.ValueInjecter;
using Orleankka;
using Orleankka.Cluster;
using Orleans;
using Orleans.Hosting;
using Orleans.Streams;
using Orleans.Runtime;
using Troolio.Core;
using Troolio.Core.Projection;
using Troolio.Core.Serialization;
using Troolio.Stores;

namespace Troolio.Extensions.Tests;

public sealed class Row { public Guid Id { get; set; } public string Name { get; set; } = ""; }
public sealed class ProjectionDb(DbContextOptions<ProjectionDb> options) : DbContext(options)
{
    public DbSet<Row> Rows => Set<Row>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<Row>().HasIndex(row => row.Name).IsUnique();
}
[GenerateSerializer]
public sealed record CreateRow([property: Id(0)] Guid Key, [property: Id(1)] string Name, Metadata Headers) : Event(Headers);
[GenerateSerializer]
public sealed record UpdateRow([property: Id(0)] Guid Key, [property: Id(1)] string Name, Metadata Headers) : Event(Headers);
[GenerateSerializer]
public sealed record DeleteRow([property: Id(0)] Guid Key, Metadata Headers) : Event(Headers);
[GenerateSerializer]
public sealed record DeactivateProjection;

public interface IRowProjection : IProjectionActor, IGrainWithGuidCompoundKey { }
public sealed class ProjectionLifecycle
{
    public int Activations;
    public TaskCompletionSource Deactivated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

[ProjectionStreamSubscription("Rows")]
public sealed class RowProjection : EntityFrameworkBatchedProjection<Row, ProjectionDb>, IRowProjection
{
    public override async Task OnActivateAsync(CancellationToken token)
    {
        await base.OnActivateAsync(token);
        ServiceProvider.GetRequiredService<ProjectionLifecycle>().Activations++;
    }
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        ServiceProvider.GetRequiredService<ProjectionLifecycle>().Deactivated.TrySetResult();
        await base.OnDeactivateAsync(reason, token);
    }
    protected override void SetupMappings()
    {
        Mapper.AddMap<EventEnvelope<CreateRow>, Task<EventEntityCreate<Row>>>(envelope =>
            Task.FromResult(new EventEntityCreate<Row>(envelope.Event.Key, new Row { Id = envelope.Event.Key, Name = envelope.Event.Name })));
        Mapper.AddMap<EventEnvelope<UpdateRow>, Task<EventEntityUpdate<Row>>>(envelope =>
            Task.FromResult(new EventEntityUpdate<Row>(envelope.Event.Key, new Action<Row>[] { row => row.Name = envelope.Event.Name })));
        Mapper.AddMap<EventEnvelope<DeleteRow>, Task<EventEntityDelete<Row>>>(envelope =>
            Task.FromResult(new EventEntityDelete<Row>(envelope.Event.Key)));
    }
    public Task On(DeactivateProjection _) { Activation.DeactivateOnIdle(); return Task.CompletedTask; }
    public Task On(EventEnvelope<CreateRow> envelope) => Create(envelope);
    public Task On(EventEnvelope<UpdateRow> envelope) => Update(envelope);
    public Task On(EventEnvelope<DeleteRow> envelope) => Delete(envelope);
}

[TestFixture]
[Category("Integration")]
public sealed class EfIntegrationTests
{
    private IHost _host = null!;
    private IRowProjection _projection = null!;
    private string _database = "";
    [OneTimeSetUp]
    public async Task Setup()
    {
        _database = Path.Combine(Path.GetTempPath(), "troolio-ef-" + Guid.NewGuid().ToString("N") + ".db");
        _host = Host.CreateDefaultBuilder().ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Error))
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Extensions:Clustering:Storage"] = "Local", ["Extensions:Diagnostics"] = "None" }))
            .TroolioServer<InMemoryStore, JsonEventSerializer>("Extensions", new[] { typeof(RowProjection).Assembly },
                services => services.AddSingleton<ProjectionLifecycle>().AddDbContext<ProjectionDb>(options => options.UseSqlite("Data Source=" + _database)), null, null,
                (_, silo) => silo.UseLocalhostClustering(19210, 19211, serviceId: "extensions", clusterId: "extensions")
                    .UseInMemoryReminderService().AddMemoryGrainStorageAsDefault().AddMemoryGrainStorage("PubSubStore")
                    .AddBroadcastChannel(Constants.OrchestrationStreamPrefix, options => options.FireAndForgetDelivery = false)
                    .AddMemoryStreams(Constants.ProjectionStreamPrefix, _ => { }).UseOrleankka());
        using (var scope = _host.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<ProjectionDb>().Database.EnsureCreatedAsync();
        await _host.StartAsync();
        _projection = _host.Services.GetRequiredService<IGrainFactory>().GetGrain<IRowProjection>(Guid.Empty, "aqp:Rows-000", null);
    }
    [OneTimeTearDown]
    public async Task Teardown()
    {
        if (_host is not null) { await _host.StopAsync(); _host.Dispose(); }
    }
    private static EventEnvelope<T> Envelope<T>(Guid id, T value, ulong version) where T : Event =>
        new(id.ToString(), "Rows-" + id, value, version);
    private static Metadata Headers() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Test]
    public async Task Awaited_Writes_And_Idempotent_Creates_Keep_Current_State()
    {
        Guid id = Guid.NewGuid();
        var create = Envelope(id, new CreateRow(id, "first", Headers()), 1);
        await _projection.ReceiveTell(create);
        await _projection.ReceiveTell(Envelope(id, new UpdateRow(id, "second", Headers()), 2));
        await _projection.ReceiveTell(create);
        using var scope = _host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProjectionDb>();
        Assert.That((await context.Rows.FindAsync(id))!.Name, Is.EqualTo("second"));
        await _projection.ReceiveTell(Envelope(id, new DeleteRow(id, Headers()), 3));
        await _projection.ReceiveTell(Envelope(id, new DeleteRow(id, Headers()), 3));
        context.ChangeTracker.Clear();
        Assert.That(await context.Rows.FindAsync(id), Is.Null);
    }

    [Test]
    public async Task Live_Stream_Delivery_Resumes_After_Projection_Deactivation()
    {
        Guid id = Guid.NewGuid();
        var stream = _host.Services.GetRequiredService<IClusterClient>().GetStreamProvider(Constants.ProjectionStreamPrefix)
            .GetStream<IEventEnvelope>("aqp:Rows-000", Guid.Empty);
        await stream.OnNextAsync(Envelope(id, new CreateRow(id, "before", Headers()), 1));
        await WaitForRow(id, "before");
        await _projection.ReceiveTell(new DeactivateProjection());
        await _host.Services.GetRequiredService<ProjectionLifecycle>().Deactivated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await stream.OnNextAsync(Envelope(id, new UpdateRow(id, "after", Headers()), 2));
        await WaitForRow(id, "after");
        Assert.That(_host.Services.GetRequiredService<ProjectionLifecycle>().Activations, Is.GreaterThanOrEqualTo(2));
    }

    private async Task WaitForRow(Guid id, string expected)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(8);
        while (DateTime.UtcNow < deadline)
        {
            using var scope = _host.Services.CreateScope();
            Row? row = await scope.ServiceProvider.GetRequiredService<ProjectionDb>().Rows.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id);
            if (row?.Name == expected) return;
            await Task.Delay(50);
        }
        Assert.Fail($"Live projection did not reach expected row state '{expected}'.");
    }

    [Test]
    public async Task Database_Constraint_Failure_Is_Reported_And_The_Next_Write_Can_Commit()
    {
        Guid first = Guid.NewGuid(), rejected = Guid.NewGuid(), next = Guid.NewGuid();
        string name = "constraint-" + Guid.NewGuid().ToString("N");
        await _projection.ReceiveTell(Envelope(first, new CreateRow(first, name, Headers()), 1));
        Assert.CatchAsync<Exception>(() => _projection.ReceiveTell(Envelope(rejected, new CreateRow(rejected, name, Headers()), 1)));
        await _projection.ReceiveTell(Envelope(next, new CreateRow(next, name + "-next", Headers()), 1));
        using var scope = _host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProjectionDb>();
        Assert.That(await context.Rows.FindAsync(rejected), Is.Null);
        Assert.That(await context.Rows.FindAsync(next), Is.Not.Null);
    }

    [Test]
    public void Persistence_Failures_Reach_The_Caller() => Assert.ThrowsAsync<Troolio.Core.Projection.Exceptions.EntityDoesNotExistException>(
        () => _projection.ReceiveTell(Envelope(Guid.NewGuid(), new UpdateRow(Guid.NewGuid(), "missing", Headers()), 1)));
}
