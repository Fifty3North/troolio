using EventStore.ClientAPI;
using System.Text;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Stores.EventStore;

namespace Troolio.Stores
{
    public class ESStore : IStore
    {
        public readonly Troolio.Core.Serialization.IEventSerializer Serializer;
        private readonly ILogger _logger;

        public ESStore(Troolio.Core.Serialization.IEventSerializer serializer, ILogger logger)
        {
            Serializer = serializer;
            _logger = logger;
        }

        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public async Task <IEvent[]> ReadStream(string streamName)
        {
            return await ReadStreamFromEvent(streamName, StreamPosition.Start);
        }

        //TODO: Class to hook up to EventStore, as a database
        public async Task<ulong> Append(string streamName, ulong expectedEvVersion, ICollection<IEvent> events)
        {
            // First event version (number) in EventStore is 0. Within Actor implementation is 1.
            long expectedVersion = (long)expectedEvVersion - 1;

            if (events.Count == 0)
            {
                return 0;
            }

            //EventData[] serialized = events.Select(e => e is LinkEvent le ? 
            //    ToEventData(le.EventId, le, new Dictionary<string,object>()) : 
            //    ToEventData((Event)e)
            //).ToArray();

            EventData[] serialized = events.Select(e => ToEventData(e)).ToArray();

            try
            {
                WriteResult result =  await ES.Connection!.AppendToStreamAsync(streamName, expectedVersion, serialized);
                return (ulong)events.Count;
            }
            catch (global::EventStore.ClientAPI.Exceptions.WrongExpectedVersionException)
            {
                throw new Troolio.Stores.Exceptions.WrongExpectedVersionException($"Wrong Expected EventStore Event Version - Possible duplicate activation of actor '{streamName}' detected");
            }
        }

        public async Task<IEvent[]> ReadStreamFromEvent(string streamName, ulong evVersion)
        {
            StreamEventsSlice? currentSlice;

            // First event version (number) in EventStore is 0. Within Actor implementation is 1.
            long startEventNumber = (long)evVersion - 1;

            if (startEventNumber < StreamPosition.Start)
            {
                startEventNumber = StreamPosition.Start;
            }

            List<IEvent> events = new List<IEvent>();

            do
            {
                currentSlice = await ES.Connection!.ReadStreamEventsForwardAsync(streamName, startEventNumber, 256, true);

                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                {
                    break;
                }

                if (currentSlice.Status == SliceReadStatus.StreamDeleted)
                {
                    throw new InvalidOperationException($"Stream '{streamName}' has beed unexpectedly deleted");
                }

                startEventNumber = currentSlice.NextEventNumber;

                foreach (ResolvedEvent e in currentSlice.Events)
                {
                    events.Add(ConvertResolvedEventToIEvent(e));
                }

            }
            while (!currentSlice.IsEndOfStream);

            return events.ToArray();
        }

        public async Task<(IEvent? Event, ulong Version)> ReadLastEvent(string streamName)
        {
            EventReadResult? eventReadResult = await ES.Connection!.ReadEventAsync(streamName, StreamPosition.End, true);

            if (eventReadResult?.Event != null)
            {
                ResolvedEvent resolvedEvent = eventReadResult.Event.Value;

                IEvent @event = ConvertResolvedEventToIEvent(resolvedEvent);

                // First event version (number) in EventStore is 0. Within Actor implementation is 1.
                // N.B.: for a link event, eventReadResult.EventNumber = -1 regardless of how many events written to the stream
                //ulong version = (ulong)eventReadResult.EventNumber + 1;
                ulong version = (ulong)resolvedEvent.OriginalEventNumber + 1;

                return (@event, version);
            }

            return (null, 0);
        }

        public async Task<IEvent?> ReadStreamEvent(string streamName, ulong evVersion)
        {
            // First event version (number) in EventStore is 0. Within Actor implementation is 1.
            long eventNumber = (long)evVersion - 1;

            if (eventNumber < StreamPosition.Start)
            {
                return null;
            }

            EventReadResult? eventReadResult = await ES.Connection!.ReadEventAsync(streamName, eventNumber, true);

            if (eventReadResult?.Event != null)
            {
                return ConvertResolvedEventToIEvent(eventReadResult.Event.Value);
            }
            else
            {
                return null;
            }
        }

        private EventData ToEventData(IEvent @event)
        {
            IDictionary<string, object> headers = new Dictionary<string, object>();

            if (@event is Event ev)
            {
                Guid eventId = ev.Headers.MessageId;

                string eventTypeName = ev.GetType().AssemblyQualifiedName!;

                byte[] eventData = Serializer.Serialize(ev);

                headers.Add(nameof(Metadata.CorrelationId), ev.Headers.CorrelationId);

                byte[] metadata = Serializer.Serialize(headers);

                return new EventData(eventId, eventTypeName, Serializer.IsJson, eventData, metadata);
            }
            else if (@event is LinkEvent linkEvent)
            {
                Guid eventId = linkEvent.EventId;

                // First event version (number) in EventStore is 0. Within Actor implementation is 1.
                long eventNumber = (long)linkEvent.EventVersion - 1;

                string linkData = $"{eventNumber}@{linkEvent.StreamName}";

                byte[] metadata = Serializer.Serialize(headers);

                return new EventData(eventId, "$>", Serializer.IsJson, Encoding.ASCII.GetBytes(linkData), metadata);
            }
            else
            {
                throw new ArgumentException("Unsupported IEvent type", nameof(@event));
            }
        }

