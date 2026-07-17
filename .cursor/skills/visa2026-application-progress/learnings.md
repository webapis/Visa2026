# Application progress — learnings (append-only)

Read **before** progress/approval work; **append** after verified fixes. Promotion rules: [MATURITY.md](./MATURITY.md).

---

## Entries

### 2026-07-17 — ApprovalLegProfile column on Applications (via ministry) ListView

- **Request**: Show **Approval leg profile** on `Application_ListView_ViaMinistries` only (not Direct migration / default Application list).
- **Approach**: Keep `[VisibleInListView(false)]` on `Application.ApprovalLegProfile`; enable the column via model `Index` on ViaMinistries + `CustomViewClonerUpdater.SetColumnVisibility`; captions in DesignedDiffs + tr/ru/tk localization; Blazor `Model.xafml` column width/index.
- **Preload**: `ApplicationListViewPreloadController` Includes `ApprovalLegProfile.MinistryLegs.ApprovingMinistry` so `DefaultProperty` `MinistriesLabel` (e.g. Türkmenenergo-Energetika) renders without N+1.
- **Prevent**: Do not flip `VisibleInListView(true)` globally — that would pollute Direct migration lists; mirror Person TemporaryVisitors `ProjectContract` pattern (view-specific Index).
- **Cross-skill**: —
### 2026-07-10 — Ministrlik empty on ApplicationProgress (missing snapshots)

- **Symptom**: Progress detail **Ministrlik** blank; Progress list status shows `1st ministry review approved` without `- Energetika` (or similar).
- **Root cause**: `MinistryStepLabel` / `StatusListLabel` only read `Application.ApprovalLegSnapshots`. Snapshots are created when the officer changes **ApprovalLegProfile** in the Application detail controller, or during data-import `OnSaving`. Imported / older apps often have a profile + ministry progress rows but **empty snapshots**.
- **Fix (runtime)**: `GetMinistryShortNameForLeg` falls back to live `ApprovalLegProfile.MinistryLegs` → `ApprovingMinistry.ShortNameTm`. `EnsureSnapshots` heals incomplete snapshots on every Application / Progress save.
- **Fix (migrated data)**: `patch/Application-ApprovalLegSnapshots.ps1` / `--backfill-application-approval-leg-snapshots` — do **not** use `ApplicationProgress-MinistryLegs.ps1` (deletes progress).
- **Prevent**: Do not resolve ministry labels from snapshots alone when `ApprovalLegProfile` is set; keep snapshot heal on save for SLA / persistence.
- **Cross-skill**: visa2014-to-visa2026-import | visa2026-onprem-legacy-sync

### 2026-06-15 — Migration service SLA (Phase 2)

- **Scope**: `ApplicationMigrationSlaProfile` tenant lookup + per-`ApplicationType` FK; runtime via `ApplicationMigrationSlaHelper`; ListView columns `WorkingDaysInMigrationStep`, `MigrationSlaStatement`; row tint merged into `ProgressSlaAppearanceCode` (ministry first, then migration).
- **Clock**: `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE` → terminal `PROCESS_ISSUED` / `PROCESS_REJECTED` / `PROCESS_CANCELLED`; Mon–Fri `WorkingDaysHelper`.
- **Validation**: Block progress save to migration step when type lacks profile or `MaxDaysInReview` ≤ 0; warn (non-blocking) on `ApplicationType` save without profile; block profile delete when referenced (`DeleteBehavior.Restrict` + controller).
- **Seed**: `tenant/application-migration-sla-profile.json` (manifest v13); `MigrationSlaProfileCode` on all rows in `ApplicationTypeConfigurationCatalog.json`; wired in `ApplicationTypeConfigurationUpdater` after `LookupCatalogSyncUpdater`.
- **Docs**: [`docs/APPLICATION_PROGRESS_MIGRATION_SLA.md`](../../../docs/APPLICATION_PROGRESS_MIGRATION_SLA.md). Office prep SLA not in scope.
- **Prevent**: Do not add new `ApplicationState` for migration SLA — computed signal only (same as ministry). Reuse `APP_PROGRESS_SLA_*` appearance keys.
- **Cross-skill**: visa2026-bo-state-colors (row CSS) | visa2026-lookup-data (tenant catalog)

