# Shopping modernization progress

Integrated scope: public Core 10.0.8; Orleans 10 serializer generation; SQLite 10.0.12; retained local EF projection extension pending coordinator package publication. No sibling framework or test-kit references remain.

Completed: standalone host and API plus embedded-host option, live list/item/member projections, caller-filtered paged SQL catalogs, verified development identities, actor/read-model authorization, stable join-code registration and idempotent membership, pure replay, original README diagrams and C# examples. Removed former MySQL migrations and obsolete excluded logging filter.

Compatibility findings: Core 10.0.8 linked models need an Orleans surrogate converter for the base record; Orleans 10 initializes per-grain activator components rather than the global DI activator. The sample provides a final IConfigureGrainTypeComponents registration which initializes the existing resolver. Current runtime tests verify the resulting model folds and transport.

Validation: three actual-runtime/SQLite/HTTP tests passed. They cover create/item/join, duplicate membership, authorization and impersonation denial, bounded relational catalogs, linked-model JSON reads, own-stream replay after host restart, and startup with 10,000 malformed unrelated events without reading that stream. Standalone host and load utility Release builds both completed with zero warnings/errors. Final changed-helper pass also passed all three runtime tests. Compiler sharing disabled and MSBuild bound to two workers.

Retained limitations: development-only fixed credentials, local filesystem event storage and SQLite, volatile mock email queue, ordinary eventual projection/orchestration delivery. No production durability or whole-system rollback claim. Docker publication was not exercised by this worker; coordinator owns final integration and browser/runtime validation.

Worker branch task/samples-shopping-261007; coordinator integrates Shopping/Tests/docs commit only. Partial adapter commit is a test dependency; adapter worker owns final adapter integration.
