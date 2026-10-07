# Progress

Active deliverable: Modernize, validate, publish and document retained samples/extensions.

Initial audits running: Codex /root/audit_extensions and /root/audit_samples (actual collaboration identifiers). Existing changes preserved. Public Core10.0.8 verified. New samples-repository NuGet trust policy requested; work continues. No implementation or publication claims yet.

## 00:40 integration

- Calculator migrated to Core10.0.8 package, explicit Orleans SDK generation, immutable actor state, headless real-runtime smoke: 61, build zero warnings/errors.
- Self-contained versions and public-only NuGet configuration; original dirty source preserved in private Git snapshot54c093731bc61d38dc9e9581fc0e5f0aa8925ed0.
- Scoped EF/EventStore partiale6e22e8 and ToDof25b996 applied without resetting original work. Coordinator actual ToDo runtime tests4/4passed with Kurrent12113, FileSystem/SQLite restart and pure replay evidence.
- Extension worker reports17realbackendchecks passing; finalhandoffpending. Shopping worker tracing actual publicCore linkedmodel compatibility; official Orleans10 per-type activator findings from independent read-only reviewer. No framework publication changes made.
- Modern Vue/Vite/Axios build passed; TypeScript5.9 retained because current vue-tsc fails with TypeScript7. Browser integrated Shopping journey pending backend handoff.
- Manual main-only OIDC release workflow, productionenvironmentmainbranch policy and NUGET_USER=andyhoyle configured in Fifty3North/troolio; NuGet-sidepolicyrequested, no userconfirmationyet. Seven release safeguard regressiontests pass.
- Publicsourceguides/workflow buildmerge started; finalproviderexamples and publication/indexing stillpending.

Worker worktrees retained pending integration: /home/fedora/t/worktrees/troolio-samples-extensions-261007 (task/samples-extensions-261007; active), /home/fedora/t/worktrees/troolio-samples-shopping-261007 (task/samples-shopping-261007; active), /home/fedora/t/worktrees/troolio-samples-todo-261007 (task/samples-todo-261007; completed f25b996, clean, integrated scopedpatch, coordinatorfinalcommitandcleanup pending).

## Integrated candidate

All worker implementation patches integrated into the original checkout without resetting the initial dirty work. Worker source commits: EF/EventStoree6e22e8 +extensions8699813; Shopping0ae38548fe16b9491239cb92110f6fea3fe3a349; ToDof25b996 +providerdemos5401345. Primary added public/default conditional references, bounded ToDoSQL pages+test, frontendidentity/catalogmapping/pagination/tracing fixes, metadataheaders/configuration, self-containedsolutions/settings and release/docs tooling.

Integrated gate: all17projects build0warnings/errors; Shopping3 +ToDo5 +extensions20 =28 passed,0failed,0skipped. Calculator returns61; persistedMartenCounter advances2to3 with immediateexactretryunchanged; RedisasyncCRUDandboundedpagespass. Nine release safeguards pass, including retainedSourceCoreversion lookup. Workflowlintpass (olderactionlint lacksnewqueuekey, specificschemawarningexcluded).

RealPlaywright journeys: ToDocreatecomplete/filter/paging390pxpass; ShoppingAlicecreate/item/cross-off, Bobjoin/currentcatalog/hiddeninvitecode390pxpass; realtracingenable/command/flush/messageinspectionpass. A browserprobe initiallywaitedfornativeoptionvisibilityinsteadattachedDOM; correctedprobe. Switchingtabsinitiallyunmountedtracingcontrols andforgotclientstate; fixedwithv-showandreranlivejourney.

Publicdocs52guidesbuilt; localguide links valid; sevennew/updatedguidescontain source examples and have no mobileoverflow. DownloadableWorkshopCore10.0.8/SQL10.0.8build0warnings/errors. Generatedbin/objareverificationartifactsonly andmustnotbe copiedtoPages. Excludedlibraryname/releaseevidencepatternscan mustrestrictpublishedsourcefiles, notcompilerdependencyJSON.

Readonlyrelease reviewer /root/audit_samples identified old-artifact recovery beingvalidatedagainstcurrentCore version; fixed byreadingpropsfromacceptedsourcecommit andcoveredwithregressiontests. Fixed403authorizationdocsnippet.

Publication remainspending: additional NuGet-sidepolicyrequestedforFifty3North/troolio; sourcePR/CI/main release dispatch next. Userhasnotyetconfirmedpolicycreation.

## Main integration and release consumer correction

PR #21 merged at `4c265e2`; both latest branch/PR CI runs succeeded. All three worker worktrees were clean, reviewed patches were integrated (extension source identical after normalizing line endings; Shopping and ToDo differences are documented coordinator fixes), and disposable checkouts were removed. Worker branches and evidence remain.

