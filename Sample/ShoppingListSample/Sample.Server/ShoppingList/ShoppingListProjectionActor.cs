﻿using Orleankka;
using Orleans;
using Sample.Shared.ShoppingList;
using Troolio.Core;

namespace Sample.Server.ShoppingList
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

            await System.ActorOf<EmailActorMock>(Constants.SingletonActorId).Tell(new SendEmailNotification(e.Event.Headers, email, result.Title));
        }
    }
}
