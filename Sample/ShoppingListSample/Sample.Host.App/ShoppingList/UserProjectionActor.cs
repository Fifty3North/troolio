using Orleans;
using Orleans.Concurrency;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Events;
using Sample.Shared.InternalCommands;
using Troolio.Core;
using Troolio.Core.Reliable.Interfaces;
using Troolio.Core.Reliable.Messages;

namespace Sample.Host.App.ShoppingList;

[RegexImplicitStreamSubscription($"{nameof(ShoppingListActor)}-.*")]
[Reentrant]
[StatelessWorker]
public class UserProjectionActor : ProjectionActor, IUserProjectionActor
{
    public async Task On(EventEnvelope<NewListCreated> e)
    {
        string email = "dummy@somewhere.com";

        var command = new SendEmailNotification(e.Event.Headers, email, "Example");

        var actorPath = System.Worker<IEmailActor>().Path;

        await System.Worker<IBatchJobActor>()
            .Tell(new AddBatchJob(actorPath, command));
    }
}