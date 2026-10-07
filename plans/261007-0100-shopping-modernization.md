# Shopping modernization plan

Scope: ShoppingListSample C# hosts, actor contracts, SQLite read models, tests and sample documentation. Preserve the supplied dirty baseline snapshot.

1. Replace sibling framework source references with public Core 10.0.8 and Orleans 10 generator packages. Consume the retained EF extension locally until publication.
2. Use SQLite 10.0.12 in both host and API, resolving one configured absolute database path. Enable live list, item and membership projections. Serve bounded catalogs using SQL.
3. Keep replay reducers pure. Make repeated orchestration commands idempotent and forward original event metadata. Individual actor queries enforce contributor authorization.
4. Add explicit development-only fixed demo identities; reject impersonation and deny unauthenticated reads and telemetry. Production requires a real authentication implementation.
5. Replace legacy tests with real actor runtime plus relational SQLite tests covering collaboration, authorization, projection freshness, duplicate delivery, and own-stream replay. Bound test ports and compiler concurrency.
6. Document supported local run commands, domain examples, eventual delivery and storage limitations.

Research: current Core source SiloExtensions/CqrsActor/EventSourcedActor, published Core API, Microsoft EF SQLite provider and limitations, Orleans serialization documentation. Requested context7 and microsoft-learn MCP tools are unavailable, so direct official documentation and current source supply the reference.

Sources: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/ and https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/serialization
