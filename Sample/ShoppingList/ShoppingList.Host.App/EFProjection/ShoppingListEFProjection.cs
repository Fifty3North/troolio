using Omu.ValueInjecter;
using Orleans.Concurrency;
using ShoppingList.Host.App;
using ShoppingList.Shared.Events;
using Troolio.Core;
using Troolio.Core.Projection;

namespace Sample.Database.Projection
{
    [ProjectionStreamSubscription(nameof(ShoppingListActor))]
    [Reentrant]
    public class ShoppingListEFProjection : EntityFrameworkBatchedProjection<Model.ShoppingList, Model.ShoppingListsDbContext>, ShoppingList.Shared.ActorInterfaces.IShoppingListEFProjection
    {
        protected override void SetupMappings()
        {
            Mapper.AddMap<EventEnvelope<NewListCreated>, Task<EventEntityCreate<Model.ShoppingList>>>(src =>
            {
                Guid listId = Guid.Parse(src.Id);

                var item = new Model.ShoppingList()
                {
                    Id = listId,
                    Title = src.Event.Title,
                    AuthorId = src.Event.Headers.UserId,
                };

                return Task.FromResult(new EventEntityCreate<Model.ShoppingList>(listId, item));
            });
        }

        async Task On(EventEnvelope<NewListCreated> e) => await base.Create(e);
    }
}
