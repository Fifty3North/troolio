using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Diagnostics;

namespace Troolio.Stores.Tests
{
    [TestFixture]
    [Ignore("Requires Postgres running and takes approx 1 minute to run tests")]
    public class MartenStoreTests
    {
        private static readonly Random _rnd = new Random();

        private MartenStore _store;

        private static IList<int> ConcurrencyTestCases = new List<int> { 50, 250, 500, 1000, 2500, 5000 };

        [OneTimeSetUp]
        public async Task Setup()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _store = new MartenStore(configuration);

            await _store.Clear();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            Trace.Flush();

            await _store.Clear();
        }

        [Test]
        public async Task StoreCanWriteToStream()
        {
            string streamName = GetUniqueStreamName();
            Core.IEvent[] eventsToWrite = GetEventsToWrite(2);
            await _store.Append(streamName, 0L, eventsToWrite);
            Assert.Pass();
        }

        [Test]
        public async Task StoreCanReadFromStream()
        {
            string streamName = GetUniqueStreamName();
            Core.IEvent[] eventsToWrite = GetEventsToWrite(3);
            await _store.Append(streamName, 0L, eventsToWrite);

            Core.IEvent[] eventsRead = await _store.ReadStream(streamName);
            Assert.That(eventsToWrite.Length, Is.EqualTo(eventsRead.Length));

            for (int i = 0; i < eventsToWrite.Length; i++)
            {
                AssertThatEventReadMatchesExpected(eventsToWrite[i], eventsRead[i]);
            }
        }

        [Test]
        [TestCaseSource(nameof(ConcurrencyTestCases))]
        public async Task StoreCanWriteToMultipleStreamsConcurrently(int concurrentCount)
        {
            Debug.WriteLine($"Writing to {concurrentCount} streams concurrently...");

            (string StreamName, Core.IEvent[] Events)[] eventsToWrite = GetEventsToWriteToStreams(concurrentCount);

            Stopwatch sw = Stopwatch.StartNew();

            await Task.WhenAll(eventsToWrite.Select(async (ew) =>
            {
                await _store.Append(ew.StreamName, 0L, ew.Events);
            }));

            sw.Stop();

            Debug.WriteLine($"Written to {concurrentCount} streams concurrently in {sw.ElapsedMilliseconds} ms");

            foreach ((string streamName, Core.IEvent[] eventsWritten) in eventsToWrite)
            {
                Core.IEvent[] eventsRead = await _store.ReadStream(streamName);

                Assert.That(eventsWritten.Length, Is.EqualTo(eventsRead.Length));
            }
        }

        [Test]
        [TestCaseSource(nameof(ConcurrencyTestCases))]
        public async Task StoreCanReadFromMultipleEmptyStreamsConcurrently(int concurrentCount)
        {
            int[] results = await Task.WhenAll(Enumerable.Range(0, concurrentCount).Select(async (_) => {
                string streamName = GetUniqueStreamName();
                Core.IEvent[] eventsRead = await _store.ReadStream(streamName);
                return eventsRead.Length;
            }));

            foreach (int eventsRead in results)
            {
                Assert.Zero(eventsRead);
            }
        }

        [Test]
        [TestCaseSource(nameof(ConcurrencyTestCases))]
        public async Task StoreCanReadFromMultipleStreamsConcurrently(int concurrentCount)
        {
            (string StreamName, Core.IEvent[] Events)[] eventsToWrite = GetEventsToWriteToStreams(concurrentCount);

            foreach ((string streamName, Core.IEvent[] events) in eventsToWrite)
            {
                await _store.Append(streamName, 0L, events);
            }

            Debug.WriteLine($"Reading from {concurrentCount} streams concurrently...");

            Stopwatch sw = Stopwatch.StartNew();

            int[] results = await Task.WhenAll(eventsToWrite.Select(async (w) =>
            {
                Core.IEvent[] eventsRead = await _store.ReadStream(w.StreamName);

                return eventsRead.Length;
            }));

            sw.Stop();

            Debug.WriteLine($"Read from {concurrentCount} streams concurrently in {sw.ElapsedMilliseconds} ms.");

            Assert.That(results.Length, Is.EqualTo(eventsToWrite.Length));

            for (int i = 0; i < eventsToWrite.Length; i++)
            {
                Assert.That(eventsToWrite[i].Events.Length, Is.EqualTo(results[i]));
            }
        }

        [Test]
        [TestCaseSource(nameof(ConcurrencyTestCases))]
        public async Task StoreCanReadLastEventAsNullWhenNoEventsWrittenFromMultipleStreamsConcurrently(int concurrentCount)
        {
            Debug.WriteLine($"Reading last event from {concurrentCount} streams concurrently...");

            Stopwatch sw = Stopwatch.StartNew();

            Core.IEvent?[] results = await Task.WhenAll(Enumerable.Range(0, concurrentCount).Select(async (w) =>
            {
                string streamName = GetUniqueStreamName();

                (Core.IEvent? @event, _) = await _store.ReadLastEvent(streamName);

                return @event;
            }));

            sw.Stop();

            Debug.WriteLine($"Read last event from {concurrentCount} streams concurrently in {sw.ElapsedMilliseconds} ms.");

            foreach (Core.IEvent @event in results)
            {
                Assert.IsNull(@event);
            }
        }

