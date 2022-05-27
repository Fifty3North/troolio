using Sample.Shared.Events;
using Troolio.Core;
using Troolio.Core.ReadModels;

namespace Sample.Shared.ReadModels;

public record ShoppingListItem : TroolioReadModel
{
    public ShoppingListItem(Guid id)
        => Id = id;
    public Guid Id { get; }
    public string Description { get; private set; }
    public ushort Quantity { get; private set; }
    public bool CrossedOff { get; private set; }

    public ShoppingListItem On(EventEnvelope<ItemCrossedOffList> _) 
        => this with { CrossedOff = true };
    public ShoppingListItem On(EventEnvelope<ItemAddedToList> ev) 
        => this with { Description = ev.Event.Description, Quantity = ev.Event.Quantity };
        
}
