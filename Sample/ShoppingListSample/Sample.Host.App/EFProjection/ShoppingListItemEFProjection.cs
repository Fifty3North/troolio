using Omu.ValueInjecter;
using Orleankka;
using Orleans;
using Orleans.Concurrency;
using Sample.Shared.enums;
using Sample.Shared.ShoppingList;
using System;
using System.Threading.Tasks;
using Troolio.Core;
using Troolio.Core.Projection;

namespace Sample.Database.Projection
{
    public interface IShoppingListItemEFProjection : IActorGrain, IGrainWithStringKey { }


    [RegexImplicitStreamSubscription("ShoppingListActor-.*")]
    [Reentrant]
    public class ShoppingListItemEFProjection : EntityFrameworkBatched<Model.ShoppingListItem, Model.ShoppingListsDbContext>, IShoppingListItemEFProjection, IGrainWithGuidCompoundKey
    {
        #region Boiler Plate ...
        public override Task OnActivateAsync()
        {
            SetupMappings();
            return base.OnActivateAsync();
        }

        /// <summary>
        /// If the projection doesn't handle an event then ignore it.
        /// Otherwise, we will result in 'Orleankka.UnhandledMessageException'
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public override async Task<object> Receive(object envelope)
        {
            if (Dispatcher.CanHandle(envelope.GetType()))
            {
                return await base.Receive(envelope);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        #endregion

        private void SetupMappings()
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
