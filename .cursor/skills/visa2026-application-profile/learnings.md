# Application Profile — learnings (append-only)

Read **before** Application Profile work; **append** after verified fixes and slice completions. Promotion rules: [MATURITY.md](./MATURITY.md).

---

## Entries

### 2026-08-08 — Slice 10m: Report Dashboard ministry SQL

- **Delivered**: `ministry_roster_lines` CTE (M2M + legacy) via `{{MINISTRY_ROSTER_CTE}}` in 8 embedded `.postgres.sql` views; visa-extension completed `IssuedVisa` dual-read; ministry invitation legacy EF loader + role filters use M2M roster.
- **Next**: `SyncRulesUpdater`; `ApplicationItem` BO removal post-import.

### 2026-08-08 — Slice 10l: Report Dashboard visa extension SQL

- **Delivered**: Shared visa/work-permit extension roster CTEs; migrated `View_VisaExtensionStatus`, `vw_rd_visa_app_progress`, `vw_rd_work_permit_app_progress`, `vw_rd_visa_state`, `vw_rd_visa_extension_required` unfinished-extension filter; invitation in-process/rejected first person from M2M; `IssuedVisaID` dual-read in status view.
- **Next**: Ministry `vw_rd_application_via_ministry_*` embedded SQL files; `SyncRulesUpdater`; `ApplicationItem` removal.

### 2026-08-08 — Slice 10k: Report Dashboard child-link C# filters

- **Delivered**: `GetLinkedChildIdsInApplicationDateRange` on `ReportDashboardRosterQueryHelper`; Education, Address, Position, Medical Last-N loaders in `ReportDashboardQueryService` use M2M resolved links + legacy fallback; `vw_rd_application` first person from `ApplicationPeople`.
- **Fix**: Corrupted `ProgressStateCode` line in `vw_rd_application` SQL (`\`r\`n` literal) repaired during view migration.
- **Next**: Visa extension / work permit progress SQL views; `SyncRulesUpdater`; `ApplicationItem` BO removal.

### 2026-08-08 — Slice 10j: Report Dashboard roster SQL (phase B start)

- **Delivered**: `ReportDashboardPostgresRosterSql`; migrated `vw_rd_registration`, `vw_rd_passport`, `vw_rd_to_be_checked_in`, `vw_rd_to_be_checked_out` to M2M resolved links + legacy `ApplicationItems` fallback; `ReportDashboardRosterQueryHelper` for Travel, Registration on process, overview passport/address/travel counts.
- **Pattern**: Same dual-read as `ApplicationRosterHelper` — apps with `ApplicationPeople` rows use resolved links only; legacy `ApplicationItems` only when parent app has no M2M roster.
- **Next**: Remaining Report Dashboard views (education, position, medical, ministry extension, …), `SyncRulesUpdater`, then `ApplicationItem` BO removal post-import.

### 2026-08-08 — Slice 10i: Visa.IssuingApplication dual-read

- **Delivered**: `Visa.IssuingApplication` FK + deploy backfill; Path A matcher prefers `ApplicationPerson` M2M; validations/chronology use effective issuing application; legacy `IssuingApplicationItem` deprecated on detail when app FK set.
- **Verify**: Create visa for person on M2M-only application — Issuing Application field populated; F5/deploy backfills existing visas from item FK.
- **Prevent**: New visa linking code should set `IssuingApplication`, not only `IssuingApplicationItem`.
- **Next**: Report Dashboard SQL + sync rules + ApplicationItem BO removal.
- **Cross-skill**: visa2014 import post-pass still sets `IssuingApplicationItem` until import scripts updated.

### 2026-08-08 — Slice 10h: Runtime roster reads via ApplicationPeople

- **Delivered**: `ApplicationRosterHelper` centralizes M2M-first roster reads with legacy `ApplicationItem` fallback; Resminamalar merge hydrates from `ApplicationPerson`; header BO `AvailablePeople` + validation use M2M; Application cancel counts + ListView preload use roster helper.
- **Verify**: Workspace app with linked people only (no ApplicationItem rows) — Resminamalar rows populate; invitation/work-permit person pickers show roster.
- **Prevent**: New runtime reads should call `ApplicationRosterHelper`, not `Application.ApplicationItems` directly.
- **Next**: Phase B BO/schema drop after import + `vw_rd_*` migration.
- **Cross-skill**: visa2026-resminamalar (merge rows).

