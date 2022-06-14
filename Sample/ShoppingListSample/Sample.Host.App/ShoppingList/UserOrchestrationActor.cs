using Orleans;
using Orleans.Concurrency;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Events;
using Sample.Shared.InternalCommands;
using Troolio.Core;
using Troolio.Core.Reliable.Interfaces;
using Troolio.Core.Reliable.Messages;

namespace Sample.Host.App.ShoppingList;

[RegexImplicitStreamSubscription("ShoppingListActor-.*")]
[Reentrant]
[StatelessWorker]
public class UserOrchestrationActor : OrchestrationActor
{
    public async Task On(EventEnvelope<NewListCreated> e)
    {
        await System.ActorOf<IUserActor>(e.Event.Headers.UserId.ToString()).Tell(new RecordListId(e.Event.Headers, Guid.Parse(e.Id)));

        string email = "dummy@somewhere.com";

        var command = new SendEmailNotification(e.Event.Headers, email, "Example");

        await System.ActorOf<IBatchJobActor>(Constants.SingletonActorId)
            .Tell(new AddBatchJob(Constants.SingletonActorId, command));

    }
}