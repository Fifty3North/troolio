using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using System.Text;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Stores.EventStore;
using Troolio.Stores.Serialization;

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

        public async Task <TroolioEvent[]> ReadStream(string streamName)
        {
            StreamEventsSlice? currentSlice;
            long startEventNumber = StreamPosition.Start;

            List<TroolioEvent> events = new List<TroolioEvent>();

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
                    TroolioEvent @event = DeserializeStoredEvent(e.Event)!;
                    @event = @event with { Headers = new Metadata(Guid.Empty, Guid.Empty, Guid.Empty) };
                    Dictionary<string, object> metadata = DeserializeMetadata(e.Event.Metadata);

                    if (TryGetMetadataId(metadata, nameof(Metadata.CorrelationId), out Guid correlationId))
                    {
                        @event = @event with { Headers = @event.Headers with { CorrelationId = correlationId } };
                    }

                    if (TryGetMetadataId(metadata, nameof(Metadata.UserId), out Guid userId))
                    {
                        @event = @event with { Headers = @event.Headers with { UserId = userId } };
                    }

                    if (TryGetMetadataId(metadata, nameof(Metadata.DeviceId), out Guid deviceId))
                    {
                        @event = @event with { Headers = @event.Headers with { DeviceId = deviceId } };
                    }

                    if (TryGetMetadataId(metadata, nameof(Metadata.MessageId), out Guid messageId))
                    {
                        @event = @event with { Headers = @event.Headers with { MessageId = messageId } };
                    }

                    if (TryGetMetadataId(metadata, nameof(Metadata.TransactionId), out Guid transactionId))
                    {
                        @event = @event with { Headers = @event.Headers with { TransactionId = transactionId } };
                    }

                    if (TryGetMetadataId(metadata, nameof(Metadata.CausationId), out Guid causationId))
                    {
                        @event = @event with { Headers = @event.Headers with { CausationId = causationId } };
                    }

                    events.Add(@event);
                }
                
            }
            while (!currentSlice.IsEndOfStream);

            return events.ToArray();
        }

        private static bool TryGetMetadataId(Dictionary<string, object> metadata, string key, out Guid id)
        {
            if (metadata != null && metadata.TryGetValue(key, out object? value) && value != null && Guid.TryParse(value.ToString(), out id))
            {
                return true;
            }

            id = Guid.Empty;

            return false;
        }

        internal Dictionary<string, object> DeserializeMetadata(byte[] metadata)
        {
            try
            {
                return (Dictionary<string, object>)Serializer.Deserialize(metadata, typeof(Dictionary<string, object>));
            }
            catch (SerializationException)
            {
                _logger?.Error($"Couldn't deserialize metadata. Are you missing an assembly reference, chaged an event or deleted it?");
                var original = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Couldn't deserialize metadata. Are you missing an assembly reference, chaged an event or deleted it?");
                Console.ForegroundColor = original;
                return null!;
            }
        }

        internal TroolioEvent? DeserializeStoredEvent(RecordedEvent @event)
        {
            try
            {
                Type eventType = Type.GetType(@event.EventType)!;

                if (eventType != null)
                {
                    return (TroolioEvent)Serializer.Deserialize(@event.Data, eventType);
                }
                else
                {
                    return null;
                }
            }
            catch (SerializationException)
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

        //TODO: Class to hook up to EventStore, as a database
        public async Task<StoreWriteResponse> Write(string streamName, ulong evVersion, ICollection<ITroolioEvent> events)
        {
            events = events.ToList();
            if (events.Count == 0)
            {
                return new StoreWriteResponse(0);
            }

            EventData[] serialized = events.Select(e => e is LinkEvent le ? 
                ToEventData(le.EventId, le, new Dictionary<string,object>()) : 
                ToEventData((TroolioEvent)e)
            ).ToArray();

            try
            {
                WriteResult result =  await ES.Connection!.AppendToStreamAsync(streamName, (long)evVersion - 1, serialized);
                return new StoreWriteResponse((ulong)events.Count);
            }
            catch (WrongExpectedVersionException)
            {
                throw new InvalidOperationException($"Duplicate activation of actor '{streamName}' detected");
            }
        }

        private EventData ToEventData(Guid eventId, object @event, IDictionary<string, object> headers)
        {
            // remove metadata from event body as they get inserted into metadata
            byte[] data = Serializer.Serialize(@event, new Dictionary<string, Type> { 
                { nameof(TroolioEvent.Headers), typeof(TroolioMessage) } 
            });

            byte[] metadata = Serializer.Serialize(headers);

            if (@event is LinkEvent l)
            {
                return new EventData(eventId, "$>", Serializer.IsJson, UTF8Encoding.ASCII.GetBytes(l.data), metadata);
            } 
            else
            {
                string eventTypeName = @event.GetType().AssemblyQualifiedName!;
                return new EventData(eventId, eventTypeName, Serializer.IsJson, data, metadata);
            }
            
        }

        protected internal EventData ToEventData(TroolioEvent @event)
        {
            IDictionary<string, object> headers = new Dictionary<string, object>()
            {
                { nameof(Metadata.CorrelationId), @event.Headers.CorrelationId },
                { nameof(Metadata.UserId), @event.Headers.UserId },
                { nameof(Metadata.DeviceId), @event.Headers.DeviceId },
                { nameof(Metadata.MessageId), @event.Headers.MessageId },
                { nameof(Metadata.TransactionId), @event.Headers.TransactionId! },
                { nameof(Metadata.CausationId), @event.Headers.CausationId! }
            };

            return ToEventData(@event.Headers.MessageId, @event, headers);
        }
    }
}