### 2026-08-08 — Slice 10g: ApplicationItem officer UI cutoff (phase A)

- **Delivered**: Removed ApplicationItem sub-nav under Applications; Person issued-documents tab uses `ApplicationPeople` (Applications linked); dossier Applications = M2M only; disabled `ApplicationItemDocumentCopiesController` on ListView.
- **Verify**: No Application items nav child; Person → Applications (linked); workspace document copies still work.
- **Prevent**: Officer paths must use Application workspace + `ApplicationPerson`; do not re-add ApplicationItem ListView actions without explicit legacy need.
- **Next**: Phase B — BO/schema removal (import, `vw_rd_*`, sync rules, Resminamalar merge) after VISA2014 cutover.
- **Cross-skill**: visa2026-document-copies (workspace path canonical).

### 2026-08-08 — Workspace: drop full profiles rail

- **Symptom**: Application Workspace left rail duplicated Configuration → Application Profile catalog and confused officers.
- **Fix**: Removed profile list/search rail from workspace; keep profile strip (title/chips) with Configure + New Application for the linked profile only.
- **Prevent**: Profile browsing/admin lives only under Configuration → Application Profile.
- **Cross-skill**: —

### 2026-08-08 — Catalog master-detail left rail

- **Symptom**: Opening a profile left the catalog ListView; officers lost the profile list on the left.
- **Fix**: Catalog shell is left rail (search + profiles + New) + inline overview on the right; Configure still opens wizard.
- **Prevent**: Do not navigate away to OverviewHost for the default select path from catalog.
- **Cross-skill**: —

### 2026-08-08 — Slice 8c: Custom catalog home (native List/Detail not officer UI)

- **Delivered**: `ApplicationProfileCatalogHost` + Blazor catalog (search, Active/locked badges, New / Configure / row open); Configuration nav via `ApplicationProfileCatalogModelUpdater` + `ApplicationProfileCatalogNavigationController`; `ApplicationProfile` `[NavigationItem(false)]`; ListView row → overview, New → create+wizard; overview **Configure profile** CTA.
- **Verify**: Configuration → Application Profile → catalog (no checkbox grid) → row → overview → Configure → wizard; New profile → wizard.
- **Prevent**: Non-persistent hosts need nav ModelUpdater + CustomShowNavigationItem (Report Dashboard pattern), not `[NavigationItem("Configuration")]` alone.
- **Next**: Slice 10 close-out (`ApplicationItem` hard-remove) or 13b after import.
- **Cross-skill**: —

### 2026-08-08 — Slice 10f: Profiles rail wired

- **Delivered**: `ApplicationWorkspaceProfileRailHelper` — profile row opens **Configure profile** wizard; **`+`** creates new Application with selected profile (same pipeline as picker; inherits `CreationProgressRoute` from current workspace Application) and opens new workspace.
- **Verify**: Workspace left rail → click profile name → wizard; click **`+`** on another profile → new Application workspace opens.
- **Next**: Slice 10 close-out (`ApplicationItem` hard-remove) or workspace Resminamalar / multi-select roster.

### 2026-08-07 — Slice 10e: Document copies on Application workspace (roster line)

- **Delivered**: `ApplicationPersonLinkedDocumentsResolver` + `ApplicationPersonPdfPackageLineHydrator`; `DocumentCopiesLineScope` on `DocumentCopiesSlotRequest`; workspace Person tab **Document copies** (selected `ApplicationPerson` row); `ApplicationPersonPdfBatchEnqueueService` (`ItemKeyType` = `ApplicationPerson`); worker hydrates roster lines for packer/PDF; resolver visibility uses `ApplicationProfileConfigurationResolver` (profile-first).
- **Verify**: Workspace → Person tab → select roster row → **Document copies** → slot catalog, scan preview, application form download, package → PDF toast; legacy `ApplicationItem` ListView **Document copies** still works.
- **Deferred**: Multi-select roster lines in workspace; previous passport/WP/invitation slots until resolver stores them on `ApplicationPerson`.
- **Next**: Slice 10 close-out (`ApplicationItem` hard-remove).

### 2026-08-07 — Slice 10d: Application ListView opens workspace

