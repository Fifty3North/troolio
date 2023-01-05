﻿using Microsoft.Extensions.Configuration;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Events;
using ShoppingList.Shared.InternalCommands;
using ShoppingList.Shared.Queries;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Stateful;
using Troolio.Stores;

namespace ShoppingList.Host.App.ShoppingList;

public class UserActor : StatefulActor<UserState>, IUserActor
{
    public UserActor(IStore store, IConfiguration configuration) : base(store, configuration) 
    { 
        State = new UserState(ImmutableList<Guid>.Empty); 
    }

    #region Commands ...
    public IEnumerable<Event> Handle(RecordListId command) => new[] { new ListIdRecorded(command.ListId, command.Headers) };
    #endregion

    #region Events ...
    public void On(ListIdRecorded ev) => State = State with { Lists = State.Lists.Add(ev.ListId) };
    #endregion

    #region Queries ...
    public async Task<ImmutableList<ShoppingListQueryResult>> Handle(MyShoppingLists query)
    {
        return (
          await Task.WhenAll(
            this.State.Lists.Select(async (l) =>
                await this.System.ActorOf<IShoppingListActor>(l.ToString())
                    .Ask(new ShoppingListDetails())
            )
          )
        ).ToImmutableList<ShoppingListQueryResult>();
    }
    #endregion
}
