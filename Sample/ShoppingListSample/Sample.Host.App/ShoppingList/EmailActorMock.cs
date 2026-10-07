using Orleans.Concurrency;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.InternalCommands;
using Troolio.Core;
namespace Sample.Host.App.ShoppingList;
[StatelessWorker]
public sealed class EmailActorMock : DispatchActor, IEmailActor
{
    // Teaching placeholder: no provider call or delivery guarantee.
    public Task Handle(SendEmailNotification command) => Task.CompletedTask;
}