- **Delivered**: `ApplicationListViewWorkspaceNavigationController` — row activate on Application ListViews opens workspace (not legacy `Application_DetailView`).
- **Verify**: Applications list → open row → workspace; picker create still opens workspace; toolbar **Open workspace** unchanged.
- **Next**: `ApplicationItem` hard-remove (slice 10 close-out) or child-tab SQL views.

### 2026-08-07 — Slice 10c: Workspace in-tab actions + person SQL view

- **Delivered**: `ApplicationWorkspacePersonUiActions` bridge; Person tab **Link existing…** / **Unlink** / **Open detail** wired to XAF popup actions; row selection on Person tab; `vw_application_workspace_person` + `ApplicationWorkspacePostgresViewsSql` startup heal; picker/create opens workspace (prior session).
- **Verify**: Application workspace → Person tab → **Link existing…** → pick person → row appears; select row → **Open detail**; **Unlink** removes roster row.
- **Deferred**: SQL views for passport/visa/etc. tabs; `ApplicationItem` hard-remove (slice 10 close-out).

### 2026-08-07 — Slice 13a: Profile-first runtime + cutover prep

- **Delivered**: `ApplicationProfileConfigurationResolver` capability methods (`CanIssueVisa`, `CanIssueInvitation`, `CanIssueWorkPermit`, `CanBeIssuingApplicationForVisa`); `ApplicationTypeCapabilities` Application overloads; profile-aware queries in `Visa`, `VisaIssuingLinkPathAMatcher`, `Invitation`, `WorkPermit`, `ApplicationItemVisaDefaults`; `ApplicationProgressRouteNavigation` criteria use `CreationProgressRoute` + profile + type fallback; `Application` validation requires profile **or** type (Type hidden on detail when profile set).
- **Dual-write**: Picker still syncs matching `ApplicationType` on create for Report Dashboard / sync rules / PDF until slice **13b**.
- **Deferred (13b)**: Drop `Applications.ApplicationTypeID`; migrate Report Dashboard SQL, `SyncRulesUpdater`, `PdfMappingHelper`, import mappers.
- **Verify**: Create Application via profile picker (no manual Type) → appears in correct route nav list; link Visa/Invitation/WorkPermit to issuing Application; Type field hidden on detail when profile present.
- **Cross-skill**: visa2014-to-visa2026-import (import must set `ApplicationProfile` before 13b)

### 2026-08-07 — Slice 8b: Wizard prototype parity

- **Delivered**: Step 1 applicability scope cards (Always vs Scoped + criteria); Step 2 property table with Require + Has default + lookup defaults + signatory pickers; Step 3 ministry/migration state Include/SLA tables (`ApplicationProfileProgressStateSetting` child BO + catalog seeder); Step 4 template add/edit/remove (name, kind, sort) in wizard.
- **Schema**: `ApplicationProfileProgressStateSettings` table in `ApplicationProfileSchemaSql` + EF mapping; permissions on child type.
- **Not wired yet**: `ProgressStateSettings` → `ApplicationProgress` route/SLA engine (configuration stored; read in later slice with **visa2026-application-progress**).
- **Verify**: Configure profile → walk all 5 steps → Save profile; existing profiles get default state rows on first wizard open.
- **Cross-skill**: visa2026-application-progress (state checklist consumption)

### 2026-08-07 — Slice 9: Profile picker at Application create

- **Delivered**: `ApplicationProfilePickerHost`, Blazor picker component + CSS, `ApplicationProfilePickerNewController` (intercepts **New** on Application ListViews), `ApplicationProfilePickerQueryService` (active + route filter + applicability criteria + MRU by last `ApplicationDate`), `ApplicationProfilePickerApplyHelper` (profile FK + dual-read `ApplicationType` sync + defaults).
- **Flow**: List **New** → choose profile → **Use profile (live link)** → new Application DetailView with read-only profile and seeded per-Application defaults.
- **Route lists**: Via-ministries / direct-migration ListViews filter profiles by `ProgressRoute`; general Applications list shows all active profiles.
- **Locked profiles**: Still selectable for new Applications; picker shows **Config locked** badge (configuration edits blocked on profile, not on new app).
- **Verify**: Applications → **New** → pick profile → DetailView shows **Application Profile** + defaults (Visa Type, etc. when configured on profile).
- **Officer manual**: `user-manual/docs/en/guides/applications/application-profiles.md`, `administration/configuration/application-profiles.md` (preview prose/mermaid; no E2E screenshots yet).
- **Next**: Slice 10b — real M2M workspace data; Slice 11 — Person/Dossier start application.
- **Cross-skill**: visa2026-application-progress (route filter) | visa2026-user-manual | visa2026-person-dossier (slice 11 entry)