        //private EventData ToEventData(Guid eventId, object @event, IDictionary<string, object> headers)
        //{
        //    // remove metadata from event body as they get inserted into metadata
        //    byte[] data = Serializer.Serialize(@event, new Dictionary<string, Type> {
        //        { nameof(Event.Headers), typeof(Message) }
        //    });

        //    byte[] metadata = Serializer.Serialize(headers);

        //    if (@event is LinkEvent linkEvent)
        //    {
        //        // First event version (number) in EventStore is 0. Within Actor implementation is 1.
        //        long eventNumber = (long)linkEvent.EventVersion - 1;
        //        string linkData = $"{eventNumber}@{linkEvent.StreamName}";

        //        return new EventData(eventId, "$>", Serializer.IsJson, Encoding.ASCII.GetBytes(linkData), metadata);
        //    }
        //    else
        //    {
        //        string eventTypeName = @event.GetType().AssemblyQualifiedName!;
        //        return new EventData(eventId, eventTypeName, Serializer.IsJson, data, metadata);
        //    }
        //}

        //private EventData ToEventData(Event @event)
        //{
        //    IDictionary<string, object> headers = new Dictionary<string, object>()
        //    {
        //        { nameof(Metadata.CorrelationId), @event.Headers.CorrelationId },
        //        { nameof(Metadata.UserId), @event.Headers.UserId },
        //        { nameof(Metadata.DeviceId), @event.Headers.DeviceId },
        //        { nameof(Metadata.MessageId), @event.Headers.MessageId },
        //        { nameof(Metadata.TransactionId), @event.Headers.TransactionId! },
        //        { nameof(Metadata.CausationId), @event.Headers.CausationId! }
        //    };

        //    return ToEventData(@event.Headers.MessageId, @event, headers);
        //}

        private IEvent ConvertResolvedEventToIEvent(ResolvedEvent resolvedEvent)
        {
            Event @event = DeserializeStoredEvent(resolvedEvent.Event)!;

            return @event;
        }

        //private IEvent ConvertResolvedEventToIEvent(ResolvedEvent resolvedEvent)
        //{
        //    Event @event = DeserializeStoredEvent(resolvedEvent.Event)!;

        //    @event = @event with { Headers = new Metadata(Guid.Empty, Guid.Empty, Guid.Empty) };
        //    Dictionary<string, object> metadata = DeserializeMetadata(resolvedEvent.Event.Metadata);

        //    if (TryGetMetadataId(metadata, nameof(Metadata.CorrelationId), out Guid correlationId))
        //    {
        //        @event = @event with { Headers = @event.Headers with { CorrelationId = correlationId } };
        //    }

        //    if (TryGetMetadataId(metadata, nameof(Metadata.UserId), out Guid userId))
        //    {
        //        @event = @event with { Headers = @event.Headers with { UserId = userId } };
        //    }

        //    if (TryGetMetadataId(metadata, nameof(Metadata.DeviceId), out Guid deviceId))
        //    {
        //        @event = @event with { Headers = @event.Headers with { DeviceId = deviceId } };
        //    }

        //    if (TryGetMetadataId(metadata, nameof(Metadata.MessageId), out Guid messageId))
        //    {
        //        @event = @event with { Headers = @event.Headers with { MessageId = messageId } };
        //    }

        //    if (TryGetMetadataId(metadata, nameof(Metadata.TransactionId), out Guid transactionId))
        //    {
        //        @event = @event with { Headers = @event.Headers with { TransactionId = transactionId } };
        //    }

        //    if (TryGetMetadataId(metadata, nameof(Metadata.CausationId), out Guid causationId))
        //    {
        //        @event = @event with { Headers = @event.Headers with { CausationId = causationId } };
        //    }

        //    return @event;
        //}

        private Event? DeserializeStoredEvent(RecordedEvent @event)
        {
            try
            {
                Type eventType = Type.GetType(@event.EventType)!;

                if (eventType != null)
                {
                    return (Event)Serializer.Deserialize(@event.Data, eventType);
                }
                else
                {
                    return null;
                }
            }
            catch (Troolio.Core.Serialization.EventDeserializationException)
            {
                _logger?.Error($"Couldn't deserialize type '{@event.EventType}'. Are you missing an assembly reference, chaged an event or deleted it?");
                //Debug.Assert(false, $"Couldn't deserialize type '{@event.EventType}'. Are you missing an assembly reference, chaged an event or deleted it?");
                var original = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Couldn't deserialize type '{@event.EventType}'. Are you missing an assembly reference, chaged an event or deleted it?");
                Console.ForegroundColor = original;
                return null;
            }
        }

        //private Dictionary<string, object> DeserializeMetadata(byte[] metadata)
        //{
        //    try
        //    {
        //        return (Dictionary<string, object>)Serializer.Deserialize(metadata, typeof(Dictionary<string, object>));
        //    }
        //    catch (Troolio.Core.Serialization.EventDeserializationException)
        //    {
        //        _logger?.Error($"Couldn't deserialize metadata. Are you missing an assembly reference, chaged an event or deleted it?");
        //        var original = Console.ForegroundColor;
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine($"Couldn't deserialize metadata. Are you missing an assembly reference, chaged an event or deleted it?");
        //        Console.ForegroundColor = original;
        //        return null!;
        //    }
        //}

        //private static bool TryGetMetadataId(Dictionary<string, object> metadata, string key, out Guid id)
        //{
        //    if (metadata != null && metadata.TryGetValue(key, out object? value) && value != null && Guid.TryParse(value.ToString(), out id))
        //    {
        //        return true;
        //    }

        //    id = Guid.Empty;

        //    return false;
        //}
    }
}
