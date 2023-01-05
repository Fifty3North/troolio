using Orleans.Concurrency;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Events;
using ShoppingList.Shared.InternalCommands;
using Troolio.Core;

namespace ShoppingList.Host.App.ShoppingList;

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