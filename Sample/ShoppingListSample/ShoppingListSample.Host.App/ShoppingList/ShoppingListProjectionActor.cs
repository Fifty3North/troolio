using Orleans.Concurrency;
using ShoppingListSample.Shared.ActorInterfaces;
using ShoppingListSample.Shared.Events;
using ShoppingListSample.Shared.InternalCommands;
using ShoppingListSample.Shared.Queries;
using Troolio.Core;
using Troolio.Core.Reliable.Interfaces;
using Troolio.Core.Reliable.Messages;

namespace ShoppingListSample.Host.App.ShoppingList
{
    /// <summary>
    /// The ShoppingListProjection will only take place once a command is fully orchestrated successfully.  This needs 
    /// to be used for external systems as otherwise the external system can be notified of a command but the command 
    /// subsequently fails
    /// </summary>
    [ProjectionStreamSubscription(nameof(AllShoppingListsActor))]
    [Reentrant]
    public class ShoppingListProjectionActor : ProjectionActor
    {
        async Task On(EventEnvelope<ListJoinedUsingCode> e)
        {
            string email = "dummy@somewhere.com";

            ShoppingListQueryResult result = await System.ActorOf<IShoppingListActor>(e.Event.ListId.ToString()).Ask<ShoppingListQueryResult>(new ShoppingListDetails());

            var command = new SendEmailNotification(e.Event.Headers, email, result.Title);

            var actorPath = System.Worker<IEmailActor>().Path;

            await System.Worker<IBatchJobActor>()  
                .Tell(new AddBatchJob(actorPath, command));
        }
    }
}
