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

        private readonly bool _preCreateEventTables = true;

        private static readonly TimeSpan[] _retryDurations = new[]
        {
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(1000),
            TimeSpan.FromMilliseconds(2000),
            TimeSpan.FromMilliseconds(5000),
            TimeSpan.FromMilliseconds(10000)
        };

        public MartenStore(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString(_connectionStringName);

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentException($"Connection string not found - ConnectionStrings:{_connectionStringName}");
            }

            _store = DocumentStore.For((options) => {
                options.Connection(_connectionString);
                options.DatabaseSchemaName = _schemaName;
                options.Events.StreamIdentity = Marten.Events.StreamIdentity.AsString;
                options.AutoCreateSchemaObjects = (_preCreateEventTables)
                    ? Weasel.Core.AutoCreate.None
                    : Weasel.Core.AutoCreate.All;
            });

            CreateDatabaseIfNotExists();

            if (_preCreateEventTables)
            {
                CreateEventTablesIfNotCreated();
            }
        }

        public async Task Clear()
        {
            await _store.Advanced.Clean.CompletelyRemoveAllAsync();

            if (_preCreateEventTables)
            {
                CreateEventTablesIfNotCreated();
            }
        }

        public async Task<ulong> Append(string streamName, ulong expectedEvVersion, ICollection<Core.IEvent> events)
        {
            long expectedVersionAfterAppend = (long)expectedEvVersion + events.Count;

            object[] eventsToStore = _wrapEvents
                ? events.Select(e => new EventWrapper(e)).ToArray()
                : events.ToArray();

            using (IDocumentSession session = _store.OpenSession())
            {
                session.Events.Append(streamName, expectedVersionAfterAppend, eventsToStore);

                try
                {
                    await RetrySessionAction(session, async (s) =>
                    {
                        await s.SaveChangesAsync();
                        return true;
                    });
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
            return await ReadStreamFromEvent(streamName, 0L);
        }

        public async Task<Core.IEvent[]> ReadStreamFromEvent(string streamName, ulong evVersion)
        {
            IReadOnlyList<Marten.Events.IEvent> streamEvents = await ReadStreamEvents(streamName, (long)evVersion);

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

        //public async Task<(Core.IEvent? Event, ulong Version)> ReadLastEvent(string streamName)
        //{
        //    using (IDocumentSession session = _store.OpenSession())
        //    {
        //        Marten.Events.IEvent? streamEvent = await RetrySessionAction(session, async (s) =>
        //        {
        //            return await s.Events.QueryAllRawEvents()
        //                .Where(e => e.StreamKey == streamName)
        //                .OrderByDescending(e => e.Version)
        //                .FirstOrDefaultAsync();
        //        });

        //        if (streamEvent != null)
        //        {
        //            Core.IEvent? @event = await DecodeEvent(streamEvent);

        //            return (@event, (ulong)streamEvent.Version);
        //        }

        //        return (null, 0);
        //    }
        //}

        // This gives better time performance compared with above especially when no events written to the stream
        public async Task<(Core.IEvent? Event, ulong Version)> ReadLastEvent(string streamName)
        {
            long version = 0;

            IReadOnlyList<Marten.Events.IEvent>? streamEvents = null;

            using (IDocumentSession session = _store.OpenSession())
            {
                try
                {
                    version = await RetrySessionAction(session, async (s) =>
                    {
                        Marten.Events.StreamState streamState = await s.Events.FetchStreamStateAsync(streamName);

                        return streamState?.Version ?? 0;
                    });
                }
                catch (Marten.Exceptions.MartenCommandException)
                {
                    // (troolio) Marten Events Schema and tables are not created until an event is inserted to a stream
                    // relation "troolio.mt_streams" does not exist
                }

                if (version != 0)
                {
                    streamEvents = await ReadStreamEvents(session, streamName, version, version);
                }
            }

            if (streamEvents != null && streamEvents.Count != 0)
            {
                Core.IEvent? @event = await DecodeEvent(streamEvents[0]);

                return (@event, (ulong)version);
            }

            return (null, 0);
        }

        public async Task<Core.IEvent?> ReadStreamEvent(string streamName, ulong evVersion)
        {
            long version = (long)evVersion;

            IReadOnlyList<Marten.Events.IEvent> streamEvents = await ReadStreamEvents(streamName, version, version);

            return (streamEvents.Count != 0) ? await DecodeEvent(streamEvents[0]) : null;
        }

        private async Task<IReadOnlyList<Marten.Events.IEvent>> ReadStreamEvents(string streamName, long fromVersion = 0L, long toVersion = 0L)
        {
            using (IDocumentSession session = _store.OpenSession())
            {
                return await ReadStreamEvents(session, streamName, fromVersion, toVersion);
            }
        }

        private async Task<IReadOnlyList<Marten.Events.IEvent>> ReadStreamEvents(IDocumentSession session, string streamName, long fromVersion = 0L, long toVersion = 0L)
        {
            return await RetrySessionAction(session, async (s) =>
            {
                return await s.Events.FetchStreamAsync(streamName, fromVersion: fromVersion, version: toVersion);
            });
        }

        private async Task<T> RetrySessionAction<T>(IDocumentSession session, Func<IDocumentSession, Task<T>> action)
        {
            for (int i = 0; i < _retryDurations.Length; i++)
            {
                try
                {
                    return await action(session);
                }
                catch (PostgresException ex)
                {
                    if (ex.SqlState == "53300")
                    {
                        System.Diagnostics.Debug.WriteLine($"Marten Store Error - Too many clients. Attempt: {i + 1}");

                        await Task.Delay(_retryDurations[i]);
                        continue;
                    }

                    throw;
                }
                catch (NpgsqlException ex)
                {
                    if (ex.Message.ToLower().Contains("connection pool"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Marten Store Error - Connection Pool exhausted. Attempt: {i + 1}");

                        await Task.Delay(_retryDurations[i]);
                        continue;
                    }

                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
            }

            throw new Exceptions.MaximumRetriesExceededException();
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
            const string postgres_db_name = "postgres";

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

        private void CreateEventTablesIfNotCreated()
        {
            using (IDocumentStore store = DocumentStore.For((option) =>
            {
                option.Connection(_connectionString);
                option.DatabaseSchemaName = _schemaName;
                option.Events.StreamIdentity = Marten.Events.StreamIdentity.AsString;
                option.AutoCreateSchemaObjects = Weasel.Core.AutoCreate.CreateOnly;
            }))
            {
                store.Storage.Database.EnsureStorageExists(typeof(Marten.Events.IEvent));
            }
        }

    }
}
