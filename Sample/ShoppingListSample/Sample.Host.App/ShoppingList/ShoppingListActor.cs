using Microsoft.Extensions.Configuration;
using Sample.Shared.ShoppingList;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Creatable;
using Troolio.Stores;

namespace Sample.Host.App.ShoppingList;

public class ShoppingListActor : CreatableActor<ShoppingListState, CreateNewList>, IShoppingListActor
{
    public ShoppingListActor(IStore store, IConfiguration configuration) : base(store, configuration) { }

    #region Commands ...
    public IEnumerable<TroolioEvent> Handle(AddItemToList command)
    {
        if (!(this.State.Author == command.Headers.UserId || this.State.Collaborators.Contains(command.Headers.UserId)))
        {
            throw new UnauthorizedAccessException();
        }

        // check
        if (this.State.Items.Any(i => i.Name.ToLower() == command.payload.Description))
        {
            throw new ItemAlreadyExistsException();
        }

        yield return new ItemAddedToList(Guid.NewGuid(), command.payload.Description, command.payload.Quantity, command.Headers);
    }
    public IEnumerable<TroolioEvent> Handle(CreateNewList command) => new[] { new NewListCreated(command.payload.Title, command.Headers) };
    public IEnumerable<TroolioEvent> Handle(CrossItemOffList command)
    {
        if (!(this.State.Author == command.Headers.UserId || this.State.Collaborators.Contains(command.Headers.UserId)))
        {
            throw new UnauthorizedAccessException();
        }
        else if (!this.State.Items.Any((i) => i.Id == command.payload.ItemId))
        {
            throw new ItemDoesNotExistException();
        }

        yield return new ItemCrossedOffList(command.payload.ItemId, command.Headers);
    }
    public IEnumerable<TroolioEvent> Handle(JoinList command)
    {
        if (this.State.Author == command.Headers.UserId)
        {
            throw new AuthorCannotJoinListException();
        }
        else if (this.State.Collaborators.Contains(command.Headers.UserId))
        {
            throw new UserHasAlreadyJoinedListException();
        }

        yield return new ListJoined(command.Headers);
    }
    public IEnumerable<TroolioEvent> Handle(RemoveItemFromList command)
    {
        if (!(this.State.Author == command.Headers.UserId || this.State.Collaborators.Contains(command.Headers.UserId)))
        {
            throw new UnauthorizedAccessException();
        } 
        else if (this.State.Author != command.Headers.UserId)
        {
            throw new CollaboratorCannotRemoveItemFromListException();
        }

        if (!this.State.Items.Any((i) => i.Id == command.payload.ItemId))
        {
            throw new ItemDoesNotExistException();
        }

        yield return new ItemRemovedFromList(command.payload.ItemId, command.Headers);
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
    public void On(ListJoined ev) => State = State with { Collaborators = State.Collaborators.Add(ev.Headers.UserId) };
    public void On(NewListCreated ev) => this.State = new ShoppingListState(ev.Headers.UserId, ImmutableList<ShoppingListItemState>.Empty, ev.Title, ImmutableList<Guid>.Empty);
    #endregion

    #region Queries ...
    public ShoppingListQueryResult Handle(ShoppingListDetails query)
    {
        return new ShoppingListQueryResult(
            Guid.Parse(this.GrainReference.GrainIdentity.PrimaryKeyString),
            this.State.Title,
            this.State.Items.Select(
                i => new ShoppingItemQueryItem(
                    i.Id, i.Name, (ShoppingItemQueryItemState)i.Status, i.Quantity
                )
            ).ToImmutableList(),
            this.State.Collaborators
         );
    }
    #endregion
}