Release run 37550711609 passed the runtime suite and packing but failed before tag reservation or publication: its empty NuGet cache was nested inside the consumer project, so the SDK's recursive C# glob compiled package-cache content files. Move the isolated cache beside the project. A regression exercises the actual SDK Compile item evaluation with a cached C# trap; all ten safeguards pass. The runtime source is unchanged, so the latest full-suite result is reused for this focused correction.

A fresh final site contains 52 guides, valid internal links, no generated bin/obj paths, and no excluded library or release-evidence text. Pages publication still waits for actual package availability. Owned Shopping/UI/docs previews stopped; three provider fixtures retained for the pending public-package regression.

## 01:22 release authorization handoff

Source is on samples main at `78009d8d9a088b444f2f945121eb78854d1fc111`; PRs #21 and #22 merged normally after CI success. Release run https://github.com/Fifty3North/troolio/actions/runs/37551272220 passed all 28 tests (zero skipped), packed the four 10.0.2 extensions and compiled their APIs from a fresh isolated package cache with zero warnings/errors. Exact packages and manifest are retained under draft tag `nuget/v10.0.2`, pointing to that accepted source commit; a local backup is in `artifacts/retained-10.0.2`.

NuGet login then failed HTTP 401: no matching trust policy for `andyhoyle`. No package upload was attempted. Requested NuGet-side policy: owner Fifty3North, repository troolio, workflow filename publish-nuget.yml, environment nuget-production, Troolio.* glob, new-package/new-version scopes. This account action is required; GitHub configuration is already complete. Policy guidance: https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing.

After the user creates the policy, resume the accepted bytes with:

```sh
gh workflow run publish-nuget.yml --repo Fifty3North/troolio --ref main -f resume_version=10.0.2
```

Then require successful public indexing and public consumer checks, run the sample verification with `--published`, and publish the already staged site in `/tmp/troolio-docs-public`. The site update is intentionally pending so it does not advertise unpublished packages. Source guides are versioned in `docs/public`; final generated output is `artifacts/public-site-final`.

Completed worker worktrees removed, their branches retained. Owned browser servers stopped. Provider containers will be stopped while waiting, preserving their data; restart only troolio-samples-redis-261007, troolio-samples-postgres-261007 and troolio-samples-kurrent-261007 before local public-package tests. Existing unrelated framework fixtures are untouched. The deliverable remains incomplete until NuGet and Pages publication succeed.

Actual recovery check completed successfully: it downloaded the retained draft bundle, verified package payloads and original Core dependency version, and prepared 10.0.2 without rebuilding. All three owned provider containers are stopped with data preserved. Only this progress file remains locally modified; application/provider changes and release tooling are pushed on main. The public docs checkout contains the staged unpublished site update.

## Completed publication

The user confirmed the additional NuGet policy was created. Trusted Publishing authenticated successfully in https://github.com/Fifty3North/troolio/actions/runs/37551770011, which resumed the retained accepted artifacts without rebuilding. All four extensions are public at 10.0.2: Troolio.Projection.EntityFramework, Troolio.Projection.Redis, Troolio.Stores.EventStore and Troolio.Stores.Marten. The release is https://github.com/Fifty3North/troolio/releases/tag/nuget/v10.0.2; its accepted source remains `78009d8d9a088b444f2f945121eb78854d1fc111`. Public payload comparison and a fresh public consumer compile succeeded with zero warnings/errors.

Ran `scripts/verify.py --published` using fresh package and HTTP caches outside project directories. All 17 projects built with zero warnings/errors; Shopping 3, ToDo 5 and extensions 20 passed, zero skipped. Calculator returned 61; persistent Marten Counter and Redis CRUD/change pages completed; the Shopping frontend built. Package assets explicitly resolve the extension dependencies as NuGet packages, and test/application output DLL hashes match the restored public package DLLs. The prior actual browser application journeys remain applicable to these identical published provider payloads. TRX files are in `artifacts/public-test-results`; the gate log is preserved in `/home/fedora/t/backups/troolio-samples-261007-0023/published-sample-gate.log`.

Published the expanded public documentation at https://fifty3north.github.io/troolio-docs/ from docs commit `ade57d2da20ad086da14e66ddcde83ea3a6d28fb`. Pages deployment https://github.com/Fifty3North/troolio-docs/actions/runs/37552433264 succeeded. The site has 52 authored guides, including the new sample and extension guides, current package installation instructions, source examples and the updated downloadable Workshop. Live Playwright checks passed eight relevant guides, current version text, search results, mobile navigation/layout, absence of excluded implementation names and release-evidence text, and the download. Public docs checkout is clean.

All worker worktrees were removed after reviewed integration, with branch references retained. Owned browser servers and the three disposable provider containers are stopped; fixture data and useful evidence are preserved. Unrelated framework services and source were untouched. This deliverable is complete. This final progress-only update reuses the accepted runtime/package/browser results; it changes no application or release code.
