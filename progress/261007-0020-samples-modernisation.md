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
