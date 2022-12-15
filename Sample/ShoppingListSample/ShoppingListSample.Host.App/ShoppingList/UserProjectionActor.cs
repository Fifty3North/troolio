using Orleans.Concurrency;
using ShoppingListSample.Shared.ActorInterfaces;
using ShoppingListSample.Shared.Events;
using ShoppingListSample.Shared.InternalCommands;
using Troolio.Core;
using Troolio.Core.Reliable.Interfaces;
using Troolio.Core.Reliable.Messages;

namespace ShoppingListSample.Host.App.ShoppingList;

[ProjectionStreamSubscription(nameof(ShoppingListActor))]
[Reentrant]
public class UserProjectionActor : ProjectionActor
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