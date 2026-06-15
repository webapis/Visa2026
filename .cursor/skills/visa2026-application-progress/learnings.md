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
