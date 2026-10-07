using NUnit.Framework;
namespace Troolio.Tests;
[SetUpFixture]
public sealed class SetupFixture
{
    [OneTimeSetUp] public Task Start() => Setup.ActorSystemServer.Start();
    [OneTimeTearDown] public Task Stop() => Setup.ActorSystemServer.Shutdown();
}
