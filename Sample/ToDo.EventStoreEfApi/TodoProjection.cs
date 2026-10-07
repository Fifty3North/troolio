using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Omu.ValueInjecter;
using Troolio.Core;
using Troolio.Core.Projection;

public interface ITodoEfProjection : IProjectionActor { }
[ProjectionStreamSubscription(nameof(TodoActor))]
public sealed class TodoEfProjection : EntityFrameworkBatchedProjection<TodoReadModel, TodoReadModelDbContext>, ITodoEfProjection
{
    protected override void SetupMappings()
    {
        Mapper.AddMap<EventEnvelope<TodoCreated>, Task<EventEntityCreate<TodoReadModel>>>(source =>
            Task.FromResult(new EventEntityCreate<TodoReadModel>(source.Event.TodoId, new TodoReadModel
            {
                Id = source.Event.TodoId, OwnerId = source.Event.OwnerId, Title = source.Event.Title,
                CreatedAt = source.Event.CreatedAt
            })));
        Mapper.AddMap<EventEnvelope<TodoCompleted>, Task<EventEntityUpdate<TodoReadModel>>>(source =>
            Task.FromResult(new EventEntityUpdate<TodoReadModel>(source.Event.TodoId,
                [model => model.IsCompleted = true, model => model.CompletedAt = source.Event.CompletedAt])));
    }
    public Task On(EventEnvelope<TodoCreated> e) => Create(e);
    public Task On(EventEnvelope<TodoCompleted> e) => Update(e);
}
public sealed class TodoReadModel
{
    [Key] public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
public sealed class TodoReadModelDbContext(DbContextOptions<TodoReadModelDbContext> options) : DbContext(options)
{
    public DbSet<TodoReadModel> Todos => Set<TodoReadModel>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoReadModel>().Property(x => x.Title).HasMaxLength(200);
        modelBuilder.Entity<TodoReadModel>().HasIndex(x => x.OwnerId);
        // SQLite can order UTC ticks in SQL; DateTimeOffset itself has no native ordering.
        modelBuilder.Entity<TodoReadModel>().Property(x => x.CreatedAt)
            .HasConversion(value => value.UtcTicks, ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
    }
}
