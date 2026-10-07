using Omu.ValueInjecter;
using Sample.Database.Model;
using Sample.Host.App.ShoppingList;
using Sample.Shared.Events;
using Troolio.Core;
using Troolio.Core.Projection;

namespace Sample.Database.Projection;

[ProjectionStreamSubscription(nameof(ShoppingListActor))]
public sealed class ShoppingListMemberEFProjection
    : EntityFrameworkBatchedProjection<ShoppingListMember, ShoppingListsDbContext>
{
    protected override void SetupMappings()
    {
        Mapper.AddMap<EventEnvelope<NewListCreated>, Task<EventEntityCreate<ShoppingListMember>>>(e =>
            Task.FromResult(Member(Guid.Parse(e.Id), e.Event.Headers.UserId)));
        Mapper.AddMap<EventEnvelope<ListJoined>, Task<EventEntityCreate<ShoppingListMember>>>(e =>
            Task.FromResult(Member(Guid.Parse(e.Id), e.Event.Headers.UserId)));
    }
    private static EventEntityCreate<ShoppingListMember> Member(Guid listId, Guid userId)
    {
        var id = ShoppingListMember.Key(listId, userId);
        return new(id, new ShoppingListMember { Id = id, ShoppingListId = listId, UserId = userId });
    }
    public Task On(EventEnvelope<NewListCreated> e) => Create(e);
    public Task On(EventEnvelope<ListJoined> e) => Create(e);
}
