﻿using ShoppingList.Shared.Events;
using Troolio.Core;
using Troolio.Core.ReadModels;

namespace ShoppingList.Shared.ReadModels;

public record ShoppingListItemReadModel : TroolioReadModel
{
    public ShoppingListItemReadModel(Guid id)
    {
        Id = id;
    }
    public Guid Id { get; }
    public string Description { get; private set; }
    public ushort Quantity { get; private set; }
    public bool CrossedOff { get; private set; }

    public ShoppingListItemReadModel On(EventEnvelope<ItemCrossedOffList> _)
    {
        return this with { CrossedOff = true };
    }
    public ShoppingListItemReadModel On(EventEnvelope<ItemAddedToList> ev)
    {
        return this with { Description = ev.Event.Description, Quantity = ev.Event.Quantity };
    }
        
}
