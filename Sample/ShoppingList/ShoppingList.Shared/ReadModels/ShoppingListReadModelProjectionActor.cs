using ShoppingList.Shared.Events;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Stores;

namespace ShoppingList.Shared.ReadModels;

[ProjectionStreamSubscription("ShoppingListActor")]
[ProjectionStreamSubscription("AllShoppingListsActor")]
public class ShoppingListReadModelProjectionActor : ReadModelProjectionActor<ShoppingListReadModel>
{
    public ShoppingListReadModelProjectionActor(IStore store) : base(store) 
	{
	}


    public string Handle(EventEnvelope<NewListCreated> ev)
    {
        return ev.Id.ToString();
    }
    public string Handle(EventEnvelope<ItemAddedToList> ev)
    {
        return ev.Id.ToString();
    }
    public string Handle(EventEnvelope<ItemRemovedFromList> ev)
    { 
        return ev.Id.ToString(); 
    }
    public string Handle(EventEnvelope<ListJoined> ev)
    {
        return ev.Id.ToString();
    }
    public string Handle(EventEnvelope<ShoppingListAdded> ev)
    {
        return ev.Event.ListId.ToString();
    }
}
