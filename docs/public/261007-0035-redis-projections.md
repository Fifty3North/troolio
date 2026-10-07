# Redis projections

Redis derived read models, entity indexes and bounded partition change feeds for Troolio applications. Requires .NET 10 and Troolio.Core 10.0.8. Redis is a derived cache; keep authoritative business events in the actor's event store.

```bash
dotnet add package Troolio.Projection.Redis --version 10.0.2
```

## Register an owned connection

Register one provider for each application configuration. The connection is reusable and belongs to that provider's DI lifetime; separate provider instances retain their own configuration.

```csharp
services.AddSingleton<IRedisConnectionProvider>(_ =>
    new RedisConnectionProvider(configuration.GetConnectionString("Redis")!));
services.AddSingleton<RedisProvider<TaskCard>>();
services.AddSingleton<IRedisReadProvider<TaskCard>>(sp =>
    sp.GetRequiredService<RedisProvider<TaskCard>>());
services.AddSingleton<IRedisWriteProvider<TaskCard>>(sp =>
    sp.GetRequiredService<RedisProvider<TaskCard>>());
```

Use a secured production connection string supplied through host configuration. The host disposes the singleton connection provider.

## Create, update and read an entity

The entity ID identifies one cached object. The partition ID identifies an index and a change feed, such as one user's task board. Provider transactions commit the mutation, index change and feed entry together on a single Redis server.

```csharp
public sealed class TaskCard
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
}

var card = new TaskCard { Id = taskId, Title = "Buy milk" };
string cursor = await provider.CreateEntity(taskId, card, boardId);
card.Title = "Buy oat milk";
await provider.UpdateEntity(taskId, card, boardId, x => x.Title!);
TaskCard? current = await provider.GetEntityAsync(taskId);
```

Use asynchronous reads inside async actor and HTTP handlers. Each successful write returns its own change cursor, including concurrent writes. Failures propagate; a cursor is not returned for an uncommitted transaction.

## Partition membership and deletion

Membership indexes can share one entity across partitions. Removing membership leaves the entity itself available.

```csharp
await provider.AddEntityKeyToPartition(taskId, secondBoardId);
await provider.UpdateEntityKeyInPartition(taskId, secondBoardId);
await provider.RemoveEntityKeyFromPartition(taskId, secondBoardId);
// Delete removes the shared entity plus this partition's index entry.
await provider.DeleteEntity(taskId, boardId);
```

If an entity belongs to several partitions, remove its other memberships before globally deleting it. Entity keys currently use the CLR type's simple name, so choose distinct read-model class names across your application. Existing key layout is retained. Multi-key transactions are intended for a single Redis server; this adapter does not configure Redis Cluster hash slots.

## Read a bounded change page

Absolute offsets are independent from Troolio correlation/causation IDs. A page reads at most 1000 feed entries; 100 is the default. Advance only after processing the page.

```csharp
long offset = savedOffset;
RedisChangePage page = await provider.GetChangePageAsync(boardId, offset, 100);
foreach (ChangeHashEntry change in page.Entries)
{
    // Read the current entity, or remove it locally when the entity is absent.
    await RefreshCard(change.EntityKey);
}
savedOffset = page.NextOffset;
```

`HasMore` means the page filled its requested limit; the next page can be empty. Legacy `GetChangeEntries` checks only the latest 1000 entries and rejects a cursor outside that window. Offset paging assumes retained feed entries have not been trimmed. Configure retention and snapshot/reload behavior together; Redis feed entries are cache synchronization hints, not a durable delivery ledger.

## Event-driven projection

A projection can map each live event to `RedisEventEntityCreate`, `RedisEventEntityUpdate`, partition membership changes or deletion. `RedisPersistence<TEntity>.Persist` awaits the mapped mutations and rejects unsupported mapping types. The application supplies live event subscription and durable recovery strategy. Pure actor replay never updates Redis or sends notifications.

```csharp
Mapper.AddMap<EventEnvelope<TaskRenamed>, Task<RedisEventEntityUpdate<TaskCard>>>(e =>
    Task.FromResult(new RedisEventEntityUpdate<TaskCard>(e.Event.TaskId,
        new TaskCard { Id = e.Event.TaskId, Title = e.Event.Title },
        e.Event.BoardId, card => card.Title!)));
// A RedisPersistence<TaskCard> handler awaits Update(e).
```

Authorization remains with the application: resolve partition IDs from the verified caller and enforce access before exposing cards or feeds.


## Run the provider example

[Redis Read Models on GitHub](https://github.com/Fifty3North/troolio/tree/main/Sample/RedisReadModels) is a small executable using this package's public API. Point it at an owned development Redis server:

```sh
ConnectionStrings__Redis='localhost:16379' \
dotnet run --project Sample/RedisReadModels
```

The program creates a card, updates one field, reads the committed value, deletes the card and walks a change feed in pages of two. It uses a fresh partition and never scans or flushes shared Redis data.

```csharp
var updated = await cards.UpdateEntity(cardId, card, partitionId,
    value => value.IsComplete);
var page = await cards.GetChangePageAsync(partitionId, offset, maxCount: 2);
offset = page.NextOffset;
```

Use a projection-specific class name and a caller-authorized partition. A partition is a query/index boundary, not an authentication mechanism. A deleted entity may still have retained change entries so clients can remove stale local cards. A current read can include a newer change than the feed entry you are processing; this is a latest-state refresh protocol rather than historical event replay.

## Recover a derived cache

Keep a saved cursor only as long as its feed history is retained. Pair any truncation policy with a reload marker and an explicit bounded refresh source. A cache miss is not evidence that the authoritative domain object never existed. Use a relational read model or bounded repair feed when reconstructing the cache; serving startup should not replay every actor stream.
