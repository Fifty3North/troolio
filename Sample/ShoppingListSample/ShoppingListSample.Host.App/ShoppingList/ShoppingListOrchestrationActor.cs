using Orleans.Concurrency;
using ShoppingListSample.Shared.ActorInterfaces;
using ShoppingListSample.Shared.Events;
using ShoppingListSample.Shared.InternalCommands;
using Troolio.Core;

namespace ShoppingListSample.Host.App.ShoppingList;

[OrchestrationStreamSubscription(nameof(AllShoppingListsActor))]
[Reentrant]
[StatelessWorker]
public class ShoppingListOrchestrationActor : OrchestrationActor
{
    async Task On(EventEnvelope<ListJoinedUsingCode> e)
    {
        await System.ActorOf<IShoppingListActor>(e.Event.ListId.ToString()).Tell(new JoinList(e.Event.Headers));
    }
}