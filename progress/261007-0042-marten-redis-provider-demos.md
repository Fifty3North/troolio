# Marten and Redis runnable provider demos

Worker /root/modernize_todo. Worktree /home/fedora/t/worktrees/troolio-samples-todo-261007, branch task/samples-todo-261007, base f25b996. Scope only new Sample/MartenCounter, Sample/RedisReadModels and this task's timestamp plan/progress. ToDo, root props, solutions and providers were not edited.

## Implemented

MartenCounter: actual public Core 10.0.8 TroolioServer<MartenStore>, explicit Orleans SDK10, host-owned document store, explicit optional development schema provisioning, stable counter key, immutable pure actor reducer, command/query and exact last-operation retry checks. New process activation reads the counter's own stream. Application-owned local silo/gateway19320/19321 avoid other fixture ports.

RedisReadModels: one reusable disposable connection; asynchronous create, selected-field update, entity read and delete; unique entity/partition IDs; bounded GetChangePageAsync requests; assertions of committed cursor order and deletion. No key enumeration or global flushing. Both READMEs include run/install, source concepts, diagrams, identity boundaries and provider-specific limits.

Projects default to published extension PackageReference using the central extension version. Prepublication checks used UseLocalExtensions=true and TroolioExtensionsSourceRoot pointing to the adapter worker's current source; public Core stayed a NuGet dependency. No generated cache directories were copied.

## Real provider smoke

Marten endpoint: owned PostgreSQL localhost15432, database troolio_extensions. Schema explicitly provisioned on the first run only. The same stable CounterActor key44444444-4444-4444-4444-444444444444 produced these console results:

- First process: 0 -> 1; immediate duplicate retained1.
- Second process, schema auto-create disabled: 1 -> 2; immediate duplicate retained2.
- Third process reused the second run's operation ID: 2 -> 2; persisted last-operation retry remained idempotent across process restart.

Logs /tmp/troolio-marten-counter-run1.log, /tmp/troolio-marten-counter-run2.log, /tmp/troolio-marten-counter-retry.log. All processes exited successfully. No event history reset occurred.

Redis endpoint: owned localhost16379. Unique partition20d4b3c5-f0b2-4499-93c3-198e34e3fa51, card40c61bf6-ed65-439f-9c9f-4425880152e1. Create/read, selective completion update/read and delete/absence passed. Change pages returned2 then1 entries, offsets2 then3, exact create/update/delete cursor order. Log /tmp/troolio-redis-readmodels-run.log. Process exited successfully. The card was deleted; its small partition feed remains intentionally available.

Both builds used MSBUILDDISABLENODEREUSE=1 and UseSharedCompilation=false, with -m:2 supplied for the initial dotnet run build. No build warnings appeared. Edited new project/source/README files use CRLF. Git diff whitespace check passed with core.whitespace=cr-at-eol.

## Coordinator integration

Add both projects to the retained sample solution. Publish the retained extension versions, then repeat the two runnable journeys using default public PackageReferences without UseLocalExtensions. Provider publication, public docs updates and GitHub pushes remain coordinator scope. No worker application or owned fixture process remains active.
