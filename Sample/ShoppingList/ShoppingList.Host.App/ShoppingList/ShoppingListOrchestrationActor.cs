using Orleans.Concurrency;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Events;
using ShoppingList.Shared.InternalCommands;
using Troolio.Core;

namespace ShoppingList.Host.App.ShoppingList;

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