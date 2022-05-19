using NUnit.Framework;
using Troolio.Stores;

namespace Troolio.Tests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            IStore store = new FileSystemStore();
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
