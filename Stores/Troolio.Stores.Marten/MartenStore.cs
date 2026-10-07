using Marten;
using Marten.Newtonsoft;
using Microsoft.Extensions.Configuration;
using JasperFx.Events;
using Newtonsoft.Json;
using Troolio.Core;
using IEvent = Troolio.Core.IEvent;
using Marten.Events;
using Troolio.Core.ReadModels;

namespace Troolio.Stores;

// Preserve the wrapper name and shape used by existing Marten-backed Troolio streams.
public sealed record EventWrapper(IEvent Event);

/// <summary>An ordinary event store; it does not provide atomic projection dispatch.</summary>
public sealed class MartenStore : IStore, IDisposable
{
    private readonly IDocumentStore _store;
    private readonly bool _ownsStore;
    public MartenStore(IDocumentStore store) => _store = store;
    public MartenStore(IConfiguration configuration)
    {
        string connection = configuration.GetConnectionString("Marten")
            ?? throw new ArgumentException("ConnectionStrings:Marten is required.");
        _store = DocumentStore.For(options => Configure(options, connection));
        _ownsStore = true;
    }
    /// <summary>Configure wrapper compatibility and expected-version append behavior.</summary>
    public static void Configure(StoreOptions options, string connection, bool createSchema = false)
    {
        options.Connection(connection);
        options.DatabaseSchemaName = "troolio";
        options.Events.StreamIdentity = StreamIdentity.AsString;
        options.Events.AppendMode = EventAppendMode.Rich;
        options.Events.AddEventType(typeof(EventWrapper));
        options.AutoCreateSchemaObjects = createSchema ? JasperFx.AutoCreate.CreateOrUpdate : JasperFx.AutoCreate.None;
        options.UseNewtonsoftForSerialization(configure: settings => settings.TypeNameHandling = TypeNameHandling.Auto);
    }
    public Task Clear() => throw new NotSupportedException("Provision or reset an explicitly owned test database; clearing shared event history is not supported.");
    public async Task<ulong> Append(string streamName, ulong expectedEvVersion, ICollection<IEvent> events)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName);
        if (events.Count == 0) return 0;
        long expectedAfter = checked((long)expectedEvVersion + events.Count);
        await using IDocumentSession session = _store.LightweightSession();
        session.Events.Append(streamName, expectedAfter, events.Select(value => (object)new EventWrapper(value)).ToArray());
        try { await session.SaveChangesAsync(); }
        catch (JasperFx.Events.EventStreamUnexpectedMaxEventIdException exception)
        { throw new Exceptions.WrongExpectedVersionException($"Concurrent append to '{streamName}': {exception.Message}"); }
        return (ulong)events.Count;
    }
    public Task<IEvent[]> ReadStream(string streamName) => ReadStreamFromEvent(streamName, 0);
    public async Task<IEvent[]> ReadStreamFromEvent(string streamName, ulong evVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName);
        await using IQuerySession session = _store.QuerySession();
        var records = await session.Events.FetchStreamAsync(streamName, fromVersion: checked((long)Math.Max(1UL, evVersion)));
        List<IEvent> events = new(records.Count);
        foreach (var record in records) events.Add(await Decode(record.Data));
        return events.ToArray();
    }
    public async Task<(IEvent? Event, ulong Version)> ReadLastEvent(string streamName)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName);
        await using IQuerySession session = _store.QuerySession();
        var state = await session.Events.FetchStreamStateAsync(streamName);
        if (state is null || state.Version == 0) return (null, 0);
        var records = await session.Events.FetchStreamAsync(streamName, fromVersion: state.Version, version: state.Version);
        return (await Decode(records.Single().Data), checked((ulong)state.Version));
    }
    public Task<IEvent?> ReadStreamEvent(string streamName, ulong evVersion) => ReadStreamEventCore(streamName, evVersion, 0);
    private async Task<IEvent?> ReadStreamEventCore(string streamName, ulong evVersion, int depth)
    {
        if (depth > 32) throw new InvalidDataException("Link resolution exceeded the bounded depth; check for a cycle.");
        if (evVersion == 0) return null;
        await using IQuerySession session = _store.QuerySession();
        var records = await session.Events.FetchStreamAsync(streamName, fromVersion: checked((long)evVersion), version: checked((long)evVersion));
        return records.Count == 0 ? null : await Decode(records.Single().Data, depth);
    }
    private async Task<IEvent> Decode(object data, int depth = 0)
    {
        IEvent value = data is EventWrapper wrapper ? wrapper.Event
            : data as IEvent ?? throw new InvalidDataException("Stored event does not contain a supported Troolio event.");
        if (value is LinkEvent link)
            return await ReadStreamEventCore(link.StreamName, link.EventVersion, depth + 1)
                ?? throw new InvalidDataException($"Link target '{link.StreamName}'/{link.EventVersion} is missing.");
        return value;
    }
    public void Dispose() { if (_ownsStore) _store.Dispose(); }
}
