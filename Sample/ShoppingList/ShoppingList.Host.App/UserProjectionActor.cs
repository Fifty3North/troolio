using Orleans.Concurrency;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Events;
using ShoppingList.Shared.InternalCommands;
using Troolio.Core;
using Troolio.Core.Reliable.Interfaces;
using Troolio.Core.Reliable.Messages;

namespace ShoppingList.Host.App.ShoppingList;

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