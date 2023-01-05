﻿using Microsoft.Extensions.Configuration;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Commands;
using ShoppingList.Shared.Events;
using ShoppingList.Shared.Exceptions;
using ShoppingList.Shared.InternalCommands;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Stateful;
using Troolio.Stores;

namespace ShoppingList.Host.App;

public class AllShoppingListsActor : StatefulActor<AllShoppingListsState>, IAllShoppingListsActor
{
    
    public AllShoppingListsActor(IStore store, IConfiguration configuration) : base(store, configuration)
    { 
         State = new AllShoppingListsState(ImmutableDictionary<string, Guid>.Empty);
    }

    #region Commands ...
    public IEnumerable<Event> Handle(AddShoppingList command)
    {
        // Generate code
        // TODO - *** This needs to be a core field type, "AutogeneratedString4" ***
        string joinCode = Guid.NewGuid().ToString()[24..];

        yield return new ShoppingListAdded(command.Headers, command.ListId, joinCode);
    }
    public IEnumerable<Event> Handle(JoinListUsingCode command)
    {
        if (command.Code == null || !this.State.Lists.ContainsKey(command.Code))
        {
            throw new InvalidJoinCodeException();
        }

        yield return new ListJoinedUsingCode(command.Headers, command.UserId, this.State.Lists[command.Code]);
    }
    #endregion

    #region Events ...
    public void On(ListJoinedUsingCode _)  // Stub this non-state changing event
    { 
    }
    public void On(ShoppingListAdded ev)
    {
        this.State = this.State with { Lists = State.Lists.Add(ev.JoinCode, ev.ListId) };
    }
    #endregion

    #region Queries 
    internal string On(AuthorRequestedJoinCode query)
    {
        return State.Lists.FirstOrDefault(l => l.Value == query.ListId).Key;
    }
    #endregion
}