        [Test]
        [TestCaseSource(nameof(ConcurrencyTestCases))]
        public async Task StoreCanReadLastEventFromMultipleStreamsConcurrently(int concurrentCount)
        {
            (string StreamName, Core.IEvent[] Events)[] eventsToWrite = GetEventsToWriteToStreams(concurrentCount);

            foreach ((string streamName, Core.IEvent[] events) in eventsToWrite)
            {
                await _store.Append(streamName, 0L, events);
            }

            Debug.WriteLine($"Reading last event from {concurrentCount} streams concurrently...");

            Stopwatch sw = Stopwatch.StartNew();

            Core.IEvent?[] results = await Task.WhenAll(eventsToWrite.Select(async (w) =>
            {
                (Core.IEvent? @event, _) = await _store.ReadLastEvent(w.StreamName);

                return @event;
            }));

            sw.Stop();

            Debug.WriteLine($"Read last event from {concurrentCount} streams concurrently in {sw.ElapsedMilliseconds} ms.");

            Assert.That(results.Length, Is.EqualTo(eventsToWrite.Length));

            for (int i = 0; i < eventsToWrite.Length; i++)
            {
                AssertThatEventReadMatchesExpected(eventsToWrite[i].Events.Last(), results[i]);
            }
        }

        [Test]
        [TestCaseSource(nameof(ConcurrencyTestCases))]
        public async Task StoreCanReadEventAtVersionFromMultipleStreamsConcurrently(int concurrentCount)
        {
            (string StreamName, Core.IEvent[] Events)[] eventsToWrite = GetEventsToWriteToStreams(concurrentCount);

            foreach ((string streamName, Core.IEvent[] events) in eventsToWrite)
            {
                await _store.Append(streamName, 0L, events);
            }

            Debug.WriteLine($"Reading event at version from {concurrentCount} streams concurrently...");

            Stopwatch sw = Stopwatch.StartNew();

            Core.IEvent?[] results = await Task.WhenAll(eventsToWrite.Select(async (w) =>
            {
                return await _store.ReadStreamEvent(w.StreamName, 3);
            }));

            sw.Stop();

            Debug.WriteLine($"Read event at version from {concurrentCount} streams concurrently in {sw.ElapsedMilliseconds} ms.");

            Assert.That(results.Length, Is.EqualTo(eventsToWrite.Length));

            for (int i = 0; i < eventsToWrite.Length; i++)
            {
                AssertThatEventReadMatchesExpected(eventsToWrite[i].Events[2], results[i]);
            }
        }

        #region Private Helper Methods

        private string GetUniqueStreamName()
        {
            return $"{nameof(Models.Foo.FooActor)}-{Guid.NewGuid()}";
        }

        private (string StreamName, Core.IEvent[] Events)[] GetEventsToWriteToStreams(int streamCount)
        {
            (string StreamName, Core.IEvent[] Events)[] eventsToWrite = Enumerable.Range(0, streamCount).Select((_) =>
            {
                string streamName = GetUniqueStreamName();
                Core.IEvent[] events = GetEventsToWrite((uint)_rnd.Next(4, 10));
                return (streamName, events);
            }).ToArray();

            return eventsToWrite;
        }

        private Core.IEvent[] GetEventsToWrite(uint count = 3)
        {
            return Enumerable.Range(0, (int)count).Select(i => {
                return (Core.IEvent)(
                    (i % 2 == 0)
                    ? new Models.Foo.NameUpdated(Guid.NewGuid().ToString().Substring(0, 8), GetHeaders())
                    : new Models.Foo.SizeUpdated((uint)_rnd.Next(0, 1000), GetHeaders())
                );
            }).ToArray();
        }

        private Core.Metadata GetHeaders()
        {
            return new Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        }

        private static void AssertThatEventReadMatchesExpected(Core.IEvent iEventWritten, Core.IEvent iEventRead)
        {
            Assert.IsNotNull(iEventRead);

            if (iEventWritten is Models.Foo.NameUpdated nameUpdated)
            {
                Models.Foo.NameUpdated? eventRead = iEventRead as Models.Foo.NameUpdated;
                Assert.IsNotNull(eventRead);
                Assert.That(nameUpdated.Name, Is.EqualTo(eventRead.Name));
                AssertThatEventHeadersMatchExpected(nameUpdated, eventRead);
            }
            else if (iEventWritten is Models.Foo.SizeUpdated sizeUpdated)
            {
                Models.Foo.SizeUpdated? eventRead = iEventRead as Models.Foo.SizeUpdated;
                Assert.IsNotNull(eventRead);
                Assert.That(sizeUpdated.Size, Is.EqualTo(eventRead.Size));
                AssertThatEventHeadersMatchExpected(sizeUpdated, eventRead);
            }
        }

        private static void AssertThatEventHeadersMatchExpected(Core.Event expected, Core.Event actual)
        {
            Assert.IsNotNull(actual.Headers);
            Assert.AreEqual(expected.Headers.CorrelationId, actual.Headers.CorrelationId);
            Assert.AreEqual(expected.Headers.UserId, actual.Headers.UserId);
            Assert.AreEqual(expected.Headers.DeviceId, actual.Headers.DeviceId);
            Assert.AreEqual(expected.Headers.MessageId, actual.Headers.MessageId);
        }

        #endregion
    }
}