### 2026-06-13 — MinistryLetterFileID schema drift (Invalid column name)

- **Symptom**: `SqlException: Invalid column name 'MinistryLetterFileID'` when opening Applications (via ministry) list — stack through lazy-loaded `ProgressHistory` and `PrimaryStateCode`.
- **Try**: Reproduce on DB that existed before `MinistryLetterFile` property; check `ApplicationProgresses` columns.
- **Test**: Restart app after updater; confirm column + FK exist; list loads without SQL error.
- **Root cause**: EF model mapped `MinistryLetterFile` before SQL column existed on upgraded databases.
- **Fix**: `ApplicationProgressMinistryLetterFileSchemaUpdater` + idempotent SQL in `ApplicationProgressMinistryLetterFileSchemaSql`; register in `Module.GetModuleUpdaters` **before** schema sync.
- **Prevent**: Any new scalar FK on `ApplicationProgress` → pre-schema SQL updater pattern; see [reference.md](./reference.md) § Ministry decision letter.
- **Cross-skill**: lifecycle-docker (deploy) | —

### 2026-06-13 — ApprovalLegSnapshots Clear() breaks EF tracking

- **Symptom**: Exception or lost snapshots when replacing ministry legs on contract change.
- **Root cause**: `ObservableCollection.Clear()` sends Reset notification; EF Core change tracker rejects it on aggregated children.
- **Fix**: Delete existing snapshot rows via `ObjectSpace.Delete` in a loop (`ProjectContractMinistryHelper.ApplySnapshot`).
- **Prevent**: Never `Clear()` on XAF aggregated collections — delete entities individually.
- **Cross-skill**: —

### 2026-06-13 — Active contract SLA fields required (Option C)

- **Requirement**: Admin must set expected working days per ministry leg before a `ProjectContract` can be saved as active; officers see SLA warning/overdue on Application ListView.
- **Model**: `ProjectContractMinistryLeg.MaxDaysInReview` + optional `WarningDaysBeforeMax`; copied to `ApplicationApprovalLegSnapshot` on contract selection.
- **Validation**: `ProjectContractMinistryController` + `ProjectContractMinistryHelper.TryValidateLegSla` block commit when `IsActive` and any leg lacks `MaxDaysInReview > 0` or warning ≥ max. Class-level `RuleCriteria` for positive values and warning &lt; max; no `RuleRequiredField` on leg (allows draft leg setup).
- **SLA clock**: Only on `{n}_REVIEW_STARTED`; Mon–Fri via `WorkingDaysHelper`; `ApplicationProgressSlaHelper` + ListView fields `WorkingDaysInCurrentStep`, `ProgressSlaStatement`; row tint via `APP_PROGRESS_SLA_WARNING` / `APP_PROGRESS_SLA_OVERDUE` (overrides workflow color in `ApplicationProgressRowAppearanceController`).
- **Seed/backfill**: Defaults max 10 / warn 8 in `ProjectContractMinistrySeedUpdater` + `ProjectContractMinistryLegSlaBackfillUpdater`.
- **Prevent**: Do not auto-advance progress for overdue; do not add new `ApplicationState` for SLA — computed signal only.
- **Cross-skill**: visa2026-bo-state-colors (row CSS) | —

### 2026-06-13 — RuleCriteria on scalar property breaks login

- **Symptom**: `InvalidOperationException: The 'RuleCriteria' rule can be applied only to class or collection property` on `ProjectContractMinistryLeg.MaxDaysInReview` — app fails at startup / login.
- **Root cause**: `[RuleCriteria]` was placed on scalar `int?` properties; XAF only allows it on the class or on collection properties.
- **Fix**: Move SLA leg rules to **class-level** `[RuleCriteria]` on `ProjectContractMinistryLeg` (same pattern as `WorkPermitItem`, `ApplicationProgress`). Active-contract `MaxDaysInReview` requirement stays in `ProjectContractMinistryController` / `TryValidateLegSla`.
- **Prevent**: Never put `[RuleCriteria]` on scalar members — use class-level criteria or `[RuleValueComparison]` for single-field checks.
- **Cross-skill**: —

### 2026-06-13 — ProjectContractMinistryLeg FK on new contract save

