# Reference: Application progress & ministry approval

Companion to [SKILL.md](./SKILL.md).

---

## File map

| Area | Path |
|------|------|
| Progress BO | `Visa2026.Module/BusinessObjects/ApplicationProgress.cs` |
| Parent BO | `Visa2026.Module/BusinessObjects/Application.cs` (`ProgressHistory`, `ProjectContract`, `ApprovalLegSnapshots`) |
| Contract + legs | `ProjectContract.cs`, `ProjectContractMinistryLeg.cs`, `ApprovingMinistry.cs` |
| Snapshot child | `ApplicationApprovalLegSnapshot.cs` |
| **Deprecated** enum | `ApplicationStatus.cs` — use `ApplicationState` catalog codes only |
| Leg code builders | `ApplicationProgressLegCodes.cs` (`MaxLegCount = 5`, `IsMinistryDecisionStateCode`) |
| Stable constants | `ApplicationProgressCatalogCodes.cs`, `ApplicationProgressStateCodes`, `ApplicationProgressLocationCodes` |
| Resolver (leg count, contract rules, header lock) | `ApplicationProgressProfileResolver.cs` — `LockedApplicationHeaderTargetItems`, `HasProgressBeyondOfficePreparation`, `TryValidateApplicationUnchangedAfterProgress`; see [approval doc §3.4](../../../docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md) |
| Allowed codes / suggestions | `ApplicationProgressRouteHelper.cs` |
| Transition graph + save validation | `ApplicationProgressTransitionHelper.cs` |
| Latest row helper | `ApplicationProgressHelper.cs` |
| Contract snapshot | `ProjectContractMinistryHelper.cs` |
| ListView state code | `ApplicationProgressPrimaryStateCodeResolver.cs` → `Application.PrimaryStateCode` |
| Catalog JSON | `DatabaseUpdate/LookupCatalogs/application-state.json`, `application-location.json` |
| Tenant seed | `LookupCatalogs/tenant/project-contract.json`, `approving-ministry.json` |
| Type route seed | `ApplicationTypeConfigurationCatalog.json` |

### Controllers (Module)

| Controller | Role |
|------------|------|
| `ApplicationProgressCommitValidationController` | Block illegal progress + contract on commit |
| `ApplicationProgressDetailViewController` | Suggest next state/location on detail |
| `ApplicationProjectContractMinistryController` | Contract change → snapshot + warnings; reverts `ProjectContract` when header locked |
| `ProjectContractMinistryController` | Block empty legs / structural leg edits when referenced |
| `ApplicationProgressRowStateRefreshController` | Refresh Application list after progress save |

### Controllers (Blazor host)

| Controller | Role |
|------------|------|
| `ApplicationProgressRowAppearanceController` | DxGrid row CSS from `PrimaryStateCode` + `BoStateAppearanceColors` |

### Database updaters

| Updater | Role |
|---------|------|
| `ApplicationProgressMinistryLetterFileSchemaUpdater` | `MinistryLetterFileID` column + FK before EF schema |
| `ProjectContractMinistrySeedUpdater` | Default ministries + contract leg rows on deploy |

---

## Validation pipeline (save)

```text
ApplicationProgressCommitValidationController
  → ApplicationProgressProfileResolver.TryValidateApplicationUnchangedAfterProgress (Application — locked header only; §3.4)
  → ApplicationProgressProfileResolver.TryValidateProjectContractOnApplication (Application)
  → ApplicationProgressTransitionHelper.TryValidateProgressStep (each ApplicationProgress)
       → ApplicationProgressRouteHelper (state/location allowed for route + leg count)
       → ApplicationProgressProfileResolver.TryValidateProjectContractForProgress
       → canonical (state, location) pair
       → transition graph edge from previous row
```

Officer-facing messages: `ApplicationProgress.*`, `Application.ProjectContract*`, `ProjectContract.MinistryLegs*` in `tools/GenerateModelLocalization/UiStrings.messages.json`.

---

## Leg count resolution

```text
ApplicationProgressProfileResolver.GetMinistryLegCount(application)

1. DirectToMigrationService route → 0
2. ApprovalLegSnapshots with MinistryShortName → count
3. ShowProjectContract + ProjectContract → ProjectContractMinistryHelper.GetLegCount
4. Else → ApplicationType.MinistryReviewDepth (legacy enum → 1 or 2)
```

Transition graph built for `legCount` clamped to `ApplicationProgressLegCodes.MaxLegCount`.

---

## Ministry decision letter

| Item | Detail |
|------|--------|
| Property | `ApplicationProgress.MinistryLetterFile` → `FileData` (scalar FK `MinistryLetterFileID`) |
| UI show | `[Appearance]` `HideMinistryLetterFileUnlessDecision` when `!IsMinistryDecisionStep` |
| Decision states | `*_REVIEW_APPROVED`, `*_REVIEW_REJECTED` (any leg 1…5) |
| Schema SQL | `ApplicationProgressMinistryLetterFileSchemaSql.EnsureMinistryLetterFileIdColumnSql` |
| Register updater | `Module.GetModuleUpdaters` — **before** EF schema update on existing DBs |

---

## Adding N-th ministry leg (checklist)

1. `application-state.json`: `{n}_REVIEW_APPROVED`, `{n}_REVIEW_REJECTED` (no `_REVIEW_STARTED` — removed from active workflow)
2. `application-location.json`: `AT_THE_MINISTERY_{n}`
3. Bump `LookupCatalogs/manifest.json` version
4. Confirm `ApplicationProgressLegCodes.MaxLegCount >= n`
5. `BoStateAppearanceColors` + `BO_STATE_COLORS.md` for new codes (bo-state-colors skill)
6. Unit test: full chain office → leg n → migration in `ApplicationProgressTransitionHelperThreeLegTests` pattern

---

## Tests (Module.Tests)

| File | Covers |
|------|--------|
| `ApplicationProgressProfileResolverTests.cs` | Contract, snapshot, legacy depth |
| `ApplicationProgressTransitionHelperThreeLegTests.cs` | 3-leg happy path transitions |
| `ApplicationProgressLegCodesDecisionTests.cs` | `IsMinistryDecisionStateCode` |

```powershell
dotnet build Visa2026.Module/Visa2026.Module.csproj -c Debug -p:EnableSourceLink=false
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug -p:EnableSourceLink=false
```

---

## Cross-skill boundaries

| Question | Owner |
|----------|--------|
| Which color for `2_REVIEW_APPROVED` on Application list? | **visa2026-bo-state-colors** |
| Why is next step `PROCESS_STARTED` illegal? | **this skill** (transitions) |
| Seed GT-15 contract rows | **visa2026-lookup-data** + `ProjectContractMinistrySeedUpdater` |
| Column missing after deploy | **visa2026-lifecycle-docker** + schema updater here |
