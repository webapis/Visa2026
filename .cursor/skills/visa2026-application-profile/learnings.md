# Application Profile — learnings (append-only)

Read **before** Application Profile work; **append** after verified fixes and slice completions. Promotion rules: [MATURITY.md](./MATURITY.md).

---

## Entries

### 2026-08-10 — B5b Case workspace PNG parity (Blazor)

- **Delivered**: Full case workspace lift from HTML prototype — `ApplicationWorkspaceCaseView` + `ApplicationWorkspaceCaseBuilder`; tab UIs for overview (summary tiles + stepper + linked records), people matrix + rail, progress vertical timeline + advance action, inline `ApplicationItemDocumentCopiesComponent` / `ApplicationReportPackageComponent`, SLA dashboard.
- **Files**: `OfficerShellCaseWorkspaceComponent.razor`, `OfficerShellCaseDocumentsTab.razor`, `OfficerShellCaseResminamalarTab.razor`, `ApplicationWorkspaceCaseModels.cs`, `ApplicationWorkspaceCaseBuilder.cs`.
- **Verify**: F5 → Application Profiles → In process → open row → all 6 tabs; documents/resminamalar render in-tab (wide layout, no preview-slot redirect).
- **Deferred**: Ministry letter upload + progress notes persistence (read-only shell); hide XAF tab bar.

### 2026-08-10 — B5 Case workspace 6-tab shell

- **Delivered**: `OfficerShellCaseWorkspaceComponent` — PNG `cw-*` layout with tabs (overview, people, progress, documents, resminamalar, SLA); live `ApplicationWorkspaceSnapshot` + new `CaseChrome` header fields; person link/unlink/detail + preview-slot document copies and Resminamalar.
- **Module**: `ApplicationWorkspaceCaseChrome`, `ApplicationWorkspaceResminamalarOpenHelper`.
- **Next**: parity sign-off `parity/CHECKLIST.md`; hide XAF tab bar (optional).
- **Cross-skill**: preview slot — **visa2026-preview-slot**; document copies — **visa2026-document-copies**.

### 2026-08-10 — B4 Profile templates catalog (list/grid + detail)

- **Delivered**: `OfficerShellTemplateCatalogComponent` — PNG-parity templates catalog (family chips, list/grid, pagination, status pills, staged/in-process usage counts); drill-in rail + `ApplicationProfileOverviewComponent`; `ApplicationProfileCatalogRow` extended with usage + family key.
- **Next**: PNG 6-tab case workspace shell; parity sign-off `parity/CHECKLIST.md`.
- **Cross-skill**: —

### 2026-08-10 — B2 Start process domain merge + B3 immersive chrome

- **Delivered**: `OfficerShellStartProcessService` — validates staged+ready rows, merges people into primary `Application`, deletes secondary staged shells, allocates `YYYY-NNNN` process number (`OfficerShellProcessNumberAllocator`), appends first progress step (`1_REVIEW_STARTED` or `PROCESS_STARTED` by route), syncs `Application.ProcessNumber` + latest progress.
- **Blazor**: `StartProcessAsync` commits via `ObjectSpace`, reloads queues, opens case workspace.
- **B3**: `#visa-app-shell:has(.officer-shell-host)` hides XAF `.sidebar` and strips DetailView padding (`officer-shell-host.css`).
- **Merge**: multi-select links roster via `ApplicationPersonService`; copies `ProjectContract` / profile when primary empty.
- **Next**: dedicated templates list/grid screen; PNG 6-tab case workspace; parity sign-off in `parity/CHECKLIST.md`.
- **Cross-skill**: progress transition rules — **visa2026-application-progress**.

### 2026-08-10 — B0 Blazor officer shell lift

- **Delivered**: `OfficerShellHost` + `OfficerShellComponent.razor` — PNG sidebar (staged / in-process / templates), live badge counts, list/grid queues from `IOfficerShellStagedQueryService` / `IOfficerShellInProcessQueryService`, embedded `ApplicationProfileCatalogComponent` + `ApplicationWorkspaceComponent`.
- **Nav**: Application → **Application Profiles** (`OfficerShellModelUpdater`).
- **CSS**: copied `wwwroot/officer-shell/styles/*` → `wwwroot/css/officer-shell/` + `officer-shell-host.css`.
- **Staged heuristic**: `ProcessNumber` empty + `LatestPrimaryStateCode` in `OFFICE_PREPARATION` / `DRAFT` / null.
- **Start process (v1)**: opens case workspace for first selected staged row — full merge/number assignment deferred to **B2**.
- **HTML prototype**: remains at `/officer-shell/` for parity QA; production path is XAF nav.
- **Next**: **B1** grouped staged + pagination + workspace tab chrome in shell; **B2** domain merge on Start process.
- **Cross-skill**: —

### 2026-08-10 — B1 Blazor shell PNG polish

