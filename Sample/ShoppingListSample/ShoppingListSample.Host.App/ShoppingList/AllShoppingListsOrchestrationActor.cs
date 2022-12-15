using Orleans.Concurrency;
using ShoppingListSample.Shared.ActorInterfaces;
using ShoppingListSample.Shared.Events;
using ShoppingListSample.Shared.InternalCommands;
using Troolio.Core;

namespace ShoppingListSample.Host.App.ShoppingList;

[OrchestrationStreamSubscription(nameof(ShoppingListActor))]
[Reentrant]
[StatelessWorker]
public class AllShoppingListsOrchestrationActor : OrchestrationActor
{
    public async Task On(EventEnvelope<NewListCreated> e)
    {
        await System.ActorOf<IAllShoppingListsActor>(Constants.SingletonActorId).Tell(new AddShoppingList(e.Event.Headers, Guid.Parse(e.Id)));
    }
}