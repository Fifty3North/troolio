using NUnit.Framework;
using Troolio.Core.Serialization;
using Troolio.Stores;

namespace Troolio.Tests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            IEventSerializer eventSerializer = new JsonEventSerializer(null);
            IStore store = new FileSystemStore(eventSerializer);
            await store.Clear();
            await Troolio.Tests.Setup.ActorSystemServer.Start();
        }

        [OneTimeTearDown]
        public async Task Shutdown()
        {
            await Troolio.Tests.Setup.ActorSystemServer.Shutdown();
        }
    }
}
