# Application progress — learnings (append-only)

Read **before** progress/approval work; **append** after verified fixes. Promotion rules: [MATURITY.md](./MATURITY.md).

---

## Entries

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

## 2026-06-15 — Blazor NestedFrame parent resolution

- **Symptom**: Leg popup Save still NULL `ProjectContractId`; Link-only wiring missed Blazor nested list.
- **Root cause**: `ApplicationItemCreationContext` pattern — Blazor nested UI uses `NestedFrame` + `PropertyCollectionSource`, not `Link` owner chain.
- **Fix**: `ProjectContractMinistryLegCreationContext` (NestedFrame ViewItem / ListView master / Link). Leg detail controller uses `Frame` on ObjectSaving + Committing. Object-space hooks moved to `Module.Setup` (always subscribed). `FindParentContract` falls back to single `ModifiedObjects` contract.

## 2026-06-15 — Ministry leg popup Save on new contract (nested ObjectSpace FK)

- **Symptom**: Save on ministry leg popup from **new** `ProjectContract` → `FK_ProjectContractMinistryLegs_ProjectContracts_ProjectContractId` (leg commits with `ProjectContractId` set but parent row not inserted).
- **Root cause**: Blazor leg popup can commit in a **nested** `ObjectSpace` while the unsaved parent contract lives in the **root** session — leg-only batch inserts the child before the parent exists.
- **Fix**: `ObjectSpaceHelper.GetRootObjectSpace` (reflection parent chain when present) and `ObjectSpaceHelper.Get(parent)` for EF Core popup sessions. `PrepareLegsForCommit` / `TryFinalizeLegCommit` wire legs on the parent session, `SetModified(parent)`, and redirect leg-only commits to `parentObjectSpace.CommitChanges()`. `WouldOrphanLegForeignKey` detects new parent missing from batch. Last resort: `ProjectContract.SaveBeforeMinistryLeg` user message.
- **Prevent**: Aggregated child popup saves on unsaved parents — resolve parent in **root** OS (`ApplicationItemPersonLinkedDefaults` pattern); do not re-add `ProjectContractMinistryLegNestedController` (`ObjectSpace` 1021).
