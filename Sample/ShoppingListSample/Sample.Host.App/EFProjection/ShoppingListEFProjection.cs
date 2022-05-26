using Omu.ValueInjecter;
using Orleans;
using Orleans.Concurrency;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Events;
using Troolio.Core;
using Troolio.Core.Projection;

namespace Sample.Database.Projection
{
    [RegexImplicitStreamSubscription("ShoppingListActor-.*")]
    [Reentrant]
    public class ShoppingListEFProjection : EntityFrameworkBatchedProjection<Model.ShoppingList, Model.ShoppingListsDbContext>, IShoppingListEFProjection
    {
        protected override void SetupMappings()
        {
            Mapper.AddMap<EventEnvelope<NewListCreated>, Task<EventEntityCreate<Model.ShoppingList>>>(src =>
            {
                Guid listId = Guid.Parse(src.Id);

                var item = new Model.ShoppingList()
                {
                    Id = src.Id,
                    Title = src.Event.Title,
                    AuthorId = src.Event.Headers.UserId.ToString(),
                };

                return Task.FromResult(new EventEntityCreate<Model.ShoppingList>(listId, item));
            });
        }

        async Task On(EventEnvelope<NewListCreated> e) => await base.Create(e);
    }
}
