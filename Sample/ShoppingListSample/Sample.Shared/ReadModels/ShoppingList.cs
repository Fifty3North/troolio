using Sample.Shared.Events;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Core.Utilities;

namespace Sample.Shared.ReadModels;

public record ShoppingList : TroolioReadModel
{
    public ShoppingList(Guid id)
        => Id = id;

    public Guid Id { get; }
    public string Title { get; private set; }
    public Guid OwnerId { get; private set; }
    public ImmutableArray<Guid> Collaborators { get; private set; } = ImmutableArray<Guid>.Empty;
    public ImmutableArray<ShoppingListItem> Items { get; private set; } = ImmutableArray<ShoppingListItem>.Empty;
    public string JoinCode { get; private set; }
    
    public ShoppingList On(EventEnvelope<NewListCreated> ev) 
        => this with { Title = ev.Event.Title, OwnerId = ev.Event.Headers.UserId };
    public ShoppingList On(EventEnvelope<ItemAddedToList> ev) 
        => this with { Items = Items.Add(new ShoppingListItem(ev.Event.ItemId).On(ev)) };
    public ShoppingList On(EventEnvelope<ItemRemovedFromList> ev) 
        => this with { Items = Items.RemoveAt( Items.FindIndex((i) => i.Id == ev.Event.ItemId)) };
    public ShoppingList On(EventEnvelope<ListJoined> ev) 
        => this with { Collaborators = Collaborators.Add(ev.Event.Headers.UserId) };
    public ShoppingList On(EventEnvelope<ListJoinedUsingCode> _) 
        => this;
    public ShoppingList On(EventEnvelope<ShoppingListAdded> ev) 
        => this with { JoinCode = ev.Event.joinCode };
}
