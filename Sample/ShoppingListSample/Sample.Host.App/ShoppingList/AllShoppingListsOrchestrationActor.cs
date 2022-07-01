using Orleans;
using Orleans.Concurrency;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Events;
using Sample.Shared.InternalCommands;
using Troolio.Core;

namespace Sample.Host.App.ShoppingList;

[RegexImplicitStreamSubscription($"{nameof(ShoppingListActor)}-.*")]
[Reentrant]
[StatelessWorker]
public class AllShoppingListsOrchestrationActor : OrchestrationActor
{
    public async Task On(EventEnvelope<NewListCreated> e)
    {
        await System.ActorOf<IAllShoppingListsActor>(Constants.SingletonActorId).Tell(new AddShoppingList(e.Event.Headers, Guid.Parse(e.Id)));
    }
}