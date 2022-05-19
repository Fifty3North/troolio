﻿using Orleankka;
using Orleans;
using Orleans.Concurrency;
using Sample.Shared.ShoppingList;
using Troolio.Core;

namespace Sample.Server.ShoppingList;

[RegexImplicitStreamSubscription("ShoppingListActor-.*")]
[Reentrant]
[StatelessWorker]
public class UserOrchestrationActor : OrchestrationActor
{
    public async Task On(EventEnvelope<NewListCreated> e)
    {
        await System.ActorOf<IUserActor>(e.Event.Headers.UserId.ToString()).Tell(new RecordListId(e.Event.Headers, Guid.Parse(e.Id)));
    }
}