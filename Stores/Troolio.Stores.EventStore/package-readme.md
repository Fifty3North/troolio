# Troolio.Stores.EventStore

Supported gRPC persistence for ordinary Troolio actors using KurrentDB. The historical package ID remains available; the deprecated TCP transport has been replaced. Requires .NET 10 and Troolio.Core 10.0.8.

```bash
dotnet add package Troolio.Stores.EventStore --version 10.0.2
```

## Register the client and ordinary store

The host owns a reusable client. Credentials and TLS belong in application configuration, not source code.

```csharp
services.AddSingleton(_ => new KurrentDBClient(
    KurrentDBClientSettings.Create(configuration.GetConnectionString("EventStore")!)));
services.AddSingleton<ESStore>();
```

Use `TroolioServer<ESStore>` when building the Troolio host. Its serialization and logging services supply the remaining constructor arguments:

```csharp
IHost host = Host.CreateDefaultBuilder().TroolioServer<ESStore>(
    "Tasks", new[] { typeof(TaskActor).Assembly }, services =>
    {
        services.AddSingleton(_ => new KurrentDBClient(
            KurrentDBClientSettings.Create(connectionString)));
    });
await host.StartAsync();
```

Configure the host's Orleans membership/storage profile separately. The convenience `Startup.RunWithDefaults` wrapper reads `ConnectionStrings:EventStore` from appsettings and environment variables. `ES.CreateClient(connectionString)` creates a client; it does not use a global static connection.

A local disposable insecure node can use `esdb://localhost:2113?tls=false`. Production should use TLS and credentials, for example `esdb://user:password@server:2113?tls=true`, supplied securely through configuration.

## Append with concurrency control

A stream head of 0 means no events. Troolio event versions start at 1; KurrentDB revisions start at 0. The adapter performs that translation and throws Troolio's `WrongExpectedVersionException` on a conflicting append.

```csharp
IEvent[] events = { new TaskRenamed(taskId, "New title", headers) };
ulong appended = await store.Append(streamName, expectedVersion, events);
IEvent[] tail = await store.ReadStreamFromEvent(streamName, expectedVersion + 1);
(IEvent? last, ulong head) = await store.ReadLastEvent(streamName);
```

Events retain serialized metadata including correlation, causation, user, device and transaction IDs. An event's message ID supplies its storage identity when present. Empty appends write nothing. Unknown event types or invalid payloads fail explicitly; they are not silently dropped from actor replay.

## Link streams and snapshots

Linked read models refer to source stream/version pairs. The adapter writes the KurrentDB link representation and resolves it on reads.

```csharp
await store.Append(readModelStream, linkHead,
    new IEvent[] { new LinkEvent(Guid.NewGuid(), sourceVersion, sourceStream) });
IEvent? sourceEvent = await store.ReadStreamEvent(readModelStream, linkHead + 1);
```

The link stream retains its own version even when the resolved source event has a different source version. Keep source events available for the lifetime of linked read models. Typed actor snapshot events use the same serializer and ordinary persistence API.

## Migration and delivery limits

Replace old TCP host/port settings and global `ES.Connection` registration with the injected gRPC client. KurrentDB versions without TCP support can now be used. This package preserves ordinary event CLR type names and serialized payloads; retain event types/aliases and validate historical data before renaming application types.

This adapter implements ordinary `IStore`. `Troolio.KurrentDB` provides separate policy/authority-driven selected transaction capabilities; it is not a drop-in ordinary store replacement. Domain append here does not automatically provide durable projection dispatch, a SQL inbox or atomic cross-actor operations.

Actor activation may restore its own state from its stream. Startup and readiness do not enumerate all actors or replay global history. Recovery tools should operate on bounded streams/partitions. `Clear` rejects deletion of shared event history; reset explicitly owned test infrastructure instead.

[EventStore store guide](https://fifty3north.github.io/troolio-docs/docs/eventstore-store.html) · [ToDo sample](https://github.com/Fifty3North/troolio/tree/main/Sample/ToDo.EventStoreEfApi)
