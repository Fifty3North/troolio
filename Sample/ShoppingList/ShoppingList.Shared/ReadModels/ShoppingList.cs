using ShoppingList.Shared.Events;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Core.Utilities;

namespace ShoppingList.Shared.ReadModels;

public record ShoppingListReadModel : TroolioReadModel
{
    public ShoppingListReadModel(Guid id)
        => Id = id;

    public override Func<Metadata, bool> Authorized => (metadata) => metadata.UserId == OwnerId || Collaborators.Contains(metadata.UserId);
    public Guid Id { get; }
    public string Title { get; private set; }
    public Guid OwnerId { get; private set; }
    public ImmutableArray<Guid> Collaborators { get; private set; } = ImmutableArray<Guid>.Empty;
    public ImmutableArray<ShoppingListItemReadModel> Items { get; private set; } = ImmutableArray<ShoppingListItemReadModel>.Empty;
    public string JoinCode { get; private set; }
    
    public ShoppingListReadModel On(EventEnvelope<NewListCreated> ev) 
        => this with { Title = ev.Event.Title, OwnerId = ev.Event.Headers.UserId };
    public ShoppingListReadModel On(EventEnvelope<ItemAddedToList> ev) 
        => this with { Items = Items.Add(new ShoppingListItemReadModel(ev.Event.ItemId).On(ev)) };
    public ShoppingListReadModel On(EventEnvelope<ItemRemovedFromList> ev) 
        => this with { Items = Items.RemoveAt( Items.FindIndex((i) => i.Id == ev.Event.ItemId)) };
    public ShoppingListReadModel On(EventEnvelope<ListJoined> ev) 
        => this with { Collaborators = Collaborators.Add(ev.Event.Headers.UserId) };
    public ShoppingListReadModel On(EventEnvelope<ListJoinedUsingCode> _) 
        => this;
    public ShoppingListReadModel On(EventEnvelope<ShoppingListAdded> ev) 
        => this with { JoinCode = ev.Event.JoinCode };
}
