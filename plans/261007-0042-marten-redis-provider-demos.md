# Retained provider demonstrations

Worker /root/modernize_todo, branch task/samples-todo-261007, base f25b996. Scope is two new sample directories and this plan/progress; no ToDo, root props, solutions, adapter edits, publication or pushes.

## Research

Requested context7/microsoft-learn MCP tools remain unavailable. Reviewed actual retained MartenStore.Configure and host-owned document-store constructors, Redis provider async CRUD/change-page APIs, provider test fixtures, current public Core 10.0.8 hosting and serializer requirements. Official references: [Marten StoreOptions](https://martendb.io/configuration/storeoptions), [Marten schema provisioning](https://martendb.io/schema/), [StackExchange.Redis connection ownership](https://github.com/StackExchange/StackExchange.Redis/blob/main/docs/Basics.md), [Redis transactions](https://stackexchange.github.io/StackExchange.Redis/Transactions).

## Implementation

MartenCounter runs an actual TroolioServer<MartenStore> host with explicit local Orleans configuration, public Core and SDK10. Its immutable actor state replays only its own stream; one stable counter ID persists across process runs. DI owns document-store lifetime. Schema provisioning is a deliberate development option against an existing owned database; no database creation or global history reset.

RedisReadModels owns one reusable connection, uses asynchronous provider operations, a unique entity ID and partition, bounded change-page offsets and an explicit delete. It performs no Redis key enumeration, global flush or unbounded change-feed scan. Each console demonstrates a real end-user workflow and documents source concepts, identity and production limits.

Projects default to public package references. UseLocalExtensions=true and an overridable TroolioExtensionsSourceRoot enable prepublication validation against the adapter worker's source without copying generated caches. The coordinator owns public package verification and solution integration.

## Validation

Build/run only these two projects with MSBUILDDISABLENODEREUSE=1, UseSharedCompilation=false and bounded MSBuild. Run MartenCounter twice using the same actor key and verify the second reads the previous count before its increment. Run Redis CRUD and bounded paging against the owned local Redis fixture; assert deletion and exact change history. Keep operational evidence in progress, not public end-user docs.
