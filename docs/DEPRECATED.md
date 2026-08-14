# Deprecated business objects and properties

Human-readable registry for **deprecated or legacy** domain types and members in `Visa2026.Module`. Use this when refactoring, writing updaters, or answering “can we delete this?”

**Related:** Application Profile cutover — [`docs/APPLICATION_PROFILE_PLAN.md`](APPLICATION_PROFILE_PLAN.md) · skill [`.cursor/skills/visa2026-application-profile/`](../.cursor/skills/visa2026-application-profile/). State lifecycle deprecations: [`docs/STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) and [`docs/states/`](states/). Lookup seeding “no longer used” paths: [`docs/LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md).

---

## When to update this file

Add or extend a row **in the same PR** when you:

- Mark a type or member `[Obsolete]` or hide it from the UI as legacy-only.
- Stop seeding or syncing a lookup catalog while the table/column remains.
- Drop a column or table via a `ModuleUpdater` (move the row to **Removed** with updater name).

Do **not** remove EF types or DB columns until a row exists here (or in a linked feature doc) and migration impact is understood.

### Row fields

| Column | Meaning |
|--------|---------|
| **Status** | `Deprecated` — do not use in new code/UI. `Retained` — DB/import only, hidden in app. `Removed` — no longer in schema (historical). |
| **Replacement** | What to use instead (type, property, or workflow). |
| **Schema** | `Keep table`, `Keep column`, `Dropped by <Updater>`, etc. |
| **Since** | Approx. release or PR theme (optional). |

In C#, prefer `[Obsolete("…")]` with the same replacement text when the compiler allows it; for hidden legacy columns, use an XML `<summary>Legacy …</summary>` on the property.

---

## Business objects and enums