- **Symptom**: `SaveAndClose` on new **Configuration → Project contract** with ministry legs fails: `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId`.
- **Root cause**: Explicit `ProjectContractId` scalar stayed `Guid.Empty` while nested list only populated `MinistryLegs` collection (no back-reference). EF persisted the empty scalar. `OnSaving` alone was insufficient when navigation was null.
- **Fix**: Restore explicit `ProjectContractId`; sync in `WireMinistryLegs` / `PrepareLegsForCommit` / `OnSaving`. App-wide `ProjectContractMinistryLegSaveController` (`WindowController` + `ObjectSpaceCreated`) wires legs on every commit — shadow-only FK inserted NULL under XAF EF.
- **Prevent**: Child BOs with aggregated nested lists — navigation-only parent FK (like `ProjectContractImage`) or wire back-reference before commit.
- **Cross-skill**: —

## 2026-06-15 — ProjectContract ministry leg FK (navigation-only follow-up)

- **Symptom**: SaveAndClose on Project Contract still failed with `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId` after scalar sync attempts.
- **Root cause**: Explicit `ProjectContractId` on the leg let EF insert the child with a FK scalar before the parent relationship was tracked (wrong insert order on new contracts).
- **Fix**: Drop `ProjectContractId` / `ApprovingMinistryId` from `ProjectContractMinistryLeg` BO; EF shadow FK columns via fluent API (`HasForeignKey("ProjectContractId")`). Wire `leg.ProjectContract` only; `PrepareLegsForCommit` calls `SetModified(parent)`. Added `ProjectContractMinistryLegNestedController` for nested ListView `ObjectCreated`.
- **Prevent**: Match `ProjectContractImage` pattern for aggregated children — navigation FK only, no explicit parent scalar on BO.

## 2026-06-15 — ProjectContract ministry leg New (ObjectSpace 1021)

- **Symptom**: New ministry leg from nested list → error 1021 “object belongs to another ObjectSpace” in `CreateDetailView`.
- **Root cause**: `ProjectContractMinistryLegNestedController` `ObjectCreated` assigned `MasterObject` contract and `MinistryLegs.Add(leg)` across object spaces before popup detail opened.
- **Fix**: Removed nested ListView `ObjectCreated` handler. Added `ProjectContractMinistryLegDetailDefaultsController` on leg DetailView — wire parent via `ObjectSpace.GetObject(masterContract)` when opened from `Link` + `PropertyCollectionSource`.
- **Prevent**: Never assign `MasterObject` directly to a child in `ObjectCreated`; resolve with `GetObject` in the **same** ObjectSpace as the child detail view, or rely on commit-time `WireMinistryLegs`.

## 2026-06-15 — ProjectContract ministry leg save NULL ProjectContractId

- **Symptom**: Save on leg popup → `Cannot insert NULL into column 'ProjectContractId'`.
- **Root cause**: Shadow-only FK did not populate when `ProjectContract` navigation was unset; leg popup save did not always run parent wiring before commit.
- **Fix**: Restore explicit `ProjectContractId` / `ApprovingMinistryId` + `SyncForeignKeys()` / `OnSaving`. `TryAttachLegToParent` on commit + leg detail `Committing`. Keep navigation wiring + `SetModified(parent)` for insert order.

## 2026-06-16 — Manual UI create vs JSON seed (parent already exists)

- **Symptom**: Seeded `project-contract.json` legs sync fine; manual **New** contract + ministry leg from UI → `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId`.
- **Root cause**: Seed attaches legs to **persisted** contracts (`FindContract`); UI creates parent + legs together. Explicit `ProjectContractId` scalar on the BO let EF insert the leg before the parent; Blazor leg popup can commit in a separate session.
- **Fix**: Navigation-only parent FK (shadow column) failed under XAF Blazor — NULL `ProjectContractId` on insert. Restored mapped `ProjectContractId` with deferred sync while parent `IsNewObject`; `EnsureLegInObjectSpace` copies popup-session legs into the parent root session; `[RuleRequiredField]` on `ProjectContract` navigation.
- **Prevent**: No mapped parent FK scalar on aggregated children; leg popup on unsaved parent must commit root `ObjectSpace`, not popup session.

## 2026-06-16 — ProjectContract ministry leg FK on Save (scalar pre-fill)

