using NUnit.Framework;

namespace Troolio.Tests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            await Tests.Setup.ActorSystemServer.Start();
        }

        [OneTimeTearDown]
        public async Task Shutdown()
        {
            await Tests.Setup.ActorSystemServer.Shutdown();
        }
    }
}
