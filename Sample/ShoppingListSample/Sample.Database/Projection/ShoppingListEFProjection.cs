using Omu.ValueInjecter;
using Orleankka;
using Orleans;
using Orleans.Concurrency;
using Sample.Shared.ShoppingList;
using System;
using System.Threading.Tasks;
using Troolio.Core;
using Troolio.Core.Projection;

namespace Sample.Database.Projection
{
    public interface IShoppingListEFProjection : IActorGrain, IGrainWithStringKey { }

    [RegexImplicitStreamSubscription("ShoppingListActor-.*")]
    [Reentrant]
    public class ShoppingListEFProjection : EntityFrameworkBatched<Model.ShoppingList, Model.ShoppingListsDbContext>, IShoppingListEFProjection, IGrainWithGuidCompoundKey
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
            Mapper.AddMap<EventEnvelope<NewListCreated>, Task<EventEntityCreate<Model.ShoppingList>>>(src =>
            {
                Guid listId = Guid.Parse(src.Id);

                var item = new Model.ShoppingList()
                {
                    Id = Guid.Parse(src.Id),
                    Title = src.Event.Title,
                    AuthorId = src.Event.Headers.UserId
                };

                return Task.FromResult(new EventEntityCreate<Model.ShoppingList>(listId, item));
            });

        }

        async Task On(EventEnvelope<NewListCreated> e) => await base.Create(e);

    }
}
