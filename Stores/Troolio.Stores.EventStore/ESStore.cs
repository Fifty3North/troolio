using KurrentDB.Client;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Core.Serialization;

namespace Troolio.Stores;

/// <summary>Ordinary single-stream persistence through the supported gRPC protocol.</summary>
/// <remarks>Does not implement selected atomic routes or durable dispatch. The host owns its client.</remarks>
public sealed class ESStore(KurrentDBClient client, IEventSerializer serializer, ILogger<ESStore> logger) : IStore
{
    public IEventSerializer Serializer { get; } = serializer;
    public Task Clear() => throw new NotSupportedException("Reset an explicitly owned test database instead of clearing shared event history.");

    public async Task<ulong> Append(string streamName, ulong expectedEvVersion, ICollection<IEvent> events)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName);
        if (events.Count == 0) return 0;
        EventData[] records = events.Select(ToRecord).ToArray();
        try
        {
            if (expectedEvVersion == 0)
                await client.AppendToStreamAsync(streamName, StreamState.NoStream, records);
            else
                await client.AppendToStreamAsync(streamName, expectedEvVersion - 1, records);
        }
        catch (KurrentDB.Client.WrongExpectedVersionException exception)
        {
            logger.LogWarning(exception, "Concurrent append to {Stream}", streamName);
            throw new Exceptions.WrongExpectedVersionException($"Concurrent append to '{streamName}'.");
        }
        return (ulong)records.Length;
    }

    private EventData ToRecord(IEvent value)
    {
        if (value is LinkEvent link)
        {
            if (link.EventVersion == 0) throw new ArgumentOutOfRangeException(nameof(value), "Link versions start at one.");
            byte[] data = Encoding.UTF8.GetBytes($"{link.EventVersion - 1}@{link.StreamName}");
            return new EventData(Uuid.FromGuid(link.EventId), "$>", data, contentType: "application/octet-stream");
        }
        Guid id = value is Event ev && ev.Headers.MessageId != Guid.Empty ? ev.Headers.MessageId : Guid.NewGuid();
        return new EventData(Uuid.FromGuid(id), value.GetType().AssemblyQualifiedName!,
            Serializer.Serialize(value, value.GetType()), contentType: Serializer.IsJson ? "application/json" : "application/octet-stream");
    }

    public Task<IEvent[]> ReadStream(string streamName) => ReadStreamFromEvent(streamName, 0);
    public async Task<IEvent[]> ReadStreamFromEvent(string streamName, ulong evVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(streamName);
        var read = client.ReadStreamAsync(Direction.Forwards, streamName,
            new StreamPosition(evVersion == 0 ? 0 : evVersion - 1), resolveLinkTos: true);
        if (await read.ReadState == ReadState.StreamNotFound) return [];
        List<IEvent> events = [];
        await foreach (ResolvedEvent record in read) events.Add(Decode(record));
        return events.ToArray();
    }

    public async Task<(IEvent? Event, ulong Version)> ReadLastEvent(string streamName)
    {
        var read = client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, maxCount: 1, resolveLinkTos: true);
        if (await read.ReadState == ReadState.StreamNotFound) return (null, 0);
        await foreach (ResolvedEvent record in read)
            return (Decode(record), checked(record.OriginalEventNumber.ToUInt64() + 1));
        return (null, 0);
    }

    public async Task<IEvent?> ReadStreamEvent(string streamName, ulong evVersion)
    {
        if (evVersion == 0) return null;
        var read = client.ReadStreamAsync(Direction.Forwards, streamName, new StreamPosition(evVersion - 1), maxCount: 1, resolveLinkTos: true);
        if (await read.ReadState == ReadState.StreamNotFound) return null;
        await foreach (ResolvedEvent record in read) return Decode(record);
        return null;
    }

    private IEvent Decode(ResolvedEvent record)
    {
        if (record.Event.EventType == "$>")
            throw new InvalidDataException("The linked event could not be resolved.");
        Type type = Type.GetType(record.Event.EventType, throwOnError: true)!;
        if (!typeof(IEvent).IsAssignableFrom(type)) throw new InvalidDataException("Stored type is not a Troolio event.");
        return Serializer.Deserialize(record.Event.Data.ToArray(), type) as IEvent
            ?? throw new InvalidDataException("Stored event could not be deserialized.");
    }
}
