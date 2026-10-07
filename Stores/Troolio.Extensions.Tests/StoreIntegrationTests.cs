using KurrentDB.Client;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Orleans;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Core.Serialization;
using Troolio.Stores;

namespace Troolio.Extensions.Tests;

[GenerateSerializer]
public sealed record ForeignEvent(string Value);

[GenerateSerializer]
public sealed record NameChanged([property: Id(0)] string Name, Metadata Headers) : Event(Headers);

[TestFixture("Marten")]
[TestFixture("EventStore")]
[Category("Integration")]
public sealed class StoreIntegrationTests(string backend)
{
    private IStore _store = null!;
    private IDisposable? _owner;
    private KurrentDBClient? _client;
    private static Metadata Headers() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    private static string Stream() => "extension-test-" + Guid.NewGuid().ToString("N");

    [OneTimeSetUp]
    public async Task Setup()
    {
        if (backend == "Marten")
        {
            string connection = Required("TROOLIO_TEST_MARTEN");
            IDocumentStore documentStore = DocumentStore.For(options => MartenStore.Configure(options, connection, createSchema: true));
            await documentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
            _owner = documentStore;
            _store = new MartenStore(documentStore);
        }
        else
        {
            _client = new KurrentDBClient(KurrentDBClientSettings.Create(Required("TROOLIO_TEST_EVENTSTORE")));
            _store = new ESStore(_client, new JsonEventSerializer(NullLogger<JsonEventSerializer>.Instance), NullLogger<ESStore>.Instance);
            await _store.ReadStream(Stream()); // Connection failure is a failure, not an omitted provider.
        }
    }
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Set {name} to an explicitly owned integration fixture.");
    [OneTimeTearDown]
    public async Task Teardown() { _owner?.Dispose(); if (_client is not null) await _client.DisposeAsync(); }

    [Test]
    public async Task Versions_Metadata_And_Link_Locations_RoundTrip()
    {
        string stream = Stream();
        Metadata headers = Headers() with { CausationId = Guid.NewGuid(), TransactionId = Guid.NewGuid() };
        NameChanged first = new("first", headers);
        NameChanged second = new("second", Headers());
        Assert.That(await _store.ReadStream(stream), Is.Empty);
        Assert.That(await _store.Append(stream, 0, new IEvent[] { first, second }), Is.EqualTo(2));
        IEvent[] read = await _store.ReadStream(stream);
        Assert.That(read, Is.EqualTo(new[] { first, second }));
        Assert.That(await _store.ReadStreamFromEvent(stream, 2), Is.EqualTo(new[] { second }));
        Assert.That(await _store.ReadStreamEvent(stream, 0), Is.Null);
        Assert.That(await _store.ReadStreamEvent(stream, 3), Is.Null);
        var last = await _store.ReadLastEvent(stream);
        Assert.That(last.Version, Is.EqualTo(2));
        Assert.That(last.Event, Is.EqualTo(second));
        string links = Stream();
        await _store.Append(links, 0, new IEvent[] { new LinkEvent(Guid.NewGuid(), 2, stream) });
        Assert.That(await _store.ReadStream(links), Is.EqualTo(new[] { second }));
        Assert.That((await _store.ReadLastEvent(links)).Version, Is.EqualTo(1));
    }

    [Test]
    public async Task Only_One_Concurrent_Append_With_The_Same_Expected_Version_Commits()
    {
        string stream = Stream();
        await _store.Append(stream, 0, new IEvent[] { new NameChanged("initial", Headers()) });
        async Task<bool> Attempt(string name)
        {
            try { await _store.Append(stream, 1, new IEvent[] { new NameChanged(name, Headers()) }); return true; }
            catch (Troolio.Stores.Exceptions.WrongExpectedVersionException) { return false; }
        }
        bool[] results = await Task.WhenAll(Attempt("one"), Attempt("two"));
        Assert.That(results.Count(value => value), Is.EqualTo(1));
        Assert.That((await _store.ReadStream(stream)).Length, Is.EqualTo(2));
    }

    [Test]
    public async Task Independent_Streams_And_Fresh_Store_Reads_Remain_Available()
    {
        string large = Stream(), independent = Stream();
        var events = Enumerable.Range(0, 300).Select(number => (IEvent)new NameChanged(number.ToString(), Headers())).ToArray();
        await _store.Append(large, 0, events);
        await _store.Append(independent, 0, new IEvent[] { new NameChanged("independent", Headers()) });
        Assert.That((await _store.ReadLastEvent(independent)).Version, Is.EqualTo(1));
        Assert.That((await _store.ReadStream(large)).Length, Is.EqualTo(300));
        using IDocumentStore? freshDocuments = backend == "Marten"
            ? DocumentStore.For(options => MartenStore.Configure(options, Required("TROOLIO_TEST_MARTEN"))) : null;
        IStore fresh = backend == "EventStore"
            ? new ESStore(_client!, new JsonEventSerializer(NullLogger<JsonEventSerializer>.Instance), NullLogger<ESStore>.Instance)
            : new MartenStore(freshDocuments!);
        try { Assert.That((await fresh.ReadLastEvent(independent)).Event, Is.EqualTo(new NameChanged("independent", ((NameChanged)(await _store.ReadLastEvent(independent)).Event!).Headers))); }
        finally { (fresh as IDisposable)?.Dispose(); }
    }

    [Test]
    public async Task Same_Names_Across_Namespaces_And_Typed_Snapshots_RoundTrip()
    {
        string stream = Stream();
        var first = new First.Created("first", Headers());
        var second = new Second.Created(42, Headers());
        var snapshot = new Troolio.Core.Snapshots.ActorStateVersionWritten<SnapshotState>(new SnapshotState(12), 77, Headers());
        await _store.Append(stream, 0, new IEvent[] { first, second, snapshot });
        Assert.That(await _store.ReadStream(stream), Is.EqualTo(new IEvent[] { first, second, snapshot }));
        Assert.That(await _store.ReadStreamEvent(stream, 3), Is.EqualTo(snapshot));
        Assert.That(await _store.Append(stream, 3, Array.Empty<IEvent>()), Is.Zero);
        Assert.That((await _store.ReadLastEvent(stream)).Version, Is.EqualTo(3));
    }

    [Test]
    public async Task Malformed_Stream_Fails_Without_Preventing_Independent_Stream_Reads()
    {
        string malformed = Stream(), healthy = Stream();
        if (_client is not null)
            await _client.AppendToStreamAsync(malformed, StreamState.NoStream,
                new[] { new EventData(Uuid.NewUuid(), "unknown.extension.event", "{}"u8.ToArray()) });
        else
        {
            using IDocumentStore store = DocumentStore.For(options => MartenStore.Configure(options, Required("TROOLIO_TEST_MARTEN")));
            await using var session = store.LightweightSession();
            session.Events.Append(malformed, new First.Created("unwrapped", Headers()));
            // A valid legacy unwrapped event is supported; unsupported external data fails.
            session.Events.Append(malformed, new ForeignEvent("unexpected"));
            await session.SaveChangesAsync();
        }
        Assert.CatchAsync<Exception>(async () => await _store.ReadStream(malformed));
        var value = new NameChanged("healthy", Headers());
        await _store.Append(healthy, 0, new IEvent[] { value });
        Assert.That(await _store.ReadStream(healthy), Is.EqualTo(new[] { value }));
    }

    [Test]
    public void Shared_History_Clear_Is_Explicitly_Unsupported() => Assert.ThrowsAsync<NotSupportedException>(() => _store.Clear());
}
