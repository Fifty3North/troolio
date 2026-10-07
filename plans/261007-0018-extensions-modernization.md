# Retained extension modernization

Base: 54c093731bc61d38dc9e9581fc0e5f0aa8925ed0. Scope: four optional extensions and their adapter tests. Coordinator integrates and publishes.

Preserve EF mapping APIs while using current projection lifecycle and awaited writes. Replace deprecated EventStore TCP transport with injectable KurrentDB gRPC client. Keep ordinary IStore separate from selected atomic route APIs. Upgrade Marten with explicit Newtonsoft compatibility and Rich expected-version append mode; host owns database provisioning. Fix Redis null property expressions, cursor races, per-provider connection ownership, transaction failure handling, bounded change reads. Package all four against public Core10.0.8.

Research completed before changes: current framework ProjectionActor/IStore/source and existing provider implementation, public package nuspecs, Kurrent deprecated TCP docs, Marten9 migration and append/serializer docs, EF context lifetime docs, StackExchange.Redis basics. Context7 and microsoft-learn tools are unavailable; used official sources and published API assemblies.

Focused checks: package restores/builds, EF SQLite mutation/failure tests, Redis real server tests including concurrent cursors, PostgreSQL Marten round-trip/version/link/restart tests, Kurrent ordinary-store round-trip/conflict/link tests. Use disposable owned fixture ports. No credentials/publications.
