﻿using Microsoft.Extensions.Configuration;
using ShoppingList.Shared.ActorInterfaces;
using ShoppingList.Shared.Commands;
using ShoppingList.Shared.Enums;
using ShoppingList.Shared.Events;
using ShoppingList.Shared.Exceptions;
using ShoppingList.Shared.InternalCommands;
using ShoppingList.Shared.Queries;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Creatable;
using Troolio.Stores;

namespace ShoppingList.Host.App;
public class ShoppingListActor : CreatableActor<ShoppingListState, CreateNewList>, IShoppingListActor
{
    public ShoppingListActor(IStore store, IConfiguration configuration) : base(store, configuration) 
    { 
    }

    #region Commands ...
    public IEnumerable<Event> Handle(AddItemToList command)
    {
        if (!(this.State.Author == command.UserId || this.State.Collaborators.Contains(command.UserId)))
        {
            throw new UnauthorizedAccessException();
        }

        // check
        if (this.State.Items.Any(i => i.Name == command.Description || i.Id == command.ItemId))
        {
            throw new ItemAlreadyExistsException();
        }

        yield return new ItemAddedToList(command.Headers, command.ItemId, command.Description, command.Quantity);
    }
    public IEnumerable<Event> Handle(CreateNewList command)
    {
        yield return new NewListCreated(command.Headers, command.UserId, command.Title);
    }
    public IEnumerable<Event> Handle(CrossItemOffList command)
    {
        if (!(this.State.Author == command.UserId || this.State.Collaborators.Contains(command.UserId)))
        {
            throw new UnauthorizedAccessException();
        }
        else if (!this.State.Items.Any((i) => i.Id == command.ItemId))
        {
            throw new ItemDoesNotExistException();
        }

        yield return new ItemCrossedOffList(command.Headers, command.ItemId);
    }
    public IEnumerable<Event> Handle(JoinList command)
    {
        if (this.State.Author == command.UserId)
        {
            throw new AuthorCannotJoinListException();
        }
        else if (this.State.Collaborators.Contains(command.UserId))
        {
            throw new UserHasAlreadyJoinedListException();
        }

        yield return new ListJoined(command.Headers, command.UserId);
    }
    public IEnumerable<Event> Handle(RemoveItemFromList command)
    {
        if (!(this.State.Author == command.UserId || this.State.Collaborators.Contains(command.UserId)))
        {
            throw new UnauthorizedAccessException();
        } 
        else if (this.State.Author != command.UserId)
        {
            throw new CollaboratorCannotRemoveItemFromListException();
        }

        if (!this.State.Items.Any((i) => i.Id == command.ItemId))
        {
            throw new ItemDoesNotExistException();
        }

        yield return new ItemRemovedFromList(command.Headers, command.ItemId);
    }
    #endregion

    #region Events ...
    public void On(ItemAddedToList ev) => State = State with { Items = State.Items.Add(new ShoppingListItemState(ev.ItemId, ev.Description, ItemState.Pending, ev.Quantity)) };
    public void On(ItemCrossedOffList ev)
    {
        var index = this.State.Items.FindIndex((i) => i.Id == ev.ItemId);
        State = State with
        {
            Items = State.Items.Replace(State.Items[index], State.Items[index] with
            {
                Status = ItemState.CrossedOff
            })
        };
    }
    public void On(ItemRemovedFromList ev) => State = State with { Items = State.Items.RemoveAt(this.State.Items.FindIndex((i) => i.Id == ev.ItemId)) };
    public void On(ListJoined ev) => State = State with { Collaborators = State.Collaborators.Add(ev.UserId) };
    public void On(NewListCreated ev) => this.State = new ShoppingListState(ev.UserId, ImmutableList<ShoppingListItemState>.Empty, ev.Title, ImmutableList<Guid>.Empty);
    #endregion

    #region Queries ...
    public ShoppingListQueryResult Handle(ShoppingListDetails _)
    {
        return new ShoppingListQueryResult(
            Guid.Parse(this.GrainReference.GrainIdentity.PrimaryKeyString),
            this.State.Title,
            this.State.Items.Select(
                i => new ShoppingItemQueryItem(
                    i.Id, i.Name, i.Status, i.Quantity
                )
            ).ToImmutableList(),
            this.State.Collaborators
         );
    }

    public async Task<string> Handle(GetJoinCode query)
    {
        if (this.State.Author != query.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        return await System.ActorOf<IAllShoppingListsActor>(Constants.SingletonActorId).Ask(new AuthorRequestedJoinCode(Guid.Parse(this.Id)));
    }
    #endregion
}
