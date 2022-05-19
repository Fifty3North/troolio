using Orleankka;
using Orleans;
using Orleans.Concurrency;
using Sample.Shared.ShoppingList;
using Troolio.Core;

namespace Sample.Server.ShoppingList;

[RegexImplicitStreamSubscription("AllShoppingListsActor-.*")]
[Reentrant]
[StatelessWorker]
public class ShoppingListOrchestrationActor : OrchestrationActor
{
    async Task On(EventEnvelope<ListJoinedUsingCode> e)
    {
        await System.ActorOf<IShoppingListActor>(e.Event.ListId.ToString()).Tell(new JoinList(e.Event.Headers));
    }
}