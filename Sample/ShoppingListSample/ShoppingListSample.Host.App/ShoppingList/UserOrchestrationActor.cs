using Orleans.Concurrency;
using ShoppingListSample.Shared.ActorInterfaces;
using ShoppingListSample.Shared.Events;
using ShoppingListSample.Shared.InternalCommands;
using Troolio.Core;

namespace ShoppingListSample.Host.App.ShoppingList;

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