using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net.Sockets;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Core.Serialization;
using Troolio.Stores.Tests.Models.Foo;

namespace Troolio.Stores.Tests
{
    [TestFixture]
    internal class StoreTests
    {
        private static IList<IStore> StoreTestCases;

        private static readonly string LOCALHOST = "127.0.0.1";
        private static readonly int EVENTSTORE_PORT = 1113;
        private static readonly int POSTGRES_PORT = 5432;

        private readonly Random _random = new();

        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _deviceId = Guid.NewGuid();

        static StoreTests()
        {
            ILogger<JsonEventSerializer> serializerLogger = LoggerFactory.Create(b => b.AddConsole())
                .CreateLogger<JsonEventSerializer>();

            IEventSerializer eventSerializer = new JsonEventSerializer(serializerLogger);

            StoreTestCases = new List<IStore>()
            {
                new InMemoryStore(eventSerializer),
                new FileSystemStore(eventSerializer)
            };

#if DEBUG
            // Marten
            // docker run -d -e POSTGRES_PASSWORD=abcd@1234 -p 5432:5432 postgres
            if (IsLocalPortConnectable(POSTGRES_PORT))
            {
                try
                {
                    IConfiguration configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

                    MartenStore martenStore = new MartenStore(configuration);

                    StoreTestCases.Add(martenStore);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating Marten Store: {ex.Message}");
                }
            }

            // Event Store
            // docker run -d -e EVENTSTORE_INSECURE=true -e EVENTSTORE_ENABLE_EXTERNAL_TCP=true -p 1113:1113 -d eventstore/eventstore
            if (IsLocalPortConnectable(EVENTSTORE_PORT))
            {
                try
                {
                    EventStore.ES es = new EventStore.ES(false, EVENTSTORE_PORT, LOCALHOST);

                    _= es.Connect();

                    ESStore eventStore = new ESStore(eventSerializer, null!);

                    StoreTestCases.Add(eventStore);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating Event Store: {ex.Message}");
                }
            }
#endif
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            await TryClearStores(StoreTestCases);
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await TryClearStores(StoreTestCases);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanReadNoEventsWhenNoEventsWrittenToStream(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] eventsRead = await store.ReadStream(streamName);

            Assert.IsNotNull(eventsRead);
            Assert.AreEqual(0, eventsRead.Length);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanWriteAndReadEvents(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] events = new IEvent[]
            {
                new NameUpdated("Bob", GetHeaders()),
                new SizeUpdated(12, GetHeaders())
            };

            ulong expectedVersion = 0;

            ulong storedCount = await store.Append(streamName, expectedVersion, events);

            Assert.AreEqual(events.Length, (int)storedCount);

            IEvent[] eventsRead = await store.ReadStream(streamName);

            Assert.AreEqual(events.Length, eventsRead.Length);

            AssertThatNameUpdatedEventMatchesExpected(events[0] as NameUpdated, eventsRead[0]);
            AssertThatSizeUpdatedEventMatchesExpected(events[1] as SizeUpdated, eventsRead[1]);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanAppendEventsToExistingStream(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] initialEvents = new IEvent[]
            {
                new NameUpdated("Bob", GetHeaders()),
                new SizeUpdated(12, GetHeaders())
            };

            ulong expectedVersion = 0;

            await store.Append(streamName, expectedVersion, initialEvents);

            IEvent[] subsequentEvents = new IEvent[]
            {
                new NameUpdated("Kevin", GetHeaders()),
                new NameUpdated("Stuart", GetHeaders()),
                new SizeUpdated(14, GetHeaders())
            };

            expectedVersion = (ulong)initialEvents.Length;

            ulong storedCount = await store.Append(streamName, expectedVersion, subsequentEvents);

            IEvent[] eventsRead = await store.ReadStream(streamName);

            Assert.AreEqual(initialEvents.Length + subsequentEvents.Length, eventsRead.Length);

            AssertThatNameUpdatedEventMatchesExpected(initialEvents[0] as NameUpdated, eventsRead[0]);
            AssertThatSizeUpdatedEventMatchesExpected(initialEvents[1] as SizeUpdated, eventsRead[1]);
            AssertThatNameUpdatedEventMatchesExpected(subsequentEvents[0] as NameUpdated, eventsRead[2]);
            AssertThatNameUpdatedEventMatchesExpected(subsequentEvents[1] as NameUpdated, eventsRead[3]);
            AssertThatSizeUpdatedEventMatchesExpected(subsequentEvents[2] as SizeUpdated, eventsRead[4]);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanReadEventsFromVersion(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] events = new IEvent[]
            {
                new NameUpdated("Bob", GetHeaders()),
                new NameUpdated("Kevin", GetHeaders()),
                new SizeUpdated(12, GetHeaders())
            };

            ulong expectedVersion = 0;

            await store.Append(streamName, expectedVersion, events);

            IEvent[] eventsRead = await store.ReadStreamFromEvent(streamName, 2);

            Assert.AreEqual(2, eventsRead.Length);

            AssertThatNameUpdatedEventMatchesExpected(events[1] as NameUpdated, eventsRead[0]);
            AssertThatSizeUpdatedEventMatchesExpected(events[2] as SizeUpdated, eventsRead[1]);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanReadNoEventsFromVersionWhenNoEventsWrittenToStream(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] eventsRead = await store.ReadStreamFromEvent(streamName, 2);

            Assert.IsNotNull(eventsRead);
            Assert.AreEqual(0, eventsRead.Length);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanReadLastEvent(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] events = new IEvent[]
            {
                new NameUpdated("Bob", GetHeaders()),
                new NameUpdated("Kevin", GetHeaders()),
                new SizeUpdated(12, GetHeaders())
            };

            ulong expectedVersion = 0;

            await store.Append(streamName, expectedVersion, events);

            (IEvent? @event, ulong version) = await store.ReadLastEvent(streamName);

            Assert.IsNotNull(@event);
            Assert.AreEqual(3, version);

            AssertThatSizeUpdatedEventMatchesExpected(events[2] as SizeUpdated, @event);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanReadLastEventAsNullIfNoEventsWritten(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            (IEvent? @event, ulong version) = await store.ReadLastEvent(streamName);

            Assert.IsNull(@event);
            Assert.AreEqual(0, version);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanReadEventAtVersion(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] events = new IEvent[]
            {
                new NameUpdated("Bob", GetHeaders()),
                new NameUpdated("Kevin", GetHeaders()),
                new SizeUpdated(12, GetHeaders())
            };

            ulong expectedVersion = 0;

            await store.Append(streamName, expectedVersion, events);

            IEvent? @event = await store.ReadStreamEvent(streamName, 2);

            Assert.IsNotNull(@event);

            AssertThatNameUpdatedEventMatchesExpected(events[1] as NameUpdated, @event);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanReadEventAtVersionAsNullIfNoEventsWritten(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent? @event = await store.ReadStreamEvent(streamName, 2);

            Assert.IsNull(@event);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanClearEvents(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] events = new IEvent[]
            {
                new NameUpdated("Bob", GetHeaders()),
                new NameUpdated("Kevin", GetHeaders()),
                new SizeUpdated(12, GetHeaders())
            };

            ulong expectedVersion = 0;

            await store.Append(streamName, expectedVersion, events);

            if (store is ESStore)
            {
                try
                {
                    await store.Clear();
                }
                catch
                {
                    Assert.Warn("Known issue that ESStore does not implement IStore.Clear()");
                }
            }
            else
            {
                await store.Clear();

                IEvent[] eventsRead = await store.ReadStream(streamName);

                Assert.AreEqual(0, eventsRead.Length);
            }
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanResolveLinkEvents(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] events = new IEvent[]
            {
                new NameUpdated("Bob", GetHeaders()),
                new SizeUpdated(12, GetHeaders())
            };

            string readModelStreamName = $"{nameof(FooReadModel)}-{actorId}";

            IEvent[] linkEvents = new IEvent[]
            {
                new LinkEvent(Guid.NewGuid(), 1, streamName),
                new LinkEvent(Guid.NewGuid(), 2, streamName),
            };

            await store.Append(streamName, 0, events);
            await store.Append(readModelStreamName, 0, linkEvents);

            IEvent[] eventsRead = await store.ReadStream(readModelStreamName);

            Assert.AreEqual(linkEvents.Length, eventsRead.Length);

            AssertThatNameUpdatedEventMatchesExpected(events[0] as NameUpdated, eventsRead[0]);
            AssertThatSizeUpdatedEventMatchesExpected(events[1] as SizeUpdated, eventsRead[1]);
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreThrowsKnownExceptionWhenAppendingEventsWithWrongExpectedVersion(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            IEvent[] initialEvents = new IEvent[]
            {
                new NameUpdated("Bob", GetHeaders()),
                new SizeUpdated(12, GetHeaders())
            };

            ulong expectedVersion = 0;

            await store.Append(streamName, expectedVersion, initialEvents);

            IEvent[] subsequentEvents = new IEvent[]
            {
                new NameUpdated("Kevin", GetHeaders()),
                new NameUpdated("Stuart", GetHeaders()),
                new SizeUpdated(14, GetHeaders())
            };

            expectedVersion = (ulong)initialEvents.Length - 1;

            Assert.ThrowsAsync<Exceptions.WrongExpectedVersionException>(async () => await store.Append(streamName, expectedVersion, subsequentEvents));

            expectedVersion = (ulong)initialEvents.Length + 1;

            Assert.ThrowsAsync<Exceptions.WrongExpectedVersionException>(async () => await store.Append(streamName, expectedVersion, subsequentEvents));
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreCanWriteToMultipleStreamsConcurrently(IStore store)
        {
            (string, ulong)[] results = await Task.WhenAll(Enumerable.Range(0, 5).Select(async (i) =>
                await WriteSomeEventsToStore(store)));

            foreach ((string streamName, ulong eventsStored) in results)
            {
                Assert.IsTrue(eventsStored > 0);

                IEvent[] eventsRead = await store.ReadStream(streamName);

                Assert.AreEqual(eventsStored, eventsRead.Length);
            }
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreResolvesEventTypesCorrectlyForEventsWithSameName(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"ACreatableActor-{actorId}";

            Metadata headers = GetHeaders();

            IEvent[] initialEvents = new IEvent[]
            {
                new Core.Creatable.Events.ActorCreated(headers),
                new Models.ActorCreated(DateTime.UtcNow, headers)
            };

            await store.Append(streamName, 0, initialEvents);

            IEvent[] eventsRead = await store.ReadStream(streamName);

            Assert.AreEqual(2, eventsRead.Length);

            Assert.IsTrue(eventsRead[0] is Core.Creatable.Events.ActorCreated, $"Expected: {typeof(Troolio.Core.Creatable.Events.ActorCreated).FullName}");
            Assert.IsTrue(eventsRead[1] is Models.ActorCreated, $"Expected: {typeof(Models.ActorCreated).FullName}");
        }

        [Test]
        [TestCaseSource(nameof(StoreTestCases))]
        public async Task StoreResolvesEventTypesCorrectlyForEventsWithSameNameOnDifferentStreams(IStore store)
        {
            Guid actorId1 = Guid.NewGuid();
            string streamName1 = $"{nameof(FooActor)}-{actorId1}";

            Guid actorId2 = Guid.NewGuid();
            string streamName2 = $"{nameof(Models.Bar.BarActor)}-{actorId2}";

            IEvent[] events1 = new IEvent[]
            {
                new Models.Foo.NameUpdated("Bob", GetHeaders()),
            };

            IEvent[] events2 = new IEvent[]
            {
                new Models.Bar.NameUpdated("Kevin", GetHeaders())
            };

            await store.Append(streamName1, 0, events1);
            await store.Append(streamName2, 0, events2);

            IEvent[] eventsRead1 = await store.ReadStream(streamName1);
            IEvent[] eventsRead2 = await store.ReadStream(streamName2);

            Assert.IsTrue(eventsRead1[0] is Models.Foo.NameUpdated, $"Expected: {typeof(Models.Foo.NameUpdated).FullName}");
            Assert.IsTrue(eventsRead2[0] is Models.Bar.NameUpdated, $"Expected: {typeof(Models.Bar.NameUpdated).FullName}");
        }

        #region Private Helper Methods

        private Metadata GetHeaders()
        {
            return new Metadata(Guid.NewGuid(), _userId, _deviceId);
        }

        private async Task<(string StreamName, ulong EventsWritten)> WriteSomeEventsToStore(IStore store)
        {
            Guid actorId = Guid.NewGuid();
            string streamName = $"{nameof(FooActor)}-{actorId}";

            List<IEvent> events = new List<IEvent>();

            for (int i = 0; i < _random.Next(4, 10); i++)
            {
                events.Add(new NameUpdated(Guid.NewGuid().ToString().Substring(0, 8), GetHeaders()));
            }

            ulong storedCount = await store.Append(streamName, 0, events);

            return (streamName, storedCount);
        }

        private static void AssertThatNameUpdatedEventMatchesExpected(NameUpdated expected, IEvent? actual)
        {
            Assert.IsNotNull(actual);
            NameUpdated? actualEvent = actual as NameUpdated;
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(expected.Name, actualEvent!.Name);
            AssertThatEventHeadersMatchExpected(expected, actualEvent);
        }

        private static void AssertThatSizeUpdatedEventMatchesExpected(SizeUpdated expected, IEvent? actual)
        {
            Assert.IsNotNull(actual);
            SizeUpdated? actualEvent = actual as SizeUpdated;
            Assert.IsNotNull(actualEvent);
            Assert.AreEqual(expected.Size, actualEvent!.Size);
            AssertThatEventHeadersMatchExpected(expected, actualEvent);
        }

        private static void AssertThatEventHeadersMatchExpected(Event expected, Event actual)
        {
            Assert.IsNotNull(actual.Headers);
            Assert.AreEqual(expected.Headers.CorrelationId, actual.Headers.CorrelationId);
            Assert.AreEqual(expected.Headers.UserId, actual.Headers.UserId);
            Assert.AreEqual(expected.Headers.DeviceId, actual.Headers.DeviceId);
            Assert.AreEqual(expected.Headers.MessageId, actual.Headers.MessageId);
        }

        private static async Task TryClearStores(IEnumerable<IStore> stores)
        {
            if (stores != null)
            {
                foreach (IStore store in stores)
                {
                    try
                    {
                        await store.Clear();
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool IsLocalPortConnectable(int port)
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    socket.Connect(LOCALHOST, port);
                    return true;
                }
                catch (SocketException)
                {
                }
                return false;
            }
        }

        #endregion
    }
}
