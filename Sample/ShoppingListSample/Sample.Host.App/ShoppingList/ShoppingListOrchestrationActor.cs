using Orleankka;
using Orleans;
using Orleans.Concurrency;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Events;
using Sample.Shared.InternalCommands;
using Troolio.Core;

namespace Sample.Host.App.ShoppingList;

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