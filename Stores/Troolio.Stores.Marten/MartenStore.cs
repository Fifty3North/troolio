using Marten;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Troolio.Stores
{
    // Event Wrapper for Events
    // Gets around issue whereby Events with the same name but different namespaces were not resolved correctly
    // They were getting the same mt_events.type set in the PostgresDB and not resolving actual type from mt_dotnet_type
    // Now ... The mt_events.type is the same (event_wrapper) for all the Events written but they seem to resolve correctly as per tests.
    internal record EventWrapper(Core.IEvent Event);

    public class MartenStore : IStore
    {
        private static readonly string _connectionStringName = "Marten";
        private static readonly string _schemaName = "troolio";

        private readonly string _connectionString;
        private readonly IDocumentStore _store;

        private readonly bool _wrapEvents = true;

        public MartenStore(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString(_connectionStringName);

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentException($"Connection string not found - ConnectionStrings:{_connectionStringName}");
            }

            _store = DocumentStore.For((storeOptions) => {
                storeOptions.Connection(_connectionString);
                storeOptions.DatabaseSchemaName = _schemaName;
                storeOptions.Events.StreamIdentity = Marten.Events.StreamIdentity.AsString;
                //storeOptions.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.All;
                storeOptions.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.CreateOrUpdate;
            });

            CreateDatabaseIfNotExists();
        }

        public async Task Clear()
        {
            await _store.Advanced.Clean.CompletelyRemoveAllAsync();
        }

        public async Task<ulong> Append(string streamName, ulong expectedEvVersion, ICollection<Core.IEvent> events)
        {
            using (IDocumentSession session = _store.OpenSession())
            {
                try
                {
                    long expectedVersionAfterAppend = (long)expectedEvVersion + events.Count;

                    if (_wrapEvents)
                    {
                        session.Events.Append(streamName, expectedVersionAfterAppend, events.Select(e => new EventWrapper(e)));
                    }
                    else
                    {
                        session.Events.Append(streamName, expectedVersionAfterAppend, events);
                    }

                    await session.SaveChangesAsync();
                }
                catch (Marten.Exceptions.EventStreamUnexpectedMaxEventIdException)
                {
                    throw new Exceptions.WrongExpectedVersionException($"Wrong Expected EventStore Event Version - Possible duplicate activation of actor '{streamName}' detected");
                }
            }

            return (ulong)events.Count;
        }

        public async Task<Core.IEvent[]> ReadStream(string streamName)
        {
            return await ReadStreamFromEvent(streamName, 0);
        }

        public async Task<Core.IEvent[]> ReadStreamFromEvent(string streamName, ulong evVersion)
        {
            using (IDocumentSession session = _store.OpenSession())
            {
                IReadOnlyList<Marten.Events.IEvent> streamEvents = await session.Events.FetchStreamAsync(streamName, fromVersion: (long)evVersion);

                if (streamEvents.Count == 0)
                {
                    return Array.Empty<Core.Event>();
                }

                List<Core.Event> events = new List<Core.Event>(streamEvents.Count);

                foreach (Marten.Events.IEvent streamEvent in streamEvents)
                {
                    Core.IEvent? @event = await DecodeEvent(streamEvent);

                    if (@event is Core.Event ev)
                    {
                        events.Add(ev);
                    }
                }

                return events.ToArray();
            }
        }

        public async Task<(Core.IEvent? Event, ulong Version)> ReadLastEvent(string streamName)
        {
            using (IDocumentSession session = _store.OpenSession())
            {
                long version = 0;

                try
                {
                    Marten.Events.StreamState streamState = await session.Events.FetchStreamStateAsync(streamName);

                    if (streamState != null)
                    {
                        version = streamState.Version;
                    }
                }
                catch (Marten.Exceptions.MartenCommandException)
                {
                    // (troolio) Marten Events Schema and tables are not created until an event is inserted to a stream
                    // relation "troolio.mt_streams" does not exist
                }

                if (version != 0)
                {
                    IReadOnlyList<Marten.Events.IEvent> streamEvents = await session.Events.FetchStreamAsync(streamName, fromVersion: version, version: version);

                    if (streamEvents.Count != 0)
                    {
                        Core.IEvent? @event = await DecodeEvent(streamEvents[0]);

                        return (@event, (ulong)version);
                    }
                }

                // This does not behave when running within Orleankka

                //Marten.Events.IEvent? streamEvent = session.Events.QueryAllRawEvents()
                //    .Where(e => e.StreamKey == streamName)
                //    .OrderByDescending(e => e.Version)
                //    .FirstOrDefault();

                //if (streamEvent != null)
                //{
                //    Core.IEvent? @event = await DecodeEvent(streamEvent);

                //    return (@event, (ulong)streamEvent.Version);
                //}

                return (null, 0);
            }
        }

        public async Task<Core.IEvent?> ReadStreamEvent(string streamName, ulong evVersion)
        {
            using (IDocumentSession session = _store.OpenSession())
            {
                long version = (long)evVersion;

                IReadOnlyList<Marten.Events.IEvent> streamEvents = await session.Events.FetchStreamAsync(streamName, fromVersion: version, version: version);

                return (streamEvents.Count != 0) ? await DecodeEvent(streamEvents[0]) : null;
            }
        }

        private async Task<Core.IEvent?> DecodeEvent(Marten.Events.IEvent streamEvent)
        {
            if (_wrapEvents)
            {
                if (streamEvent?.Data is EventWrapper eventWrapper)
                {
                    return await DecodeEventObject(eventWrapper.Event);
                }
            }
            else
            {
                return await DecodeEventObject(streamEvent?.Data);
            }

            return null;
        }

        private async Task<Core.IEvent?> DecodeEventObject(object? @event)
        {
            if (@event is Core.Event evnt)
            {
                return evnt;
            }
            else if (@event is Core.ReadModels.LinkEvent linkEvent)
            {
                return await ReadStreamEvent(linkEvent.StreamName, linkEvent.EventVersion);
            }

            return null;
        }

        private void CreateDatabaseIfNotExists()
        {
            string postgres_db_name = "postgres";

            NpgsqlConnectionStringBuilder connBuilder = new NpgsqlConnectionStringBuilder(_connectionString);

            if (connBuilder.Database == postgres_db_name)
            {
                return;
            }

            string marten_db_name = connBuilder.Database!;

            connBuilder.Database = postgres_db_name;

            using (NpgsqlConnection connection = new NpgsqlConnection(connBuilder.ConnectionString))
            {
                connection.Open();

                string existsSql = $"SELECT 1 FROM pg_catalog.pg_database WHERE datname = '{marten_db_name}';";

                using (NpgsqlCommand existsCommand = new NpgsqlCommand(existsSql, connection))
                {
                    object? result = existsCommand.ExecuteScalar();

                    if (result == null)
                    {
                        string createSql = $"CREATE DATABASE \"{marten_db_name}\" WITH OWNER = \"postgres\" ENCODING = 'UTF8' CONNECTION LIMIT = -1;";

                        using (NpgsqlCommand createCommand = new NpgsqlCommand(createSql, connection))
                        {
                            createCommand.ExecuteNonQuery();
                        }
                    }
                }

                connection.Close();
            }
        }
    }
}
