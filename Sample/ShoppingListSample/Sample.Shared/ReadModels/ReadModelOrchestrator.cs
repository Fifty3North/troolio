using Orleankka;
using Orleans;
using Sample.Shared.Events;
using Troolio.Core;
using Troolio.Core.ReadModels;
using Troolio.Stores;

namespace Sample.Shared.ReadModels
{
    public interface IShoppingReadModelOrchestrator : IActorGrain, IGrainWithStringKey { }

    [RegexImplicitStreamSubscription("ShoppingListActor-.*")]
    [RegexImplicitStreamSubscription("AllShoppingListsActor-.*")]
    public class ShoppingReadModelOrchestrator 
        : ReadModelOrchestrator<ShoppingList>, IShoppingReadModelOrchestrator, IGrainWithGuidCompoundKey
    {
        public ShoppingReadModelOrchestrator(IStore store) : base(store) { }

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
}
