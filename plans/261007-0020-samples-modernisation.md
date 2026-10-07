# Samples and extension modernisation

## Objective

Update Fifty3North/troolio (local trool.io-samples) to publicly available Troolio.Core 10.0.8 on .NET 10. Retain relevant learning journeys and useful optional adapters, verify them against actual published framework assemblies, publish new extension versions, and add end-user guides to the public Troolio documentation.

## Baseline and preservation

- Branch task/net10shopping, initial HEAD 7d76a6a8048ce11898eded82457e63509adfec5d.
- Existing working changes affect many files; most are CRLF conversions, with fourteen substantive tracked changes and a new untracked ToDo API/test implementation. Preserve this work; do not reset it.
- Root Directory.Build.props imports a parent Build.props and package versions are not self-contained. Multiple projects depend on sibling framework/runtime source; these references must become public package references.
- Public framework versions verified through the NuGet flat-container feed: all five framework IDs are 10.0.8. EF, EventStore and Marten extensions are currently 10.0.1; Redis has no public listing.
- Use 10.0.2 for the four extension releases, independently of the framework version. Each must depend on the current Core 10.0.8 public package.
- Preserve existing source in Git history when replacing obsolete implementations; exclude credentials, generated directories, IDE user files and local package artifacts from new commits.

## Research and instructions

Parent AGENTS.md authorizes bounded parallel work in separate worktrees; Agents.md forbids global event replay on startup and requires durable catalog read models. User instructions require CRLF, quoted Mermaid labels, timestamped planning/progress documents, source examples and end-user-only public documentation.

The required Context7 and Microsoft Learn MCP tools are not exposed in this session. Use official Microsoft Learn, upstream provider documentation and current framework source/public package metadata as the fallback before generating code.

Primary sources consulted:

- https://learn.microsoft.com/en-us/dotnet/orleans/grains/code-generation
- https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew
- https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping
- https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing
- https://github.com/NuGet/login
- https://martendb.io/events/appending.html

Provider-specific research continues inside each implementation scope. EF10 requires .NET10. Orleans code generation needs an explicit SDK reference in assemblies defining contracts and serializable types. Marten9 changes append and serializer defaults, requiring explicit compatibility choices and real PostgreSQL checks.

## Retained scope

1. Calculator: quick, headless, package-only actor/message example with graceful host shutdown.
2. Shopping List: advanced collaboration, orchestration, durable SQL catalog projections, API read models and telemetry. Repair obsolete host/API configuration, event lineage and trusted demo identity handling. Keep live projection work distinct from pure replay; replace cross-actor catalog fanout with database queries. Modernize runnable database setup and client tooling.
3. ToDo: beginner Web API using real Troolio actors, modern EventStore gRPC storage and EF relational read models. Remove the parallel hand-written aggregate/bus implementation and test-runtime bypass. Provide local default storage for easy execution and a separately verified durable backend mode.
4. Troolio.Projection.EntityFramework: modern EF integration with meaningful persistence, duplicate/retry and failure handling tests; document transaction boundaries accurately.
5. Troolio.Projection.Redis: meaningful Redis read-model adapter with corrected identity, deletion/update and partition semantics; establish package metadata and publication workflow.
6. Troolio.Stores.Marten: modern PostgreSQL/Marten ordinary actor IStore adapter with append conflicts, metadata, snapshots and links verified.
7. Troolio.Stores.EventStore: keep the established ID as an ordinary actor IStore compatibility adapter, replace unsupported TCP transport with current gRPC. This is distinct from the framework selected atomic-route KurrentDB adapter, which is not an ordinary IStore drop-in.

## Execution

One active deliverable: Modernize, validate, publish and document retained samples/extensions.

- Record audits and snapshot the existing source safely before worker edits.
- Make repository settings and package resolution self-contained and public-feed-only.
- Delegate independent adapter, Shopping actor/API, and ToDo implementation work in isolated local worktrees. Coordinator owns common project/settings files, Calculator, workflow/release tooling, integration, final verification and public documentation.
- Re-enable excluded source or replace it with runnable equivalents; no hidden legacy implementation that the main build silently ignores.
- Replace obsolete CI with .NET10 build/test and manually triggered, serialized, pinned-action OIDC publication. NuGet policy must target repository troolio, workflow publish-nuget.yml and environment nuget-production; existing trool.io policy cannot authorize this repository.
- Package only optional reusable extensions, not sample application assemblies. Validate IDs, versions, dependencies, license/readme/repository metadata and absence of sibling-source or unpublished runtime dependencies.
- Build samples first against freshly packed extensions, publish accepted artifacts, then switch all consumers to published packages and repeat an empty-cache restore/build/runtime journey.
- Add package installation/registration examples, sample walkthroughs, architecture diagrams and advanced operational boundaries to public docs. No release evidence or upstream implementation library references in public pages.
- Push reviewed source and publish docs to the already authorized public GitHub Pages repository.

## Verification

- Compile all retained projects on .NET10 without parent props or sibling framework source.
- Exercise Calculator non-interactively and verify expected sum.
- Run actual Orleans runtime/API integration tests for ToDo and Shopping, including authorization, command idempotence and relational projection updates.
- Use owned disposable Podman fixtures for Redis, PostgreSQL and Kurrent/EventStore; Docker is unavailable in this distro. Never reuse or modify unrelated running services.
- Verify adapter expected-version conflicts, round-trip metadata, snapshots/links, projection retry behavior, partition cleanup and disposal on actual relevant backends.
- Test replay remains side-effect free and startup does not scan global streams. Add a malformed or large unrelated stream test for the applicable serving boundary.
- Build the telemetry frontend and inspect its retained journeys in a browser if changed.
- Verify published extension versions are indexed and a new consumer restores only from NuGet.org.
- Validate docs links/examples/build and the deployed Pages site.

## Dependencies / pending input

NuGet.org policy creation is an authenticated account action, so the user has been asked to add the precise additional Trusted Publishing policy. Implementation and local validation can proceed independently. Existing personal username and GitHub delegated access remain authorized.

## Completion criteria

All retained samples build and have credible runtime verification; all four accepted extensions are publicly available as 10.0.2 using Core10.0.8; consumer references and comprehensive public docs describe those actual packages; reviewed source/docs are pushed; temporary worker worktrees are cleaned after integration. Do not claim completion if publication or runtime checks remain blocked.