### 2026-08-07 — Slice 8: Configuration wizard UX

- **Delivered**: `ApplicationProfileWizardHost`, 5-step Blazor `ApplicationProfileWizardComponent` + step partials, `application-profile-wizard.css`, **Configure profile** action on Application Profiles ListView, `IApplicationProfileWizardSession` / pending-open gate (Blazor DI in `Startup.cs`).
- **Steps**: Identity (name/code/audience/related-to) · Results & fields (produce/cancel flags, Require*) · Process & SLA (route, SLA days, embedded approval legs add/remove) · Templates & person (nested templates hint + person toggles) · Review & save.
- **Lock**: Wizard honors `ApplicationProfileLockHelper` — read-only UI + save blocked when profile config locked (state A).
- **Deferred**: Ministry/migration state checklist tables from prototype; template file upload in wizard (officers use standard nested templates ListView).
- **Verify**: Configuration → Application Profiles → **Configure profile** → edit → **Save profile**; locked row → read-only banner.
- **Next**: Slice 9 — profile picker at Application create.
- **Cross-skill**: visa2026-application-progress (route/legs) | visa2026-resminamalar (nested templates, slice 12)

### 2026-08-07 — Slice 7: ApplicationProfile config lock (state A)

- **Delivered**: `[Appearance]` read-only when `IsConfigLocked`; `ApplicationProfileDetailViewController` (`View.AllowEdit`); `ApplicationProfileConfigLockObjectSpaceHooks` (save guard on profile + nested legs/templates); `ApplicationProfileCloneController` (CloneObject suffix for locked-profile escape hatch).
- **Lock helper**: `IsPrimaryStateAtOrPastLockStateA` now treats `IS_BEING_PREPARED` / `OFFICE_PREPARATION` / `DRAFT` as unlocked; `IsProfileConfigLocked` queries linked Applications via ObjectSpace.
- **Officer path**: Configuration → Application Profiles → locked row is read-only; use **Clone** to duplicate and edit configuration.
- **Next**: Slice 8 — configuration wizard Blazor UX.
- **Cross-skill**: visa2026-application-progress

### 2026-08-07 — Slice 6: Appearance / progress reads ApplicationProfile first

- **Delivered**: `ApplicationProfileConfigurationResolver` (profile-first, `ApplicationType` fallback); `Application.ConfigurationVisibility` (`Cfg*` properties for XAF `[Appearance]`); updated `Application` + `ApplicationItem` criteria; `ApplicationProgressRouteHelper`, `ApplicationProgressProfileResolver` (embedded profile legs, `RequireProject` / approval-leg gates), `ApplicationMigrationSlaHelper`, migration SLA validation in `ApplicationProgressTransitionHelper`.
- **Pattern**: XAF criteria cannot call static helpers — expose `[NotMapped] CfgShow*` on `Application`; nested items use `Application.CfgShow*`.
- **Tests**: `ApplicationProfileConfigurationResolverTests` (8 facts) + existing `ApplicationProgressProfileResolverTests` still pass.
- **Not migrated**: Report Dashboard SQL, sync rules, PDF mapping, import tools — still read `ApplicationType` where appropriate until slice 13.
- **Next**: Slice 7/8 — config lock UX + wizard.
- **Cross-skill**: visa2026-application-progress | visa2026-bo-state-colors

### 2026-08-07 — Slice 5: seed ApplicationProfile from ApplicationType

- **Delivered**: `ApplicationProfileFromApplicationTypeMapper`, `ApplicationProfileSeedSync`, `ApplicationProfileSeedUpdater` (after SLA type links), `ApplicationProfileSeedGate` on host start.
- **Behavior**: One profile per `ApplicationType` (key = `Code` or slug from `Name`); idempotent re-sync updates profile scalars from Type; backfills `Application.ApplicationProfile` where `ApplicationType` is set.
- **Maps**: `ProgressRoute`, action family (registration/cancel/business trip/issuance), produce/cancel flags, audience (`Category`), Require* per-app and person toggles, ministry/migration SLA days.
- **Verify**: Restart app → Configuration → Application Profiles; Applications list **Application Profile** column; log `ApplicationProfileSeedSync: profiles created=…`.
- **Next**: Slice 6 — central resolver; switch `[Appearance]` / progress reads from `Show*` to profile.
- **Cross-skill**: visa2026-lookup-data (Type JSON seed) | visa2026-lifecycle-docker (schema heal + startup gates)

