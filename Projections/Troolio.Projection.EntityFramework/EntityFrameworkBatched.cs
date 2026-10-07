using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Omu.ValueInjecter;
using Orleankka;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Troolio.Core.Projection.Exceptions;

namespace Troolio.Core.Projection;

/// <summary>Applies live projection changes and acknowledges only completed database writes.</summary>
/// <remarks>The historical name is retained for compatibility. Writes are awaited, not kept in a volatile batch queue.
/// Applications needing durable recovery must supply an inbox/checkpoint and durable dispatch.</remarks>
public abstract class EntityFrameworkBatched<TEntity, TDbContext> : ProjectionActor
    where TEntity : class
    where TDbContext : DbContext
{
    private readonly SemaphoreSlim _operations = new(1, 1);

    public Task On(Commands.ProcessNow _) => ProcessNow();
    public Task On(Commands.Flush _) => ProcessNow();

    public async Task ProcessNow()
    {
        await _operations.WaitAsync();
        _operations.Release();
    }

    private async Task Execute(Func<TDbContext, Task> operation)
    {
        await _operations.WaitAsync();
        try
        {
            using IServiceScope scope = ServiceProvider.CreateScope();
            TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();
            await operation(context);
            foreach (var entry in context.ChangeTracker.Entries()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
                Validator.ValidateObject(entry.Entity, new ValidationContext(entry.Entity), validateAllProperties: true);
            await context.SaveChangesAsync();
        }
        finally { _operations.Release(); }
    }

    public async Task Create<TEvent>(EventEnvelope<TEvent> envelope) where TEvent : Event
    {
        EventEntityCreate<TEntity> change = await Mapper.Map<Task<EventEntityCreate<TEntity>>>(envelope);
        await Execute(async context =>
        {
            if (await context.Set<TEntity>().FindAsync(change.EntityId) is null)
                context.Set<TEntity>().Add(change.Entity);
        });
    }

    public async Task Update<TEvent>(EventEnvelope<TEvent> envelope) where TEvent : Event
    {
        EventEntityUpdate<TEntity> change = await Mapper.Map<Task<EventEntityUpdate<TEntity>>>(envelope);
        await Execute(async context =>
        {
            TEntity entity = await Find(context, change.EntityId);
            foreach (Action<TEntity> update in change.PropertyUpdateActions) update(entity);
        });
    }

    public async Task Delete<TEvent>(EventEnvelope<TEvent> envelope) where TEvent : Event
    {
        EventEntityDelete<TEntity> change = await Mapper.Map<Task<EventEntityDelete<TEntity>>>(envelope);
        await Execute(async context =>
        {
            TEntity? entity = await context.Set<TEntity>().FindAsync(change.EntityId);
            if (entity is not null) context.Set<TEntity>().Remove(entity);
        });
    }

    public Task AddItem<TChildEntity, TEvent>(EventEnvelope<TEvent> envelope)
        where TChildEntity : class where TEvent : Event => ChangeCollection<TChildEntity, TEvent>(envelope, true);

    public Task RemoveItem<TChildEntity, TEvent>(EventEnvelope<TEvent> envelope)
        where TChildEntity : class where TEvent : Event => ChangeCollection<TChildEntity, TEvent>(envelope, false);

    private async Task ChangeCollection<TChildEntity, TEvent>(EventEnvelope<TEvent> envelope, bool add)
        where TChildEntity : class where TEvent : Event
    {
        EventEntityCollection<TEntity, TChildEntity> change =
            await Mapper.Map<Task<EventEntityCollection<TEntity, TChildEntity>>>(envelope);
        await Execute(async context =>
        {
            TEntity parent = await Find(context, change.EntityId);
            TChildEntity child = await context.Set<TChildEntity>().FindAsync(change.ChildEntityId)
                ?? throw new EntityDoesNotExistException($"Child entity {typeof(TChildEntity).Name}/{change.ChildEntityId} does not exist.");
            await context.Entry(parent).Collection(((MemberExpression)change.Collection.Body).Member.Name).LoadAsync();
            ICollection<TChildEntity> collection = change.Collection.Compile()(parent);
            if (add && !collection.Contains(child)) collection.Add(child);
            if (!add) collection.Remove(child);
        });
    }

    private static async Task<TEntity> Find(TDbContext context, Guid id) =>
        await context.Set<TEntity>().FindAsync(id)
        ?? throw new EntityDoesNotExistException($"Entity {typeof(TEntity).Name}/{id} does not exist.");
}
