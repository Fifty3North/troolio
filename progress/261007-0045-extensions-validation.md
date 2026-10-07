# Retained extension validation and integration handoff

Worker: `/root/audit_extensions`. Worktree: `/home/fedora/t/worktrees/troolio-samples-extensions-261007`. Branch: `task/samples-extensions-261007`. Base: `54c093731bc61d38dc9e9581fc0e5f0aa8925ed0`. Initial adapter commit: `e6e22e8`. Coordinator owns integration, full sample journeys, CI, publication, and removal of the clean temporary worktree after integration.

## Changes

All four packages retained at candidate version10.0.2 with public Core10.0.8, MIT license expressions, repository metadata and substantial package readmes with original C# examples. EF uses the public projection lifecycle and awaited database writes; create/delete are idempotent by GUID key, failures propagate, projection interfaces inherit IProjectionActor. Removed obsolete EFPersistance, unused batch queue helpers and retired DEBUG/provider-autodetection store tests. EventStore package retains its ID and ordinary IStore role using injected KurrentDB.Client1.4.1 gRPC. Marten9.46 uses explicit Newtonsoft wrapper compatibility, Rich append mode, host-owned provisioned stores and bounded link recursion. Redis3.3.1 fixes property-expression corruption, concurrent cursor ownership, connection lifecycle, transaction completion/failure propagation and bounded asynchronous feed/entity reads. Package examples explain ordinary/selected transaction separation, live projection versus pure replay, authorization and actual recovery limits.

## Verification

New unified suite: `Stores/Troolio.Extensions.Tests/Troolio.Extensions.Tests.csproj`.

Source mode: `-p:UseLocalExtensions=true`. Default package mode references the four public package IDs at TroolioExtensionsVersion. Before publication, supply a local candidate feed through RestoreAdditionalProjectSources.

All four Release packages packed without warnings/errors. Candidate metadata checks confirmed Core10.0.8 dependency, no external implementation-library packages, root package-readme.md, correct public guide paths and license expressions. Text edits use CRLF. Whitespace check: `git -c core.whitespace=cr-at-eol diff --check`.

Final complete package-consumer suite:20 passed,0 failed,0 skipped; restore used an initially empty `/tmp/troolio-extensions-final-package-cache-261007` and candidate packages from `/tmp/troolio-extensions-packed-final-261007`. Core and third-party dependencies were restored from NuGet.org. This is validation of packed candidates, not evidence of public publication.

Evidence: `/tmp/troolio-extensions-tests-261007/extensions-final-packaged-api.trx` (20); earlier full source `extensions-release-final.trx` (18), focused final Redis `extensions-redis-mapping-final.trx` (4), focused final EF `extensions-ef-database-final.trx` (4). Initial failures retained in extensions-initial.trx/extensions-corrected.trx/extensions-expanded.trx/extensions-release.trx. They revealed test-probe actor-interface requirements, a fixture startup readiness race, Redis synchronous reads on async completion threads, and memory-stream publication returning before eventual delivery. Fixed runtime/test interfaces; async Redis APIs avoid blocking; lifecycle test awaits observable persisted state and actual deactivation instead of assuming publication means delivery.

Coverage: both durable stores round-trip all metadata, inclusive versions, link-stream location semantics, same-named events in separate namespaces, typed snapshots, conflicting concurrent expected-version appends,300-event streams, fresh readers and independent healthy streams after malformed-stream errors. EF tests run a real Orleans host against SQLite and cover duplicate create/delete, actual database constraint failures, subsequent successful writes, live memory-stream delivery and actual deactivate/reactivate subscription resumption. Redis tests use a real server and cover selected-property mappings, missing-entity membership failure, concurrent cursor-to-entity correspondence, paging bounds and connection configuration isolation.

## Owned fixtures for coordinator gates

Keep these task-owned containers running until integrated runtime/CI validation is complete:

- `troolio-samples-redis-261007`: Redis8.2-alpine, localhost16379. `TROOLIO_TEST_REDIS=localhost:16379`.
- `troolio-samples-postgres-261007`: PostgreSQL17-alpine, localhost15432. `TROOLIO_TEST_MARTEN=Host=localhost;Port=15432;Database=troolio_extensions;Username=postgres;Password=development`.
- `troolio-samples-kurrent-261007`: kurrentplatform/kurrentdb26.0.1, localhost12113. `TROOLIO_TEST_EVENTSTORE=esdb://localhost:12113?tls=false`.

Readiness: Redis PING, PostgreSQL pg_isready and Kurrent HTTP /health/live. Tests use unique stream and partition IDs, never global Clear. Existing14358/14388–14391 fixtures were untouched. The earlier stopped task-owned cached-latest Kurrent container was removed; no extra task fixture remains.

## Concrete compatibility limits

Existing production Marten databases require reviewed schema changes and validation of historical fixtures; current suite tests the preserved wrapper shape and modern round-trips, not an upgrade of every legacy production dataset. Clear is intentionally unsupported for durable shared history. EF generic writes do not supply an atomic inbox/outbox/checkpoint. Redis simple CLR-name keys and standalone-server multi-key transactions preserve the existing layout; use distinct model names, remove other memberships before global entity deletion, and coordinate retention with snapshot/reload because absolute page offsets assume untrimmed history. These application responsibilities are described in end-user package documentation.
