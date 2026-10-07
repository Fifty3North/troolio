using Orleans;
using Sample.Shared.Events;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Core.Utilities;

namespace Sample.Shared.ReadModels;

public sealed record ShoppingListReadModel : TroolioReadModel
{
    public ShoppingListReadModel(Guid id)
        => Id = id;

    [System.Text.Json.Serialization.JsonIgnore]
    public override Func<Metadata, bool> Authorized => (metadata) => metadata.UserId == OwnerId || Collaborators.Contains(metadata.UserId);
    public Guid Id { get; }
    public string Title { get; init; } = "";
    public Guid OwnerId { get; init; }
    public ImmutableArray<Guid> Collaborators { get; init; } = ImmutableArray<Guid>.Empty;
    public ImmutableArray<ShoppingListItemReadModel> Items { get; init; } = ImmutableArray<ShoppingListItemReadModel>.Empty;
    public string JoinCode { get; init; } = "";

    public ShoppingListReadModel On(EventEnvelope<NewListCreated> ev)
        => OwnerId != Guid.Empty ? this : this with { Title = ev.Event.Title, OwnerId = ev.Event.Headers.UserId, JoinCode = ev.Event.JoinCode };
    public ShoppingListReadModel On(EventEnvelope<ItemAddedToList> ev)
        => Items.Any(item => item.Id == ev.Event.ItemId) ? this : this with { Items = Items.Add(new ShoppingListItemReadModel(ev.Event.ItemId).On(ev)) };
    public ShoppingListReadModel On(EventEnvelope<ItemRemovedFromList> ev)
        => Items.Any(item => item.Id == ev.Event.ItemId) ? this with { Items = Items.RemoveAt(Items.FindIndex(item => item.Id == ev.Event.ItemId)) } : this;
    public ShoppingListReadModel On(EventEnvelope<ListJoined> ev)
        => Collaborators.Contains(ev.Event.Headers.UserId) ? this : this with { Collaborators = Collaborators.Add(ev.Event.Headers.UserId) };
    public ShoppingListReadModel On(EventEnvelope<ListJoinedUsingCode> _)
        => this;
    public ShoppingListReadModel On(EventEnvelope<ShoppingListAdded> ev)
        => this with { JoinCode = ev.Event.JoinCode };
    public ShoppingListReadModel On(EventEnvelope<ItemCrossedOffList> ev)
    {
        var item = Items.First(i => i.Id == ev.Event.ItemId);
        return this with { Items = Items.Replace(item, item.On(ev)) };
    }
}

[GenerateSerializer]
public struct ShoppingListSurrogate
{
    [Id(0)] public Guid Id;
    [Id(1)] public string Title;
    [Id(2)] public Guid OwnerId;
    [Id(3)] public ImmutableArray<Guid> Collaborators;
    [Id(4)] public ImmutableArray<ShoppingListItemReadModel> Items;
    [Id(5)] public string JoinCode;
}

[RegisterConverter]
public sealed class ShoppingListConverter : IConverter<ShoppingListReadModel, ShoppingListSurrogate>
{
    public ShoppingListReadModel ConvertFromSurrogate(in ShoppingListSurrogate value)
        => new(value.Id) { Title = value.Title, OwnerId = value.OwnerId, Collaborators = value.Collaborators, Items = value.Items, JoinCode = value.JoinCode };
    public ShoppingListSurrogate ConvertToSurrogate(in ShoppingListReadModel value)
        => new() { Id = value.Id, Title = value.Title, OwnerId = value.OwnerId, Collaborators = value.Collaborators, Items = value.Items, JoinCode = value.JoinCode };
}
