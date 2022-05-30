using Orleans;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Events;
using Sample.Shared.InternalCommands;
using Sample.Shared.Queries;
using Troolio.Core;

namespace Sample.Host.App.ShoppingList
{
    /// <summary>
    /// The ShoppingListProjection will only take place once a command is fully orchestrated successfully.  This needs 
    /// to be used for external systems as otherwise the external system can be notified of a command but the command 
    /// subsequently fails
    /// </summary>
    [RegexImplicitStreamSubscription("AllShoppingListsActor-.*")]
    public class ShoppingListProjectionActor : ProjectionActor
    {
        async Task On(EventEnvelope<ListJoinedUsingCode> e)
        {
            string email = "dummy@somewhere.com";

            ShoppingListQueryResult result = await System.ActorOf<IShoppingListActor>(e.Event.ListId.ToString()).Ask<ShoppingListQueryResult>(new ShoppingListDetails());

            await System.ActorOf<IEmailActor>(Constants.SingletonActorId).Tell(new SendEmailNotification(e.Event.Headers, email, result.Title));
        }
    }
}
