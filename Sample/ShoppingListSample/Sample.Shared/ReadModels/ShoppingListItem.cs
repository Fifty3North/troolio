using Orleans;
using Sample.Shared.Events;
using Troolio.Core;
using Troolio.Core.ReadModels;

namespace Sample.Shared.ReadModels;

[GenerateSerializer]
public record ShoppingListItemReadModel : ITroolioReadModel
{
    public ShoppingListItemReadModel(Guid id)
        => Id = id;
    [System.Text.Json.Serialization.JsonIgnore]
    public Func<Metadata, bool> Authorized => _ => false;
    [Id(0)]
    public Guid Id { get; }
    [Id(1)]
    public string Description { get; private set; } = "";
    [Id(2)]
    public ushort Quantity { get; private set; }
    [Id(3)]
    public bool CrossedOff { get; private set; }

    public ShoppingListItemReadModel On(EventEnvelope<ItemCrossedOffList> _)
        => this with { CrossedOff = true };
    public ShoppingListItemReadModel On(EventEnvelope<ItemAddedToList> ev)
        => this with { Description = ev.Event.Description, Quantity = ev.Event.Quantity };

}
