# Explore collaborative shopping lists

This .NET 10 sample connects HTTP commands, event-sourced actors, live orchestration, SQLite projections and linked read models. Its Vue application uses the same HTTP API. All framework references come from public NuGet packages.

## Run locally

From the repository root, start the silo:

```bash
dotnet run --project Sample/ShoppingListSample/Sample.Host
```

In a second terminal start the API:

```bash
dotnet run --project Sample/ShoppingListSample/Sample.Api
```

The API listens at `http://localhost:8081`. Open `/swagger` to inspect routes. The Orleans sample silo uses ports 9000 and 9001. For a single process:

```bash
dotnet run --project Sample/ShoppingListSample/Sample.Api -- --Shopping:EmbeddedHost=true
```

Both hosts resolve SQLite to the same local application data directory, `TroolioSamples/shopping/readmodels.db`. To choose another location, set `Shopping__ReadModels__Path` to the **same absolute path** in both terminals. The sample filesystem event store uses `troolio-samples-shopping` under the user's application data directory. Set `Shopping__EventFolder` to choose another owned event folder. Both are local development stores. SQLite is a relational projection demonstration, not a multi-node deployment configuration.

Docker runs the embedded variant with persistent local volumes:

```bash
docker compose -f Sample/ShoppingListSample/docker-compose.yml up --build
```

## Demo identity

`GET /demo/users` returns three fixed teaching identities: Alice, Bob and Carol. Send `Authorization: Bearer demo-alice`, `demo-bob` or `demo-carol`. Alice's ID is `11111111-1111-1111-1111-111111111111`; Bob's is `22222222-2222-2222-2222-222222222222`. A conflicting `userId` header is rejected. The verified identity supplies command `UserId` and read authorization. These publicly known credentials are suitable only for local learning. The API refuses to start outside Development/Testing; replace the demo scheme with your identity provider before deployment.

```bash
curl http://localhost:8081/demo/users
curl -X POST http://localhost:8081/ShoppingList/aaaaaaaa-1111-1111-1111-111111111111/CreateNewList \
  -H 'Authorization: Bearer demo-alice' -H 'Content-Type: application/json' \
  -d '{"title":"Weekend groceries"}'
curl http://localhost:8081/User/11111111-1111-1111-1111-111111111111/MyShoppingLists?take=50 \
  -H 'Authorization: Bearer demo-alice'
```

## Commands and pure replay

A command validates the caller and current aggregate state before producing domain events. Event handlers restore only the owning actor's state; they do not send mail or write projections during replay.

```csharp
public IEnumerable<Event> Handle(JoinList command)
{
    if (State.Author == command.Headers.UserId)
        throw new AuthorCannotJoinListException();
    if (State.Collaborators.Contains(command.Headers.UserId))
        yield break; // Repeated orchestration delivery has the same domain outcome.
    yield return new ListJoined(command.Headers);
}
public void On(ListJoined e)
    => State = State with { Collaborators = State.Collaborators.Add(e.Headers.UserId) };
```

`ShoppingListActor` owns title, author, items and collaborators. Author and collaborator can add/cross off items; only the author can remove items or request a join code. Individual aggregate queries carry the verified user ID and enforce membership.

## Orchestration and metadata

Creating a list generates a random join code once and stores it in the creation event. The live orchestrator registers that list in the join-code index. Registration and membership targets tolerate repeated deliveries.

```csharp
public Task On(EventEnvelope<NewListCreated> e)
    => System.ActorOf<IAllShoppingListsActor>(Constants.SingletonActorId)
        .Tell(new AddShoppingList(e.Event.Headers, Guid.Parse(e.Id), e.Event.JoinCode));
```

Pass the original event headers into an internal command. Core gives the child command its own message ID and sets its causation ID to the source message ID. Correlation ID connects one logical request, user ID identifies the verified caller, and device ID is a client-reported diagnostic identifier. Send `correlationId` and `deviceId` as GUID headers; the API echoes a valid correlation ID or generates one when absent. These diagnostic headers do not grant permissions. `EventEnvelope.Id` is the source actor key; it is not an event deduplication key. Domain list/item/member IDs drive idempotency.

Ordinary listener delivery is eventual. A successful create does not mean every orchestration and projection has finished, and an ordinary listener failure does not roll back every actor. Refresh/poll read models with a bounded timeout. Applications requiring durable handoff, atomic routes or external effects must configure the corresponding framework capabilities explicitly.

![Commands, durable events and query views](../assets/diagrams/samples.svg)

## Durable catalogs and linked read models

The EF extension projects committed live events into lists, items and memberships. A deterministic member ID derived from list ID and user ID prevents duplicate memberships. Catalog HTTP reads query SQLite directly with caller filtering, `skip` and `take` (maximum 100). They never scan all event streams or activate every list actor.

```csharp
var lists = await db.ShoppingLists.AsNoTracking()
    .Where(list => db.ShoppingListMembers.Any(member =>
        member.UserId == caller && member.ShoppingListId == list.Id))
    .OrderBy(list => list.Title).ThenBy(list => list.Id)
    .Skip(skip).Take(take).ToArrayAsync();
```

