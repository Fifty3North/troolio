# Troolio.Projection.EntityFramework

Apply live Troolio events to relational read models with Entity Framework Core 10. Requires .NET 10, Troolio.Core 10.0.8 and an EF 10 provider selected by your application.

```bash
dotnet add package Troolio.Projection.EntityFramework --version 10.0.2
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.12
```

## Define the read model

A projection is a derived view of events. A read model stores the fields needed by a query; it is separate from the actor's in-memory state.

```csharp
public sealed class TaskRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
}

public sealed class TaskDb(DbContextOptions<TaskDb> options) : DbContext(options)
{
    public DbSet<TaskRow> Tasks => Set<TaskRow>();
}
```

Register a scoped context in the Troolio host's service callback. The adapter creates a fresh scope for each write. Apply database migrations explicitly during deployment; host readiness does not replay every actor.

```csharp
services.AddDbContext<TaskDb>(options =>
    options.UseSqlite("Data Source=task-readmodels.db"));
```

## Map and apply a live event

The historical `EntityFrameworkBatchedProjection` name is retained. Processing now awaits the database commit. Mapping or database failures reach the caller instead of disappearing into a memory queue.

```csharp
[GenerateSerializer]
public sealed record TaskCreated(
    [property: Id(0)] Guid TaskId,
    [property: Id(1)] string Title,
    Metadata Headers) : Event(Headers);

public interface ITaskProjection : IProjectionActor { }

[ProjectionStreamSubscription("TaskActor")]
public sealed class TaskProjection :
    EntityFrameworkBatchedProjection<TaskRow, TaskDb>, ITaskProjection
{
    protected override void SetupMappings()
    {
        Mapper.AddMap<EventEnvelope<TaskCreated>, Task<EventEntityCreate<TaskRow>>>(e =>
            Task.FromResult(new EventEntityCreate<TaskRow>(e.Event.TaskId,
                new TaskRow { Id = e.Event.TaskId, Title = e.Event.Title })));
    }

    public Task On(EventEnvelope<TaskCreated> e) => Create(e);
}
```

The obsolete `EFPersistance` base and its volatile queue helper are removed. Use the event-to-change mappings above when migrating. A projection interface must inherit `IProjectionActor`. Update older interfaces that inherited only `IActor`. Register the projection's assembly with the Troolio host. The framework attaches and resumes its implicit projection-stream subscription. Actor replay restores actor state only; it must not write these rows.

## Update and delete

`Create` leaves an existing primary-key row unchanged. A repeated create cannot overwrite a later update. `Delete` tolerates an already absent row. `Update` requires an existing row and propagates a missing-row error.

```csharp
Mapper.AddMap<EventEnvelope<TaskRenamed>, Task<EventEntityUpdate<TaskRow>>>(e =>
    Task.FromResult(new EventEntityUpdate<TaskRow>(e.Event.TaskId,
        new Action<TaskRow>[] { row => row.Title = e.Event.Title })));

Mapper.AddMap<EventEnvelope<TaskDeleted>, Task<EventEntityDelete<TaskRow>>>(e =>
    Task.FromResult(new EventEntityDelete<TaskRow>(e.Event.TaskId)));
// Corresponding event handlers await Update(e) and Delete(e).
```

`AddItem` and `RemoveItem` map `EventEntityCollection<TEntity,TChildEntity>` and load the collection before changing membership. Models need a single GUID primary key for these helpers. `ProcessNow`/`Flush` remain compatibility barriers; there is no pending batch timer to flush.

## Recovery and authorization

Awaited writes provide correct completion and failure reporting. This generic adapter does not atomically commit an inbox, source checkpoint or dispatch outbox with domain events. For durable recovery, use application-owned transactions and deduplicate by source stream/version, not envelope actor ID or diagnostic message IDs. Bound repair work by stream or partition. Ordinary memory streams alone do not recover missed deliveries after a process crash.

Read-side endpoints must check the current authenticated user's access before returning rows. Metadata user IDs describe event origin; they are not proof of the current caller's permission.

[EF projection guide](https://fifty3north.github.io/troolio-docs/docs/ef-projections.html) · [Shopping sample](https://github.com/Fifty3North/troolio/tree/main/Sample/ShoppingListSample) · [ToDo sample](https://github.com/Fifty3North/troolio/tree/main/Sample/ToDo.EventStoreEfApi)
