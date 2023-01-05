using Omu.ValueInjecter;
using Orleans.Concurrency;
using ShoppingList.Host.App;
using ShoppingList.Shared.Enums;
using ShoppingList.Shared.Events;
using Troolio.Core;
using Troolio.Core.Projection;

namespace ShoppingList.Database.Projection
{
    [ProjectionStreamSubscription(nameof(ShoppingListActor))]
    [Reentrant]
    public class ShoppingListItemEFProjection : EntityFrameworkBatchedProjection<Model.ShoppingListItem, Model.ShoppingListsDbContext>, Shared.ActorInterfaces.IShoppingListItemEFProjection
    {
        protected override void SetupMappings()
        {
            Mapper.AddMap<EventEnvelope<ItemAddedToList>, Task<EventEntityCreate<Model.ShoppingListItem>>>(src =>
            {
                Guid listId = Guid.Parse(src.Id);

                var item = new Model.ShoppingListItem()
                {
                    ShoppingListId = listId,
                    Id = src.Event.ItemId,
                    Name = src.Event.Description,
                    Status = ItemState.Pending,
                    Quantity = src.Event.Quantity
                };

                return Task.FromResult(new EventEntityCreate<Model.ShoppingListItem>(src.Event.ItemId, item));
            });

            Mapper.AddMap<EventEnvelope<ItemCrossedOffList>, Task<EventEntityUpdate<Model.ShoppingListItem>>>(src =>
            {
                Guid itemId = src.Event.ItemId;
                Action<Model.ShoppingListItem>[] actions = new Action<Model.ShoppingListItem>[]
                {
                    a => a.Status = ItemState.CrossedOff
                };

                return Task.FromResult(new EventEntityUpdate<Model.ShoppingListItem>(itemId, actions));
            });

            Mapper.AddMap<EventEnvelope<ItemRemovedFromList>, Task<EventEntityDelete<Model.ShoppingListItem>>>(src =>
            {
                Guid itemId = src.Event.ItemId;

                return Task.FromResult(new EventEntityDelete<Model.ShoppingListItem>(itemId));
            });

        }

        async Task On(EventEnvelope<ItemAddedToList> e) => await base.Create(e);
        async Task On(EventEnvelope<ItemCrossedOffList> e) => await base.Update(e);
        async Task On(EventEnvelope<ItemRemovedFromList> e) => await base.Delete(e);
    }
}