`ShoppingListReadModelProjectionActor` separately links events for one list's detailed historical model. The HTTP endpoint explicitly evaluates `Authorized` before returning it. Catalogs use the indexed relational view; a linked single-model resolver has a different cost profile.

```csharp
var model = await client.Get<ShoppingListReadModel>(listId.ToString());
if (!model.Authorized(verifiedHeaders)) return Forbid();
return Ok(model);
```

Authors see their join codes in the SQL catalog; collaborators do not. To join as Bob, send `{ "joinCode": "..." }` to `POST /AllShoppingLists/JoinListUsingCode` with Bob's token. Wait for live membership projection before reading Bob's catalog.

### Read-model transport

The root linked model derives from `TroolioReadModel` so Core discovers its reducers. For public Core 10.0.8, this sample supplies an Orleans surrogate converter for transport because that base record does not provide its own generated base codec. The surrogate contains only model data; authorization remains a computed predicate and is excluded from HTTP JSON. The Shared project references `Microsoft.Orleans.Sdk`, which discovers `[RegisterConverter]` during source generation; clients and hosts both reference that Shared assembly.

```csharp
[GenerateSerializer]
public struct ShoppingListSurrogate
{
    [Id(0)] public Guid Id;
    [Id(1)] public string Title;
    [Id(2)] public Guid OwnerId;
    [Id(3)] public ImmutableArray<Guid> Collaborators;
    [Id(4)] public ImmutableArray<ShoppingListItemReadModel> Items;
    [Id(5)] public string JoinCode;
}

[RegisterConverter]
public sealed class ShoppingListConverter
    : IConverter<ShoppingListReadModel, ShoppingListSurrogate>
{
    public ShoppingListReadModel ConvertFromSurrogate(in ShoppingListSurrogate value)
        => new(value.Id) { Title = value.Title, OwnerId = value.OwnerId,
            Collaborators = value.Collaborators, Items = value.Items, JoinCode = value.JoinCode };
    public ShoppingListSurrogate ConvertToSurrogate(in ShoppingListReadModel value)
        => new() { Id = value.Id, Title = value.Title, OwnerId = value.OwnerId,
            Collaborators = value.Collaborators, Items = value.Items, JoinCode = value.JoinCode };
}
```

The host additionally initializes Core's linked-model resolver using Orleans 10 per-grain activation components. Register `AddShoppingReadModels` in the final silo configuration callback, after the default Orleans services:

```csharp
silo.ConfigureServices(services => services.AddShoppingReadModels());

public sealed class ConfigureReadModelActivation : IConfigureGrainTypeComponents
{
    public void Configure(GrainType type, GrainProperties properties, GrainTypeSharedContext shared)
    {
        if (shared.GetComponent<IGrainActivator>() is { } inner && inner is not ReadModelActivator)
            shared.SetComponent<IGrainActivator>(new ReadModelActivator(inner));
    }
}

public sealed class ReadModelActivator(IGrainActivator inner) : IGrainActivator
{
    public object CreateInstance(IGrainContext context)
    {
        var grain = inner.CreateInstance(context);
        if (grain is IReadModelResolver resolver) resolver.Initialize(context.ActivationServices);
        return grain;
    }
    public ValueTask DisposeInstance(IGrainContext context, object grain)
        => inner.DisposeInstance(context, grain);
}
```

`AddShoppingReadModels` registers `ConfigureReadModelActivation` as `IConfigureGrainTypeComponents`. See `Sample.Host.App/ReadModelActivator.cs` for the complete helper. Do not serialize delegates or rely on transport to authorize a read; the HTTP handler still checks the current verified caller.

## Tracing and external effects

Authenticated demo users can enable/disable tracing and flush it using `/Telemetry`. The email actor is a mock and the batch queue is in memory. This illustrates routing, not durable or exactly-once email delivery. Real providers need durable dispatch and provider idempotency before external sends.

## Try a bounded load

```bash
dotnet run --project Sample/ShoppingListSample/Sample.LoadTest -- 10
```

The load utility creates 1–1000 lists sequentially against a disposable local database; it is a smoke tool, not a capacity benchmark.

The former MySQL migrations and unused logging filter have been retired. This fresh SQLite teaching schema uses `EnsureCreated`; it does not migrate existing MySQL data. For a deployed application, provision versioned schema migrations separately from serving startup.

Provider references: [EF SQLite](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/) and [Orleans serialization](https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/serialization).

## Complete source and packages

[Open Shopping List on GitHub](https://github.com/Fifty3North/troolio/tree/main/Sample/ShoppingListSample). It uses public Troolio.Core 10.0.8 and Troolio.Projection.EntityFramework 10.0.2. See [EF projections](ef-projections.html), [metadata lineage](correlation-and-causation.html), and [orchestration](orchestration.html) for the framework concepts behind each handler.