- **Symptom**: Save / SaveAndClose on new **Project contract** with ministry legs → `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId`.
- **Root cause**: `ProjectContractId` scalar was set while the parent contract was still unsaved, so EF inserted the leg before the parent row. Commit-batch detection also missed the parent when legs lived only on `MinistryLegs` (not in `GetObjectsToSave(false)`).
- **Fix**: `SyncForeignKeys` clears `ProjectContractId` until the parent is persisted; `PrepareLegsForCommit` collects legs from nested collections + `GetObjectsToSave(true)` and always `SetModified` on contracts in batch; restore parent-in-batch detection via root object space.
- **Prevent**: Never pre-fill parent FK scalar on aggregated children while parent `IsNewObject`; include nested-collection legs in commit prep (not only `GetObjectsToSave(false)`).

## 2026-06-15 — Blazor NestedFrame parent resolution

- **Symptom**: Leg popup Save still NULL `ProjectContractId`; Link-only wiring missed Blazor nested list.
- **Root cause**: `ApplicationItemCreationContext` pattern — Blazor nested UI uses `NestedFrame` + `PropertyCollectionSource`, not `Link` owner chain.
- **Fix**: `ProjectContractMinistryLegCreationContext` (NestedFrame ViewItem / ListView master / Link). Leg detail controller uses `Frame` on ObjectSaving + Committing. Object-space hooks moved to `Module.Setup` (always subscribed). `FindParentContract` falls back to single `ModifiedObjects` contract.

## 2026-06-15 — Ministry leg popup Save on new contract (nested ObjectSpace FK)

- **Symptom**: Save on ministry leg popup from **new** `ProjectContract` → `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId` (leg commits with `ProjectContractId` set but parent row not inserted).
- **Root cause**: Blazor leg popup can commit in a **nested** `ObjectSpace` while the unsaved parent contract lives in the **root** session — leg-only batch inserts the child before the parent exists.
- **Fix**: `ObjectSpaceHelper.GetRootObjectSpace` (reflection parent chain when present) and `ObjectSpaceHelper.Get(parent)` for EF Core popup sessions. `PrepareLegsForCommit` / `TryFinalizeLegCommit` wire legs on the parent session, `SetModified(parent)`, and redirect leg-only commits to `parentObjectSpace.CommitChanges()`. `WouldOrphanLegForeignKey` detects new parent missing from batch. Last resort: `ProjectContract.SaveBeforeMinistryLeg` user message.
- **Prevent**: Aggregated child popup saves on unsaved parents — resolve parent in **root** OS (`ApplicationItemPersonLinkedDefaults` pattern); do not re-add `ProjectContractMinistryLegNestedController` (`ObjectSpace` 1021).

## 2026-06-16 — Save and Close on leg popup (friendly exception before wire)

- **Symptom**: Save and Close on ministry leg popup from new contract → `UserFriendlyException`: “Save the project contract before saving this ministry leg.”
- **Root cause**: Global `ProjectContractMinistryLegObjectSpaceHooks.OnCommitting` called `TryFinalizeLegCommit` **before** `ProjectContractMinistryLegDetailDefaultsController` could wire parent from `Frame` / main window. `SaveGuard` only hooked `SaveAction`, not `SaveAndCloseAction`.
- **Fix**: Hooks `OnCommitting` → `PrepareLegsForCommit` only; leg detail `Committing` wires parent then `TryCommitParentWithLeg` then `TryFinalizeLegCommit`. `SaveGuard` hooks both Save and Save and Close. `TryFinalizeLegCommit` retries `TryCommitParentWithLeg` per leg; `FindParentContract` searches root `CollectContractsInCommitBatch`.
- **Prevent**: Global object-space hooks must not throw on leg commit before frame-aware controllers run; hook **both** Save and SaveAndClose for popup guard controllers.

## 2026-06-16 — SaveGuard throws before parent resolved (cross-session new contract)