| Name | Status | Replacement | Schema | Notes |
|------|--------|-------------|--------|-------|
| **Soft delete** (Remove / Restore / Show Deleted / Recycle Bin) | Removed | Standard XAF **Delete** (hard delete) | `IsDeleted`, `DateDeleted`, `DeletedByID` dropped by `SoftDeleteColumnsCleanupUpdater`; legacy `IsDeleted = 1` rows purged on deploy | Former `ISoftDelete` / `SoftDeleteBaseObject` stack removed. |
| **RichTextMailMergeData** / Show in Mail Merge | Retained (disabled) | **User report templates** (`UserReportTemplate`), **Word reports** (`IWordReportDefinition` / Resminamalar), **Reports V2** | `RichTextMailMergeData` table retained; XAF UI off via `MailMergeFeature.Enabled = false` | Office module kept for `ContractTemplate` rich-text editor only. Re-enable: set flag, restore `RichTextMailMergeDataType` in `Startup.cs`, register `MailMergeUpdater`. |
| **MailMergeVisibility** | Retained (disabled) | Same as mail merge (above) | Table retained; hidden from model when mail merge disabled | Paired with `ShowMailMergeController` / `MailMergeUpdater`. |
| **Reports V2 predefined XtraReports** (`App_*`, registration, work permit list, etc.) | Retained (disabled) | **Word reports** (`IWordReportDefinition` / Resminamalar), **user report templates** | `ReportDataV2` rows may remain in DB; registration commented out in `ReportsUpdater.cs` | Uncomment `AddPredefinedReport` / `CreateReportVisibility` blocks to re-enable on deploy. |
| **ApplicationType** | Deprecated | **`ApplicationProfile`** (live FK on `Application`; see [`docs/APPLICATION_PROFILE_PLAN.md`](APPLICATION_PROFILE_PLAN.md)) | Table retained during dual-read; do not add new `Show*` / capability flags | UI caption **Application Type (Deprecated)**; XML docs + tooltip. No `[Obsolete]` (dual-read CS0618). All `Show*` / `CanIssue*` / route / SLA / leg-depth members are **configuration** — read via `ApplicationProfileConfigurationResolver` (profile-first, type fallback). See **Application Profile cutover** below. |
| **ApplicationTypeFilter** | Deprecated | **`ApplicationProfile`** applicability + `ApplicationProfile.SelectionCode` | Table retained; **not** in `LookupCatalogs/manifest.json` | UI caption **Application Type Filter (Deprecated)**. Still exposed read-only in security/Web API for existing FKs. See [`docs/APPLICATION_BO_TYPE_SELECTION_REFACTOR.md`](APPLICATION_BO_TYPE_SELECTION_REFACTOR.md). |
| **ApplicationTypeGroup** / **ApplicationTypeGroupMember** | Deprecated | **`ApplicationProfile.ApplicabilityCriteria`** (+ audience flags) | Tables retained | Seeded **Registration** group still drives Resminamalar type links until slice 12. Do not add new groups for officer configuration. |
| **ApplicationMigrationSlaProfile** (as **type** configuration) | Deprecated | **`ApplicationProfile.MigrationSlaDays`** | Table retained; `ApplicationTypes.MigrationSlaProfileID` retained | Lookup UI may remain for legacy rows. New SLA config belongs on the profile, not on `ApplicationType.MigrationSlaProfile`. |
| **ApplicationReason** (child of **ApplicationType**) | Retained (legacy) | Per-Application reason not in v1 profile model; field unused on `Application` | Table retained | `Application.ApplicationReason` commented out; `ShowApplicationReason` on type is legacy. |
| **UserReportTemplateApplicationType** | Deprecated | **`ApplicationProfileTemplate`** + profile nested templates (slice 12) | Join table retained | Resminamalar still unions type links with group links until template slice ships. |
| **UserReportTemplateApplicationTypeGroup** | Deprecated | **`ApplicationProfileTemplate`** + `ApplicabilityCriteria` (slice 12) | Join table retained | Same as type link row. |
| **ApplicationItem** | Removed (Phase B hard-remove) | Roster identity **`ApplicationProfileInstancePerson`** + **`ResolvedLink`**; merge/PDF projection **`ApplicationRosterMergeLine`** (plain POCO, not a BO) | `ApplicationItems` table dropped by `ApplicationItemsDropSchemaSql`; `Visas.IssuingApplicationItemID` dropped by `VisaIssuingApplicationProfileInstanceSchemaSql` | BO, doc-copies hosts, sync rules, and VISA2014 item import paths deleted. Report Dashboard SQL is M2M-only. Issued visas link via `Visa.IssuingApplicationProfileInstance`. |
| **ApplicationType `App_Visa_Ext` (702)** | Deprecated | **`App_Visa_and_WP_Ext` (708)** — Extend visa and work permit | Row retained; hidden from type-code picker | Employee visa extension only; legacy `E:7:*` imports map to 708. Migrated rows corrected via `--correct-visa-application-types`. |
| **ApplicabilityMode** (enum) | Deprecated | `UserReportTemplate.ApplicableTypeLinks`, `ApplicableProjectContractLinks`, `VisibilityCriteria` | Enum column on `UserReportTemplates` retained | `[Obsolete]` on enum and `UserReportTemplate.ApplicabilityMode`. |
| **VisaIssuingApplicationTypes** (name allowlist) | Removed | `ApplicationType.CanIssueVisa` + `ApplicationTypeCapabilities` | — | Hardcoded `ApplicationType.Name` set replaced by seeded capability flag. |
| **ApplicationStatus** (enum) | Deprecated | `ApplicationProgress` + `Application.CurrentState`; locations via **ApplicationLocation** catalog | Enum unused on `Application` BO; may remain in old import models | Docs in [`docs/BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) §8b still describe the old enum — prefer §8c progress model for new work. |

### Application Profile cutover (slices 1–9 shipped)

**Not deprecated:** **`ApplicationProgress`** (workflow history), **`ApplicationState`** catalog, per-Application **`ProjectContract`** / **`ApprovalLegProfile`** choices, or per-Application field values on `Application`. Only **configuration** that used to live on `ApplicationType` (and type-driven item visibility) moves to **`ApplicationProfile`**.

| Legacy configuration | Replacement on `ApplicationProfile` | Runtime reader |
|----------------------|-----------------------------------|----------------|
| `ApplicationProgressRoute` | `ProgressRoute` | `ApplicationProfileConfigurationResolver.GetProgressRoute` · `ApplicationProgressRouteHelper` |
| `MinistryReviewDepth` | `ApprovalLegs` (`ApplicationProfileApprovalLeg`) | `ApplicationProgressProfileResolver.GetMinistryLegCount` (embedded legs first) |
| `MigrationSlaProfile` | `MigrationSlaDays` | `ApplicationProfileConfigurationResolver` / progress SLA helpers |
| `Show*` / `CanIssue*` (Application + ApplicationItem visibility) | `ActionFamily`, produce/cancel, `Require*`, `RequirePerson*` | `Application.Cfg*` · `ApplicationProfileConfigurationResolver` |
| Type nested templates / group applicability | `NestedTemplates` · `ApplicabilityCriteria` | Slice 12 (Resminamalar) pending |
| `ApplicationType` C# / JSON seed as officer config | Profile wizard + `ApplicationProfileSeedSync` | Deploy seed only; officers use **Application Profiles** nav |

**Slice 13 (deferred):** drop `Applications.ApplicationTypeID` after import cutover — row for `Application.ApplicationType` moves to **Removed schema**.

### Lookups: seeding vs UI-only (not always “deprecated”)

| Business object | Property | Status | Replacement | Schema | Notes |
|-----------------|----------|--------|-------------|--------|-------|
| **LookupBase** | `Name` | Deprecated | **`NameTm`** (`Ady` in UI) + **`LocalizationKey`** / **`Code`** for Layer B | Column retained | Hidden in detail/list/lookup except **`ProjectContract`**. Embedded **`LookupCatalogs/*.json`** use **`NameTm` only** (not `ApplicationTypeConfigurationCatalog.json`). Global UI uses **`LocalizedDisplayName`**. |


| Name | Status | Replacement | Schema | Notes |
|------|--------|-------------|--------|-------|
| **ApplicationLocation** | Active (seeded) | — | `LookupCatalogs/application-location.json` | Catalog retained; **no longer** used on `ApplicationProgress` (progress is state-only). Layer B strings in `LookupCatalogStrings.json`. |
| **OrganizationType** | Removed | — (obsolete lookup; unused by `Application` / `ApplicationType`) | Table/FK dropped by `OrganizationTypeSchemaCleanupUpdater` | Formerly under Lookup/Organization; leftover DetailView layout nodes removed. |
| **BorderZoneLocation** | Deprecated | Comma-separated **`BorderZoneName`** on `Application`, `ApplicationItem`, and `Visa` | BO + table retained (hidden nav); **no** JSON catalog | Migrated by `ApplicationBorderZoneLocationStringUpdater` + earlier item/visa updaters. See [`docs/COMMA_SEPARATED_MULTI_SELECT.md`](COMMA_SEPARATED_MULTI_SELECT.md). Do not confuse with **ApplicationLocation**. |
| **MovementPermitLocation** | Retained (UI catalog) | Per-deployment rows in lookup UI | Table retained; excluded from manifest | See [`docs/LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md). |

---

## Properties on active business objects

| Business object | Property | Status | Replacement | Schema |
|-----------------|----------|--------|-------------|--------|
| **UserReportTemplate** | `ApplicabilityMode` | Deprecated | Applicable type/contract links + `VisibilityCriteria` | Column retained |
| **Application** | `ApplicationType` | Deprecated | **`Application.ApplicationProfile`** (live FK; set only at create) | FK retained during dual-read |
| **Application** | `ApplicationTypeQuickCode` | Deprecated | **Profile picker** at create (`ApplicationProfilePickerNewController`) | Not mapped; Blazor postback only |
| **Application** | `CreationProgressRoute` | Deprecated | **`ApplicationProfile.ProgressRoute`** + picker filter | Column retained; was type-picker route filter |
| **Application** | `ApplyDefaultsForApplicationType()` | Deprecated | **`ApplyDefaultsForApplicationProfile()`** | Method on BO; dual-read may still set Type FK on create |
| **ApplicationType** | `Show*` / `CanIssue*` (all capability and visibility flags) | Deprecated | Matching **`ApplicationProfile`** scalars (`Require*`, `Produce*`, `Cancel*`, `RequirePerson*`, …) | Columns retained on `ApplicationTypes`; do not add new flags |
| **ApplicationType** | `ApplicationProgressRoute`, `MinistryReviewDepth` | Deprecated | **`ProgressRoute`**, **`ApprovalLegs`** | Columns retained |
| **ApplicationType** | `MigrationSlaProfile` | Deprecated | **`MigrationSlaDays`** | FK retained |
| **Visa** | `IssuingApplicationItem` | Deprecated (slice 10i) | **`Visa.IssuingApplication`** → parent `Application` | FK retained for import; UI hidden when `IssuingApplication` set; backfill on deploy |
| **Application** | `ApplicationItems` collection | Deprecated (slice 10) | **`People` M2M** + auto-resolved child links | Collection retained until hard-remove |
| **ProjectContract** | `Description`, `Images`, `Documents` | Retained (legacy) | `Name` / `NameTm` / `Code` on contract; Word static text for letters | Columns retained; UI hidden |
| **Application** | `Company`, `CompanyHead`, `Representative` | Removed (Phase 3) | `Application_Company_*` / `Application_CompanyHead_*` aliases + singletons | Dropped by `OrganizationLegacySchemaCleanupUpdater` (Phase 5) |
| **Person** | `Company` | Removed (Phase 3) | Single-tenant: no per-person company FK | Dropped by `OrganizationLegacySchemaCleanupUpdater` (Phase 5) |
| **Person** | `DeclareFamilyMembersOnVisa` | Removed | `VisaApplicationFamilyMembersText` always on employee DetailView | `People.DeclareFamilyMembersOnVisa` column retained until optional schema cleanup |
| **Person** | `IsSubcontractorEmployee` | Removed | `Subcontractor` (caption **Company (Subcontractor)**) on employee DetailView without a flag | Dropped by `OrganizationLegacySchemaCleanupUpdater` |
| **Passport** | `PersonalNumber` | Retained (legacy) | `Person.PersonalNumber` | Column retained; hidden in UI |
| **Application** | `IsCancelled`, `IsRejected`, `LatestIsCancelled`, `LatestIsRejected` | Removed | `ApplicationProgress` terminal states (`PROCESS_CANCELLED`, `PROCESS_REJECTED`); `CurrentState` on list/detail | Dropped by `ApplicationLatestTerminalFlagsColumnsCleanupUpdater` |
| **Invitation** | `IsCancelled`, `IsChanged` | Removed | `InvitationItem.IsCancelled`, `InvitationItem.IsChanged`, `InvitationItem.IsUsed` | Dropped by `InvitationHeaderStatusColumnsCleanupUpdater` |
| **Invitation** | `StartDate` (property name), `ValidityDuration` | Renamed / removed | `IssuedDate` (same DB column `StartDate`); `VisaPeriod` + `VisaCategory`; `ExpirationDate` stored directly | `InvitationLegacyShapeSchemaUpdater` drops `ValidityDurationID` |
| **WorkPermitItem** | `IsChanged`, `IsExtended` | Removed | `ApplicationItem.WorkPermitItemIsChanged` (change workflow); `IsCancelled` only on item | Dropped by `WorkPermitItemStatusColumnsCleanupUpdater` |
| **WorkPermit** | `IsApplicationNotRequired`, `IsCancelled` | Removed | Optional `Application` via gear toggle (same as `Invitation`) | Dropped by `WorkPermitApplicationNotRequiredColumnCleanupUpdater` |
| **Visa** | `HasInvitation`, `HistoricalImport` | Removed | Optional `InvitationItem` / `IssuingApplicationItem` via gear toggle | Dropped by `VisaVisibilityToggleColumnsCleanupUpdater` |
| **ApplicationItem** | `PurposeOfTravel` | Removed | `CurrentPositionHistory` (registration travel purpose / Forma 16) | `PurposeOfTravelID` dropped by `ApplicationItemPurposeOfTravelColumnsCleanupUpdater` |
| **TravelHistory** | `PurposeOfTravel` | Removed | `Notes` (`Travel Notes`) | `PurposeOfTravelID` dropped by `ApplicationItemPurposeOfTravelColumnsCleanupUpdater` |
| **TravelHistory** | `SourceApplicationItem` / `SourceApplicationItemID` | Removed | Manual officer CRUD on `Person.TravelHistories` | Cleared + dropped by `TravelHistorySourceApplicationItemCleanupUpdater`; sync service removed |
| **TravelHistory** | `SourceApplication_FullApplicationNumber`, `SourceApplication_ApplicationDate` | Removed | — (were NotMapped display of parent application) | Removed with sync decoupling |
| **AddressOfResidence** | `StartDate` | Removed | `ExpirationDate` only (`DaysRemaining` vs today) | `StartDate` dropped by `AddressOfResidenceStartDateColumnCleanupUpdater` |
| **ApplicationItem** | `Address_StartDate`, `Address_StartDateText` | Removed | `Address_ExpirationDate` / `Address_ExpirationDateText` | Not mapped aliases removed from BO |

---

## Removed schema (historical)

| Artifact | Removed by | Replacement |
|----------|------------|-------------|
| `Applications.LatestIsCancelled`, `Applications.LatestIsRejected` | `ApplicationLatestTerminalFlagsColumnsCleanupUpdater` | `ApplicationProgress` terminal states; `LatestPrimaryStateCode` / `CurrentState` |
| **OrganizationType** / `OrganizationTypes` (+ `Applications.OrganizationTypeID`, `ApplicationTypes.OrganizationTypeID`) | `OrganizationTypeSchemaCleanupUpdater` | Obsolete; unused |
| `Visas.HasBorderZonePermit` | `VisaBorderZoneLocationStringUpdater` | `Visa.BorderZoneLocation` string + **BorderZoneName** catalog |
| `Visa` ↔ `City` link table | `VisaBorderZoneLocationStringUpdater` | `Visa.BorderZoneLocation` |
| `WorkPermitItemPermittedCity` / link table | `WorkPermitItemPermittedLocationsStringUpdater` | `WorkPermitItem.WorkPermittedLocations` + **WorkPermittedLocation** catalog |
| `lookup.xlsm` as runtime seed | Lookup catalog sync on deploy | `LookupCatalogs/*.json` + ApplicationType C# seeds — see [`docs/LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md) |
| **Company**, **CompanyHead**, **Representative**, **LocalEmployee** (+ child image/document tables) | `OrganizationLegacySchemaCleanupUpdater` | `CompanyProfile`, `AuthorizedSignatory`, `AuthorizedRepresentative`, `SystemSettings` (app numbering) |
| `Applications.Company` / `CompanyHead` / `Representative` FK columns | `OrganizationLegacySchemaCleanupUpdater` | Singletons + `[NotMapped]` report aliases on `Application` |
| `People.Company`, `ProjectContracts.Company`, `Lodgings.Company` FK columns | `OrganizationLegacySchemaCleanupUpdater` | Single-tenant org; `CompanyProfile` for letterhead |
| `tenant/company.json` lookup catalog | Phase 5 manifest rename | `tenant/company-profile.json` → `CompanyProfile` |
| `Invitations.IsCancelled`, `Invitations.IsChanged` | `InvitationHeaderStatusColumnsCleanupUpdater` | `InvitationItems` status flags only |
| `Invitations.ValidityDurationID` | `InvitationLegacyShapeSchemaUpdater` | `VisaPeriod` + editable `ExpirationDate` |
| `WorkPermitItems.IsChanged`, `WorkPermitItems.IsExtended` | `WorkPermitItemStatusColumnsCleanupUpdater` | `ApplicationItem.WorkPermitItemIsChanged`; item `IsCancelled` only |
| `WorkPermits.IsApplicationNotRequired`, `WorkPermits.IsCancelled` | `WorkPermitApplicationNotRequiredColumnCleanupUpdater` | Optional `WorkPermits.Application` + gear on detail view |
| `Visas.HasInvitation`, `Visas.HistoricalImport` | `VisaVisibilityToggleColumnsCleanupUpdater` | Optional `IssuingApplicationItem` / `InvitationItem` + gear on detail view |
| `TravelHistories.SourceApplicationItemID` | `TravelHistorySourceApplicationItemCleanupUpdater` | Manual `TravelHistory` CRUD; registration apps no longer auto-link |
| **Ministry** lookup BO + `Ministries` table | `MinistrySchemaCleanupUpdater` | **`ApprovingMinistry`** tenant lookup + approval profile legs |
| `tenant/ministry.json` lookup catalog | Ministry BO removal | **`tenant/approving-ministry.json`** |
| `ProjectContract.MinistryReviewDepth` | Hidden / obsolete (2026-06) | **`ProjectContract.MinistryLegs`** + **`ProjectContractMinistryLeg`** (1…5 ministries per contract row) |
| `ProjectContractApprovalProfile`, `ProjectContractApprovalLeg` | Removed (2026-06 flatten) | One **`ProjectContract`** row per process; **`ProjectContractMinistryLeg`** |
| `Application.ContractApprovalProfile` | Removed (2026-06 flatten) | **`Application.ProjectContract`** only |
| `ProjectContracts.MinistryID` FK | `ProjectContractLegacyColumnsCleanupUpdater` | Approval profiles |

---

## Tooling and docs (non-BO)

| Item | Status | Replacement |
|------|--------|-------------|
| Report Dashboard **PROJECT** chip row + **person-type** tab strip (All / Employees / Family / Temporary) | Deprecated (hidden) | Category nav alone; filters locked to **All** / **All**. Re-enable: set `ReportDashboardCatalog.ShowProjectAndPersonTypeFilters = true`. See [`docs/REPORT_DASHBOARD.md`](REPORT_DASHBOARD.md). |
| `Visa2026.DataImporter --seed-lookups-only` | **Removed** | App startup `LookupCatalogSyncUpdater` |
| `Visa2026.DataImporter --sync-positions` / `--delete-missing` | **Removed** | Tenant/global JSON via `LookupCatalogSyncUpdater` |
| `LookupSeeder.cs` (OData POST from `lookup.xlsm`) | **Removed** | Module JSON + `--export-lookup-catalogs` dev tool |
| `LOOKUPS.md` as source of truth | Removed | JSON catalogs in git; human reference only |
| `BusinessTripWordController` | Removed | `WordReportsController` + `BusinessTripSanawyReportDef` — see [`docs/WORD_REPORT_GENERATION_IDEA.md`](WORD_REPORT_GENERATION_IDEA.md) |
| **ApplicationType** configuration seed (`ApplicationTypeConfigurationUpdater`, `ApplicationTypeConfigurationApplier`, `ApplicationTypeConfigurationCatalog.json`, `ApplicationTypeConfigurationSeed`) | Retained (deploy/import only) | **`ApplicationProfileSeedSync`** + **`ApplicationProfileFromApplicationTypeMapper`** — idempotent deploy sync only; not officer UX |
| **ApplicationType** capability / selection-code updaters (`ApplicationTypeCapabilityFlagsSchemaUpdater`, `ApplicationTypeSelectionCodeUpdater`) | Retained (deploy only) | Profile seed + `ApplicationProfile.SelectionCode` — do not use for new product behavior |
| **ApplicationTypeSelectionController** + **ApplicationTypeQuickCodeEditor** | Deprecated | **`ApplicationProfilePickerNewController`** + Blazor picker (type quick-code kept for dual-read / legacy detail edit) |
| **`ApplicationProgressRouteNavigation`** criteria on `ApplicationType.ApplicationProgressRoute` | Deprecated (stale) | Profile route via **`ApplicationProfileConfigurationResolver`** — migrate nav/ListView criteria off Type FK |
| **ApplicationItem**-scoped Resminamalar / document copies / linked-document services | Deprecated (slice 10) | Application- or Person-scoped after M2M (still active until slice 10b; do not add new item-only surfaces) |
| **`SqlViewsUpdater`** + SQL Server view scripts (`SqlViews/*.sql` without `.postgres`) | **Removed** (Phase B) | **`ReportDashboardPostgresViewsUpdater`**, **`ReportDashboardPostgresRosterSql`**, **`ReportDashboardPostgresViewsHealSql`**, `ApplicationWorkspacePostgresViewsSql` — one `.postgres.sql` (or RosterSql body) per view | Updater was never registered in `Module.GetModuleUpdaters`; its T-SQL still selected from the dropped `ApplicationItems` table. |
| **`View_*` extension/tracking views without a PostgreSQL creator** — `View_VisaExtensionTracking`, `View_WorkPermitExtensionTracking`, `View_WorkPermitExtensionStatus`, `View_VisaTransferStatus`, `View_VisaCancelExtStatus`, `View_VisaCancellationStatus`, `View_ForeignWorkerMaglumat` (+ `fn_CalculateDaysRemaining`, `fn_GetVisaRegistrationState`) | **Orphaned** (DDL removed with `SqlViewsUpdater`) | Port to PostgreSQL in `ReportDashboardPostgresViewsUpdater` **or** retire the BO / nav item. `View_VisaExtensionStatus` already has a Postgres body (`ReportDashboardPostgresRosterSql.ViewVisaExtensionStatusSql`) | These views never existed on PostgreSQL, so the mapped BOs and `MaglumatCsvController` (raw `SqlConnection`) were already non-functional; only the historical T-SQL is gone. |

---

## Deploy paths (infrastructure)

| Old path | Status | Replacement |
|----------|--------|-------------|
| `scripts/on-prem/` | **Moved** | `scripts/legacy/on-prem-windows/` |
| `docs/ON_PREM_WINDOWS_SERVER.md` (full runbook) | **Moved** | `docs/legacy/ON_PREM_WINDOWS_SERVER.md` (stub at old path redirects) |
| Skill `visa2026-windows-server-setup` | **Renamed** | `.cursor/skills/legacy-on-prem-windows-setup/` |
| Skill folder `on-prem-windows-deploy` | **Renamed** | `.cursor/skills/on-prem-deploy/` (shared maturity for all on-prem skills) |
| `docker-compose.restart.override.yml` in on-prem | **Removed** (duplicate) | `scripts/linux/docker-compose.restart.override.yml` |
| Native Windows IIS deploy | **Added** (pilot) | [ON_PREM_WINDOWS_IIS.md](./ON_PREM_WINDOWS_IIS.md), `scripts/windows-iis/` |

---

## Change log

| Date | Change |
|------|--------|
| 2026-08-13 | `ApplicationItem` Phase B hard-remove: BO/table/`IssuingApplicationItemID` gone. SQL Server view path (`SqlViewsUpdater` + plain `SqlViews/*.sql`) deleted; orphaned `View_*` extension/tracking views listed for port-or-retire. |
| 2026-08-07 | Application Profile cutover registry: type config seed/UX, group/template links, `ApplicationItem` / `IssuingApplicationItem` (slice 10 pending), progress **configuration** vs `ApplicationProgress` BO clarified. |
| 2026-07-31 | Registration→TravelHistory auto-sync removed; `SourceApplicationItemID` cleared/dropped; manual TravelHistory CRUD only. |
| 2026-05-26 | On-prem Windows/WSL scripts and docs moved under `scripts/legacy/` and `docs/legacy/`; Ubuntu path is canonical. |
| 2026-05-24 | Initial registry; ApplicationLocation JSON seed called out vs BorderZoneLocation UI catalog. |
| 2026-05-24 | Phase 5: legacy org BOs/tables and org FK columns removed; moved to **Removed schema**. |
