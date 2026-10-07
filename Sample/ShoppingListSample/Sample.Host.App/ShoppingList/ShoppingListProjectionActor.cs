using Orleans.Concurrency;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Events;
using Sample.Shared.InternalCommands;
using Sample.Shared.Queries;
using Troolio.Core;
using Troolio.Core.Reliable.Interfaces;
using Troolio.Core.Reliable.Messages;

namespace Sample.Host.App.ShoppingList
{
    /// <summary>
    /// Live projection example. The mock queue is volatile; external providers need durable idempotency.
    /// </summary>
    [ProjectionStreamSubscription(nameof(ShoppingListActor))]
    [Reentrant]
    public class ShoppingListProjectionActor : ProjectionActor
    {
        async Task On(EventEnvelope<ListJoined> e)
        {
            string email = "dummy@somewhere.com";

            ShoppingListQueryResult result = await System.ActorOf<IShoppingListActor>(e.Id).Ask<ShoppingListQueryResult>(new ShoppingListDetails(e.Event.Headers.UserId));

            var command = new SendEmailNotification(e.Event.Headers, email, result.Title);

            var actorPath = System.Worker<IEmailActor>().Path;

            await System.Worker<IBatchJobActor>()
                .Tell(new AddBatchJob(e.Event.Headers, actorPath, command));
        }
    }
}