- **Symptom**: Save / Save and Close on leg popup → `UserFriendlyException` in `ProjectContractMinistryLegSaveGuardController.SaveAction_Executing` (not Committing).
- **Root cause**: `TryBringIntoObjectSpace` returned false for an **unsaved** parent contract in the main-window session when the leg popup used a different `ObjectSpace` (`IsNewObject(source)` on target space is false). Parent never wired → `CanCommitLeg` failed in Executing.
- **Fix**: `TryBringIntoObjectSpace` returns the source instance for new parents (callers resolve owning space via `ObjectSpaceHelper.ResolveObjectSpace`). Walk parent `Frame` chain (`Parent` / `TemplateFrame`). `AttachLegInSpace` always targets the parent's session. SaveGuard wires + `TryCommitParentWithLeg` only (no throw in Executing); finalize stays on leg detail `Committing`.
- **Prevent**: Cross-session parent resolution for aggregated children must not require `GetObject` on unsaved parents; defer hard guard to Committing after frame wiring.

## 2026-06-16 — StackOverflow on ministry leg Save (reentrant CommitChanges)

- **Symptom**: `System.StackOverflowException` when Save / Save and Close on ministry leg popup (no usable stack trace in VS).
- **Root cause**: Leg `Committing` redirected to `parentObjectSpace.CommitChanges()`, which re-entered the same leg `Committing` / finalize path (`TryCommitParentWithLeg` → `CommitChanges` loop). `SaveGuard` also called `TryCommitParentWithLeg` from `Executing`, doubling entry points.
- **Fix**: `LegCommitRedirectScope` (`AsyncLocal` depth) around parent `CommitChanges`; leg detail `Committing` skips redirect when scope active; `SaveGuard` only wires parent in `Executing` (redirect stays on `Committing`).
- **Follow-up (still overflowed)**: Nested leg `ObjectSpace` still ran `PrepareLegsForCommit` / `EnsureLegInObjectSpace` / `Delete` during parent redirect; `TryFinalizeLegCommit` duplicated redirect calls; `GetRootObjectSpace` had no cycle guard.
- **Follow-up fix**: `ShouldPrepareLegsOnCommit` skips nested popup OS during redirect; per-OS `PrepareLegsForCommitScope`; `TryFinalizeLegCommit` validates only (no second redirect); no `Delete` on leg copy during redirect; cycle-safe `GetRootObjectSpace` + frame walk.
- **Prevent**: Never call `CommitChanges` from ministry-leg save handlers without a reentrancy guard; one redirect entry point (`Committing`), not `Executing` + `Committing`.

## 2026-06-16 — Save contract without ministry legs

- **Symptom**: Officers cannot save active `ProjectContract` (e.g. GT-16) until ≥1 ministry leg is configured.
- **Root cause**: `ProjectContractMinistryController` blocked commit when `IsActive && !HasConfiguredLegs`.
- **Fix**: Removed contract-save legs-required check. SLA validation (`TryValidateLegSla`) still runs when legs exist. `ApplicationProgressProfileResolver.TryValidateProjectContractOnApplication` still blocks via-ministries applications without legs once progress moves past office preparation.
- **Prevent**: Do not require ministry legs on contract save; enforce at application progress / contract selection time instead.

## 2026-06-16 — Application header lock scope + gear StackOverflow

- **Symptom**: `StackOverflowException` when clicking optional-fields gear on `ApplicationItem_DetailView` after ministry progress; officers could not edit gear-hidden registration fields on in-progress applications.
- **Root cause (gear)**: `OptionalDetailFieldsController.RefreshAfterToggle` called `AppearanceController.Refresh()`, which recursively `ToggleReadOnly` on XAF synthetic layout member `AdditionalFields`.
- **Root cause (lock)**: `ApplicationReadOnlyAfterOfficePreparation` and `TryValidateApplicationUnchangedAfterProgress` locked visa/travel fields, child tabs, and all `ApplicationItem` saves once progress left office prep — too broad for fields filled later in the workflow.
- **Fix**: Gear toggle uses imperative `OptionalDetailFieldsVisibilityApplicator` only (+ reentrancy guard). Lock narrowed to `ApplicationProgressProfileResolver.LockedApplicationHeaderTargetItems` (numbering, date, type, contract); child collection commit blocking removed.
- **Docs**: [`docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md`](../../../docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md) §3.4; [`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md) — interaction + pitfall #6.
- **Prevent**: Do not add workflow/child fields to header lock without product sign-off; do not call `AppearanceController.Refresh()` from optional-fields gear toggle on Blazor.
