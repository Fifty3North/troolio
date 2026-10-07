# ToDo runtime validation

Worker: /root/modernize_todo. Worktree: /home/fedora/t/worktrees/troolio-samples-todo-261007. Branch: task/samples-todo-261007. Base: 54c093731bc61d38dc9e9581fc0e5f0aa8925ed0. Coordinator owns integration and publication.

## Implemented

Removed handwritten event bus, aggregate, projection hosted worker and Testing bypass. Added typed actor/state/messages with explicit serialization IDs, business retry semantics, server demo identity and metadata ingress. Added a DI-owned single TroolioServer runtime, durable FileSystem development store, configurable retained gRPC store and live EF SQLite projection. Updated HTTP/OpenAPI and beginner documentation with source examples and a diagram. Removed browser package diagnostics and unserved duplicate prototype page; improved completed-task text contrast.

## Actual integrated runtime checks

Command (extension source path is a temporary prepublication build override):

```bash
TROOLIO_TODO_KURRENT_CONNECTION='esdb://localhost:12113?tls=false' \
MSBUILDDISABLENODEREUSE=1 dotnet test \
Sample/ToDo.EventStoreEfApi.Tests/ToDo.EventStoreEfApi.Tests.csproj \
--no-restore --filter TestCategory=TodoRuntime \
-p:UseSharedCompilation=false \
-p:TroolioExtensionsSourceRoot=/home/fedora/t/worktrees/troolio-samples-extensions-261007 \
-m:2 --logger 'console;verbosity=minimal'
```

Result: **4 passed, 0 failed, 0 skipped**, zero build warnings. Public Core 10.0.8 and the modern EF/gRPC adapter source were loaded by actual Orleans silos. No TestKit or handwritten transport replaced runtime behavior.

Coverage: HTTP create/complete/list, exact business retries without extra events, conflicting retry, empty/oversized input, missing target, foreign owner reads and commands, ignored untrusted user header, stamped event metadata; durable FileSystem/SQLite restart; pure actor replay with SQLite triggers rejecting projection mutations; readiness and healthy unrelated commands despite a 20,000-line malformed unrelated stream; actual KurrentDB gRPC 12113 append, host restart, replay and live projected completion. The gRPC test uses unique stream IDs and never clears the shared fixture.

Browser check through a real local API at port 5084: create, complete, active/completed filters, removed diagnostics, no browser errors, no 390px mobile horizontal overflow. Chromium used local Playwright because the requested MCP was unavailable. Screenshots: /tmp/troolio-todo-desktop.png and /tmp/troolio-todo-mobile.png. Browser process and preview app stopped, generated preview data removed from the worktree. The completed-task contrast CSS adjustment was made following mobile inspection.

Evidence log: /tmp/troolio-todo-runtime-tests.log. CRLF verified for edited source/config/docs, and git diff --check passed with core.whitespace=cr-at-eol. Earlier focused runs caught and fixed custom projection interface discovery and the incorrect immediate-SQL-visibility expectation.

## Integration requirement

Replace the two temporary extension ProjectReferences (and temporary override property) with published Troolio.Projection.EntityFramework and Troolio.Stores.EventStore package references. Re-run this same actual runtime suite against those public packages. Package publication, full samples integration, external documentation updates and GitHub pushes remain coordinator scope.