- **Delivered**: Family filter chips + legend, pagination (10/25/50), grouped staged accordion (`OfficerShellStagedGroupedView`), rich grid cards with color stripe, toolbar search, SLA chips on in-process; `OfficerShellTemplateFamily` maps profile code/action family → reg/inv/ext/wp.
- **Components**: `OfficerShellPaginationBar`, `OfficerShellFamilyChips`, `OfficerShellStagedGroupedView`.
- **Still 🟡 vs PNG/HTML**: full XAF chrome, dedicated templates list/grid, case workspace 6-tab PNG layout, template catalog pagination.
- **Next**: **B2** Start process merge; optional immersive XAF chrome hide.
- **Cross-skill**: —

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
- **Cross-skill**: —

### 2026-08-10 — P10 case workspace tabs (PNG parity)

- **Delivered**: `case-tabs-ui.js` + `case-tabs.css` — People & links (table + per-person record grid + summary rail), Progress (vertical timeline + ministry detail + rail), Resminamalar (grouped catalog + preview + ZIP), SLA & deadlines (metrics, timeline, deadlines table, alerts).
- **Routes**: `#/case/p1/people`, `/progress`, `/resminamalar`, `/sla`.
- **Cross-skill**: visa2026-resminamalar, visa2026-person-detail-tabs (production reference)

### 2026-08-10 — P9 nav badge counts (PNG parity)

- **Delivered**: `nav-ui.js` — live sidebar badges (orange **18** staged, blue **24** in-process); templates subcopy “Configuration · Visa office admin”; `os-nav-badge` styling in `shell.css`.
- **Mock seed**: `seedInProcessDemoCases()` pads in-process to 24 (ext 8 · inv 6 · reg 5 · wp 5); staged remains 18.
- **Note**: counts update live after Start process (by design).
- **Cross-skill**: —

### 2026-08-10 — P8 staged grouped workspace (PNG parity)

- **Delivered**: `staged-workspace-ui.js` + `staged-workspace.css` — accordion groups by template family (reg/inv/ext/wp), avatars, row meta badges, readiness dots, collapsible sections, bottom selection bar; **Grouped** toggle + `#/staged?group=template`.
- **Cross-skill**: —

### 2026-08-10 — P6 pagination (PNG parity)

- **Delivered**: `pagination-ui.js` + `pagination.css` — shared bar on staged, in-process, and templates (list + grid): “Showing X–Y of Z”, rows-per-page select (10/25/50), Bootstrap prev/next + numbered pages; filter/search resets to page 1.
- **Store**: `pagination.{staged,inProcess,templates}` in mock-data.
- **Cross-skill**: —

### 2026-08-10 — P4 template catalog + overview (PNG parity)

- **Delivered**: `template-catalog-ui.js` + `template-catalog.css` — list/grid catalog with status pills (Active/Locked/Draft), rich grid cards (stripe, icon, stats, Configure), toolbar search + filter/sort dropdowns, pagination stub; overview with left rail cards, 4 numbered summary columns, usage stats bar, lock hint footer.
- **Mock seed**: 12 templates (chip counts All 12 / Issuance 4 / Registration 3 / Cancellation 2 / Business trip 3).
- **Routes**: `#/templates` (list/grid toggle), `#/templates/t1` (overview).
- **Cross-skill**: —

### 2026-08-10 — P3 document copies tab (PNG parity)

- **Delivered**: `document-copies-ui.js` — readiness summary + progress bar, per-person accordion (6 slots), Ready/Missing badges, preview pane with metadata; `#/case/:id/documents`.
- **Cross-skill**: visa2026-document-copies (production dialog reference)

### 2026-08-10 — P2 case workspace overview (PNG parity)

- **Delivered**: `case-workspace-ui.js` + `case-workspace.css` — header with SLA badge, person avatars, summary icon grid, horizontal progress stepper, linked-record tiles, readiness + activity rail.
- **Route**: `#/case/p1/overview` (any in-process case id).
- **Cross-skill**: —

- **Delivered**: Template wizard rebuilt with **Bootstrap 5.3** + **Bootstrap Icons** (CDN); `js/wizard-ui.js` + `styles/wizard.css` for PNG stepper, badges, green section headers, all 5 steps.
- **Wizard mode**: `os-app--wizard` collapses sidebar to icon rail (matches wizard mockup).
- **Cross-skill**: —

### 2026-08-10 — HTML officer shell (H0–H6)

- **Delivered**: Interactive prototype at `Visa2026.Blazor.Server/wwwroot/officer-shell/` — hash router, mock store, staged merge → in-process workspace, templates + 5-step wizard, PNG gallery; 22 PNGs copied to `assets/png/`.
- **Plan**: [`docs/APPLICATION_PROFILE_HTML_PROTOTYPE_PLAN.md`](../../../docs/APPLICATION_PROFILE_HTML_PROTOTYPE_PLAN.md) — slices H0–H6 **Done**; H7 (Person staging) deferred.
- **Parity**: `parity/CHECKLIST.md` created — visual sign-off not yet run at 1440×900.
- **Wizard routing**: use `#/templates/wizard/{0-4}` (query string in hash unreliable).
- **Next**: Officer walkthrough + parity checkboxes; then Blazor lift (`OfficerShellLayout.razor`) when product locks template → staged → in-process pivot.
- **Cross-skill**: —
