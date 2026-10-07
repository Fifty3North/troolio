# ToDo runtime modernisation plan

## Scope

Replace the prototype handwritten event bus and Testing bypass with one actual Troolio silo, ordinary typed event-sourced actors, live awaited EF projection and durable SQLite reads. Keep the historical folder name. Preserve the browser UI and expose configurable gRPC event storage through the retained extension.

## Research

The requested context7 and microsoft-learn tools are unavailable in this session. Reviewed current Troolio 10.0.8 SiloExtensions, EventSourcedActor state replay, Messages, FileSystemStore and original Workshop actor; inspected EF extension mappings and coordinated the awaited modern adapter API. Official references: [Orleans ASP.NET hosting](https://learn.microsoft.com/en-us/dotnet/orleans/quickstarts/build-your-first-orleans-app), [DbContext lifetime](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/), [Orleans local configuration](https://dotnet.github.io/orleans/docs/host/configuration-guide/local-development-configuration/).

## Design

Application startup starts one TroolioServer host and stops it through the web application's hosted-service lifecycle. API commands use that host's actor system. Defaults use FileSystemStore and SQLite, requiring no external service. An explicit InMemory mode is temporary development storage. Configurable EventStore mode uses the public gRPC adapter. SQLite persists across application restarts. Actor reducers only restore their own state; no startup stream enumeration or global replay exists. Owner identity comes from server demo configuration, never client user headers. Correlation ID is a validated diagnostic header; business retry uses the requested todo ID and title.

## Validation

Run affected projects only with bounded MSBuild and disabled shared compilation. Actual HTTP/runtime tests cover create/list/complete, invalid input, missing targets, duplicate create/complete, ownership enforcement, durable restart and pure own-stream replay with a malformed unrelated stream. Runtime tests use separate local ports and durable temporary SQLite/FileSystem data. Coordinator owns final package-reference replacement and public package consumer verification.