### 2026-08-07 — Postgres 42703 Applications.ApplicationProfileID missing

- **Symptom**: `column a.ApplicationProfileID does not exist` on Application ListView after ApplicationProfile BO shipped.
- **Root cause**: ModuleInfo current → XAF skipped EF schema sync; no startup heal for new profile tables/FK.
- **Fix**: `ApplicationProfileSchemaSql` + `ApplicationProfileSchemaUpdater` + `Startup` `ApplyIfMissing` (ApplicationProfiles tables + `Applications.ApplicationProfileID`).
- **Prevent**: New ApplicationProfile-related columns need idempotent SQL heal (not ModuleUpdater alone).
- **Cross-skill**: visa2026-lifecycle-docker | —

### 2026-08-07 — Slice 12: Resminamalar reads profile nested templates

- **Delivered**: `ApplicationProfileNestedTemplateCatalogHelper`; catalog prefers profile `NestedTemplates`; `profile:` entry keys in `ApplicationWordReportEntryGenerator`; name-match to `UserReportTemplate` for merge.
- **Dual-read**: empty nested list → legacy `UserReportVisibilityService` catalog unchanged.
- **Deferred**: profile `TemplateFile` bytes override at merge (uses User Report Template file); profile default FKs at empty merge fields (plan §2 decision 12).
- **Cross-skill**: visa2026-resminamalar

### 2026-08-07 — Slice 11: Person / Dossier Start application

- **Delivered**: **Start application…** on Person DetailView + Person Dossier; 2-step profile picker (MRU for seed person, usage badges, people multi-select); `ApplicationStartFromPersonHelper`; dossier Applications section from M2M.
- **Rules shipped**: via-ministry blocks without ProjectContract; registration suggests family; duplicate-open warn + acknowledge; incomplete data warned not blocked.
- **Cross-skill**: visa2026-person-dossier (Applications section columns)

### 2026-08-07 — Slice 10b: Application workspace live M2M

- **Delivered**: `ApplicationPerson` roster, auto-resolved child links, `ApplicationWorkspaceQueryService`, toolbar **Link person** / **Unlink person**, schema heal `ApplicationWorkspaceSchemaSql`.
- **Gotcha**: Service namespace must not be `Services.ApplicationPerson` — shadows BO type from sibling `Services.ApplicationWorkspace` (renamed to `ApplicationPersonRoster`).
- **Gotcha**: XAF0009 on `ApplicationPersonResolvedLink` — `LinkKind` and `LinkedObjectId` must be nullable.
- **Deferred**: SQL `vw_application_workspace_*` views; in-component toolbar buttons still decorative (use XAF actions).
- **Cross-skill**: —

### 2026-08-07 — Slice 10a: Application workspace mock UI shipped

- **Delivered**: `ApplicationWorkspaceHost`, mock `IApplicationWorkspaceQueryService`, Blazor `ApplicationWorkspaceComponent`, **Open workspace** on Application ListView/DetailView.
- **Pattern**: Same as Person dossier (non-persistent host + PropertyEditor) + Report Dashboard (mock query service).
- **Next**: Slice 10b — Person M2M domain, `ApplicationWorkspaceQueryService`, SQL `vw_app_*` tab grids; then hard-remove ApplicationItem.
- **Cross-skill**: —

### 2026-08-07 — Skill created; next slice is Type → Profile seed

- **Context**: Plan + prototypes done; `ApplicationProfile` BO and optional `Application.ApplicationProfile` FK shipped; dual-read with deprecated `ApplicationType` continues.
- **Decision**: **Slice 5** (seed profiles from ApplicationType + backfill FK) is the recommended next implementation step before wizard UX or M2M DetailView.
- **Prevent**: Do not build wizard/M2M on empty profile catalog in prod-like DBs — seed first.
- **Cross-skill**: visa2026-lookup-data (Type configuration JSON) | —
