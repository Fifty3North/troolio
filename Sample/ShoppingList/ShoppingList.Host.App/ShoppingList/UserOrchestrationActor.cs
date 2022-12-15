using Orleans.Concurrency;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Events;
using ShoppingList.Shared.InternalCommands;
using Troolio.Core;

namespace ShoppingList.Host.App.ShoppingList;

[OrchestrationStreamSubscription(nameof(ShoppingListActor))]
[Reentrant]
[StatelessWorker]
public class UserOrchestrationActor : OrchestrationActor
{
    public async Task On(EventEnvelope<NewListCreated> e)
    {
        await System.ActorOf<IUserActor>(e.Event.Headers.UserId.ToString()).Tell(new RecordListId(e.Event.Headers, Guid.Parse(e.Id)));
    }
}