# Marten event storage

PostgreSQL event persistence through Marten 9 for ordinary Troolio actors. Requires .NET 10 and Troolio.Core 10.0.8.

```bash
dotnet add package Troolio.Stores.Marten --version 10.0.2
```

## Provision and register

Create the database through deployment tooling. Runtime connections do not need permission to create databases. Configure one host-owned `IDocumentStore` and inject it into the adapter.

```csharp
services.AddSingleton<IDocumentStore>(_ => DocumentStore.For(options =>
    MartenStore.Configure(options, configuration.GetConnectionString("Marten")!)));
services.AddSingleton<MartenStore>(sp =>
    new MartenStore(sp.GetRequiredService<IDocumentStore>()));
services.AddSingleton<IStore>(sp => sp.GetRequiredService<MartenStore>());
```

Put these registrations in the `TroolioServer<MartenStore>` service callback. The explicit factories select the supplied document-store constructor and bind `IStore` without constructor ambiguity. Register the store instance with a factory when your DI container owns it. `MartenStore(IDocumentStore)` does not dispose the supplied store. The configuration constructor creates and owns its store, and reads `ConnectionStrings:Marten`. Both paths use the `troolio` schema and string stream identities.

For an explicitly owned development database, schema creation can be requested separately:

```csharp
using IDocumentStore developmentStore = DocumentStore.For(options =>
    MartenStore.Configure(options, developmentConnection, createSchema: true));
await developmentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
```

Production deployment should apply reviewed schema changes before starting serving processes. Store construction never scans or replays actor streams.

## Append and read

A Troolio stream version starts at 1;0 means an empty stream. Pass the currently known head when appending. The adapter maps concurrency failures to Troolio's `WrongExpectedVersionException`.

```csharp
var changed = new TaskRenamed(taskId, "New title", headers);
ulong written = await store.Append(streamName, expectedVersion,
    new IEvent[] { changed });
IEvent[] tail = await store.ReadStreamFromEvent(streamName, expectedVersion + 1);
(IEvent? latest, ulong version) = await store.ReadLastEvent(streamName);
```

`ReadStreamFromEvent` is inclusive. `ReadStreamEvent(stream, 0)` returns null. Empty appends write nothing. The adapter configures Marten's Rich append mode because its explicit expected-version append API requires it.

## Linked read models and snapshots

A link stores a source stream/version location instead of copying the source event. Reads resolve the original event while the link stream keeps its own version.

```csharp
await store.Append(readModelStream, expectedLinkVersion,
    new IEvent[] { new LinkEvent(Guid.NewGuid(), sourceVersion, sourceStream) });
IEvent[] resolved = await store.ReadStream(readModelStream);
```

Typed actor snapshot events also persist through `IStore`. Links must target retained events; missing targets, unsupported data and excessive link depth fail explicitly instead of silently shortening replay.

## Existing data and migration

The adapter preserves the `EventWrapper` type/shape for wrapped streams. Marten 9 changed serializer defaults, so this package explicitly uses its Newtonsoft integration with polymorphic type metadata and registers the wrapper before reading. Validate old streams and typed snapshots in an isolated database before upgrading a production application. Persisted CLR names still couple historical events to application types; preserve aliases/types or provide an explicit data migration when renaming them.

Do not rebuild global read models during startup. Actor activation may replay that actor's own stream into pure in-memory state. Catalog and cross-actor queries should read durable relational projections maintained by live events.

This ordinary store does not atomically commit a projection inbox/outbox or selected multi-actor transaction. Configure those capabilities separately. `Clear` intentionally rejects shared-history deletion; reset only explicitly owned test databases through deployment tooling.


## Run a persisted actor

[Marten Counter on GitHub](https://github.com/Fifty3North/troolio/tree/main/Sample/MartenCounter) runs an actual Troolio actor through this adapter. Create an owned PostgreSQL database, then request development schema provisioning explicitly:

```sh
ConnectionStrings__Marten='Host=localhost;Port=15432;Database=troolio_extensions;Username=postgres;Password=development' \
dotnet run --project Sample/MartenCounter -- --Marten:CreateSchema=true
```

Run again with the same counter ID to activate and replay only that actor's stored history. The program increments once and immediately retries the same business operation, which must not increment twice. Supply the same `Counter:OperationId` to retry the most recent operation across processes.

```csharp
public void On(CounterIncremented e) => State = State.Apply(e);
public CounterState Handle(GetCount _) => State;
```

The domain state contains the last business operation and its amount. This intentionally bounded demonstration remembers the most recent operation; an application needing retries over a longer window chooses a durable operation ledger or another bounded deduplication policy. Diagnostic `MessageId` values are not business operation keys.

Use the sample's [complete host setup](https://github.com/Fifty3North/troolio/blob/main/Sample/MartenCounter/Program.cs) for registration and disposal. A production host provisions the schema separately and configures clustering and read-side delivery explicitly.
