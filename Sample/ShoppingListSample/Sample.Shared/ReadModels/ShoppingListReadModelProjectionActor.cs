using Orleans;
using Sample.Shared.Events;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Stores;

namespace Sample.Shared.ReadModels;

[RegexImplicitStreamSubscription("ShoppingListActor-.*")]
[RegexImplicitStreamSubscription("AllShoppingListsActor-.*")]
public class ShoppingListReadModelProjectionActor : ReadModelProjectionActor<ShoppingListReadModel>
{
    public ShoppingListReadModelProjectionActor(IStore store) : base(store) { }

    public string Handle(EventEnvelope<NewListCreated> ev) 
        => ev.Id.ToString();
    public string Handle(EventEnvelope<ItemAddedToList> ev) 
        => ev.Id.ToString();
    public string Handle(EventEnvelope<ItemRemovedFromList> ev) 
        => ev.Id.ToString();
    public string Handle(EventEnvelope<ListJoined> ev) 
        => ev.Id.ToString();
    public string Handle(EventEnvelope<ShoppingListAdded> ev) 
        => ev.Event.ListId.ToString();
}
