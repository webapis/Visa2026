# Report Dashboard — Learnings (append-only)

Date format: `YYYY-MM-DD`

---

## 2026-08-19 — RegistrationKind InfoChange (enum 3)

**Ask:** Third Registration option **Info change** (passport/visa/address info-change profiles).

**Now:** `RegistrationKind` 3 = InfoChange. Roster SQL has `RegistrationInfoChangeProfilePredicate`. Dashboard views **unchanged**.

**Next:** When adding info-change dashboard reports, filter on this predicate.

**Files:** `ReportDashboardPostgresRosterSql.cs`

## 2026-08-19 — Profile RegistrationKind for future check-in/out views

**Ask:** Application Profile wizard Registration = Check in or Check out. Use later for Report Dashboard check-in / check-out queries.

**Now:** `ApplicationProfiles.RegistrationKind` (1 = CheckIn, 2 = CheckOut). Roster SQL has `RegistrationCheckInProfilePredicate` / `RegistrationCheckOutProfilePredicate`. Existing `vw_rd_to_be_checked_in` / `_out` and `CheckoutProfilePredicate` (`Code = 'check_out'`) **unchanged**.

**Next:** When rewiring those views, filter instances by `apf.RegistrationKind` instead of type name / code lists.

**Files:** `ApplicationProfile.cs`, `ReportDashboardPostgresRosterSql.cs`

## 2026-08-12 — F5 42703 CreationProgressRoute (NotMapped)

**Cause:** Dashboard SQL coalesced `a."CreationProgressRoute"` but the property is `[NotMapped]` (no DB column).

**Fix:** Use `apf."ProgressRoute"` only in via-ministry / direct-migration `.postgres.sql`.

## 2026-08-12 — Remaining heal-path vw_rd_* off ApplicationType

**Next after via-ministry:** invitation / WP progress / visa extension required / registration / to-be-checked / `vw_rd_application` / RosterSql registration CTEs now join `ApplicationProfiles`. Checkout narrowed to `Code = 'check_out'` (not all Registration family).

**Files:** remaining `SqlViews/*.postgres.sql` that joined Types; `ReportDashboardPostgresRosterSql.cs`; `ReportDashboardPostgresViewsUpdater.cs` invitation/application inline SQL.

## 2026-08-12 — Via-ministry dashboard SQL: ApplicationProfile not ApplicationType

**Ask / crash:** Heal `42703 column at.ApplicationProfileInstanceProgressRoute does not exist`. User: do not drive dashboard off deprecated `ApplicationTypes`.

**Fix:** Standalone via-ministry / direct-migration `.postgres.sql` + `vw_rd_visa_app_progress` + roster visa/WP CTEs join `ApplicationProfiles`. Filters: `ProgressRoute` / `CreationProgressRoute`, `ProduceInvitation`, `ProduceVisa`+`RequirePersonVisa`. Label alias `ApplicationTypeLabel` still filled from `apf.Name` for EF column name.

**Files:** `SqlViews/vw_rd_application_via_ministry_*.postgres.sql`, `vw_rd_application_direct_migration_*.postgres.sql`, `vw_rd_visa_app_progress.postgres.sql`, `ReportDashboardPostgresRosterSql.cs`

## 2026-08-12 — F5 42601 {{MINISTRY_ROSTER_CTE}} in heal

**Symptom:** Startup `ReportDashboardPostgresViewsHealSql.ExecuteEmbeddedSql` — `syntax error at or near "{"` (POSITION 74) while recreating via-ministry standalone views.

**Cause:** After ApplicationOid recreate triggered `NeedsViaMinistryStandaloneHeal`, heal loaded embedded `.postgres.sql` without expanding `{{MINISTRY_ROSTER_CTE}}` (ModuleUpdater uses `ReportDashboardSqlViewResource.Load`).

**Fix:** Heal `ExecuteEmbeddedSql` calls `ReportDashboardSqlViewResource.Load`.

**Files:** `ReportDashboardPostgresViewsHealSql.cs`

## 2026-08-12 — F5 42703 ApplicationProfileInstanceOid on On Extension heal

**Symptom:** Startup `ReportDashboardPostgresViewsHealSql.NeedsVisaAppProgressPrimaryCodeHeal` — `column o.ApplicationProfileInstanceOid does not exist` (local PG).

**Cause:** Mechanical Application → ApplicationProfileInstance rename updated embedded `vw_rd_*` SQL and the heal probe, but ModuleInfo was already current so live views still aliased `ApplicationOid`. Wrapper heal skipped recreate because PassportNumber already existed.

**Fix:** If the view has `ApplicationOid`, recreate from embedded SQL (DROP + CREATE). Probe `LatestPrimaryStateCode` only after `ApplicationProfileInstanceOid` exists. Same for via-ministry standalone and work-permit views.

**Files:** `ReportDashboardPostgresViewsHealSql.cs`

## 2026-07-17 — Application Status: default Bar Chart + equal label/bar width

**Change:** `DefaultChartViewFor(Application, _)` → `bar` (was pie). Bar row grid `140px 1fr 34px` → `1fr 1fr 48px` so label and bar track share width equally; label text `nowrap` + ellipsis with `title` for overflow.

**Why:** Application Status combined labels (state · location · depth · profile) were wrapping in a 140px column and hard to scan.

**Files:** `ReportDashboardPropertyEditor.cs`, `report-dashboard.css`, `ReportDashboardComponent.razor`.

---





## 2026-07-29 — Application subreport process labels = StatusListLabel

**Ask:** Process names in Application (via ministry) subreports must match `ApplicationProgress` (Progress history Status), not legacy "1st Review Started / Process Started".

**Source of truth:** `LookupCatalogStrings` `application-state` (At office, Sent for agreement, Cleared agreement, Processing, Issued, Rejected, Cancelled, Not received from ministry) + `ApplicationProgressListLabelHelper.FormatStatusLabel` → `"State - Ministry"` when a ministry leg applies.

**Change:** Updated via-ministry mock Status segments accordingly. Real loaders later should use latest `StatusListLabel` (not bare `Application.CurrentState`, which omits ministry).

**Files:** `ReportDashboardMockQueryService.cs`

## 2026-07-29 — Overview grid responsive columns

**Ask:** Cap / scale Overview card columns by viewport (CSS responsive best practice).

**Cause:** `repeat(auto-fit, minmax(260px, 1fr))` packed many skinny cards on wide screens; chips cramped.

**Fix:** `container-type` on `.rd-report-area` + `@container rd-report` breakpoints (2→3→4→5 cols). Viewport `@media` fallbacks. Cap at 5. `minmax(0, 1fr)` so chips stay inside cards.

**Files:** `report-dashboard.css`

## 2026-07-29 — Overview card: long Application (via ministry) sub-chips spill

**Symptom:** Sub-report chips overflow neighboring Overview cards (`white-space: nowrap` + `overflow: visible` on `.rd-overview-card`).

**Fix:** Card `overflow: hidden`; chips `white-space: normal` + `overflow-wrap` + `max-width: 100%`; `title` on chip. Shorten via-ministry Labels (drop redundant "Application for…") to match Invitation/Visa chip length.

**Files:** `report-dashboard.css`, `ReportDashboardComponent.razor`, `ReportDashboardCatalog.cs`, locale/mock labels.

## 2026-07-29 — Application (via ministry): 10 mock On Process / Completed sub-reports

**Ask:** Replace Application Status under Application (via ministry) with Invitation / Visa Extension / Other × On Process / Completed; (P)/(V) like Invitation Process (Other = P only). Mock first. Remove Include completed/cancelled toggles.

**Catalog keys:** `invitation-on-process`, `invitation-on-process-by-period-category-type`, `visa-extension-on-process`, `visa-extension-on-process-by-period-category-type`, `other-on-process`, `invitation-completed`, `invitation-completed-by-period-category-type`, `visa-extension-completed`, `visa-extension-completed-by-period-category-type`, `other-completed`.

**Rules (for later real wiring):** CanIssueInvitation / visa-ext types / other; On Process = non-terminal; Completed = terminal; grain = Application header; chart (P)=Project·State, (V)=Period·Category·Type·State.

**Hybrid:** Via ministry → mock; Direct migration Application Status stays real. `SupportsIncludeCompleted/CancelledApplicationProcesses` → always false.

**Files:** ReportDashboardCatalog, MockQueryService, HybridQueryService, UiStrings.messages.json, VisaUiMessageCatalog.g.cs, reference.md

## 2026-07-29 — Split Application category into via-ministry / direct-migration

**Ask:** Replace single Report Dashboard **Application** category with two top-level categories (same order): Application (via ministry), Application (direct migration). Same Application Status UX + Include completed/cancelled; two Overview cards.

**Nav split (answer):** XAF Applications nav is **not** filtered by MinistryReviewDepth string. `CustomNavigationUpdater` / `ApplicationProgressRouteNavigation` clone ListViews with criteria on `ApplicationType.ApplicationProgressRoute`:
- `ViaMinistries` → Applications (via ministry)
- `DirectToMigrationService` → Applications (direct migration)
Dashboard uses the **same** route enum (direct migration typically shows as “no ministry review” in combined Status).

**Change:**
- Enum: `Application` → `ApplicationViaMinistry` + `ApplicationDirectMigration`
- Catalog helpers `IsApplicationCategory` / `ApplicationProgressRouteFor`; Open ListView → `Application_ListView_ViaMinistries` / `_DirectMigration` + route criteria
- Loaders/Hybrid/Mock filter by route; localization keys + Overview cards follow Categories[]

**Files:** ReportDashboardModels/Catalog/Query/Hybrid/Mock, ReportDashboardLocalization, VisaUiMessageCatalog.g.cs, UiStrings.messages.json, PropertyEditor, docs/REPORT_DASHBOARD.md

## 2026-07-14 — Initial dashboard implementation

**What was built:**
- Full prototype with mock data for all 7 categories (Visa, Invitation, Registration, WorkPermit, Travel, BorderZone, Passport)
- Overview mode: card grid with conic-gradient donut + horizontal mini-bars per category
- Category detail mode: bar/pie/list chart, sub-report tabs, preview table
- Date range picker (6m–3y), project chips, person type tabs
- Full-page CSS overriding XAF chrome (no border-left, no border-bottom, 0 padding)

**Known patterns:**
- `Status` field in `ReportDashboardPreviewRow` drives ALL chart grouping — make sure mock and real data use human-readable labels, not codes
- `ReportDashboardPropertyEditor` must use a separate persistent `EF ObjectSpace` for DB queries — the DetailView`s `IObjectSpace` is a `NonPersistentObjectSpace` and throws when queried for EF entities
- UTF-8 (no BOM) is required for all `.cs` and `.razor` files — the `StrReplace` tool may produce UTF-16; use `[System.IO.File]::WriteAllText` with `New-Object System.Text.UTF8Encoding $false` when rewriting files
- `ComponentModelBase` is the correct base for the Blazor component model in XAF 25.2; `Disposable` is not the right base

**Pending:**
- All categories still on mock data — no SQL views created yet
- Real `ReportDashboardQueryService` methods exist but only the snapshot count logic is complete; `Load*` methods return mock-equivalent structure from EF objects
---

## 2026-07-15 — Passport By Validity via SQL view (hybrid sub-report)

**What was done:**
- Added `vw_rd_passport` (SqlViews + SqlViewsUpdater) with ValidityLabel buckets aligned to mock: Valid (>90 days), Valid (31-90 days), Expiring (<30 days), Expired, Pending.
- EF entity `VwRdPassport` + DbContext mapping (Passport ID as key, ToView).
- `ReportDashboardHybridQueryService` now routes by `(category, subReport)`; only `(Passport, by-validity)` is promoted. `LoadSnapshot` stays on mock so Overview counts stay stable.
- `LoadPassportByValidityFromView` queries `db.VwRdPassport` via EFCoreObjectSpace; legacy EF Passport query kept as fallback.

**How to promote next:**
- Add `(Passport, "by-type")` or `(Passport, "by-citizenship")` to `RealSubReports` and switch Status mapping to TypeLabel / CitizenshipLabel — no new SQL view needed.

**Watch-outs:**
- Restart app (or FORCE_XAF_DB_UPDATE once) so SqlViewsUpdater creates the view before the panel query runs.
- UTF-8 no BOM required when rewriting Module files on Windows.
---

## 2026-07-15 — Postgres vw_rd_passport missing (42P01)

**Symptom:** `Npgsql.PostgresException 42P01: relation "vw_rd_passport" does not exist` when opening Passport → By Validity on local/Demo Postgres.

**Cause:** `SqlViewsUpdater` is SQL Server–only; Module Postgres updater list skipped it. Also `ProjectContracts.Name` is EF-ignored / dropped — views must use `NameTm` only.

**Fix:** `ReportDashboardPostgresViewsUpdater` + `SqlViews/vw_rd_passport.postgres.sql`; register in `Module.cs` Postgres branch. Sync SQL Server view to NameTm. Query loader catches `UndefinedTable` and falls back to legacy EF query. Local view created via psql for immediate use.
---

## 2026-07-15 — Passport By Validity empty on Postgres (GCRecord = 0)

**Symptom:** Panel showed 0 TOTAL RECORDS after view existed.

**Cause:** Imported Demo/Postgres rows store active soft-delete as `GCRecord = 0`, not `NULL`. View used `""GCRecord"" IS NULL` and dropped all 3658 passports.

**Fix:** Soft-delete predicate `COALESCE(""GCRecord"", 0) = 0` (same intent as SQL Server `ISNULL(GCRecord, 0) = 0`). Recreated local view → 3658 rows (Employees: 1740 Expired, 1512 Valid >90d, etc.).

**Prevent:** Never filter Postgres XAF tables with `GCRecord IS NULL` alone for report views when data may come from import.
---

## 2026-07-15 — Chart showed only Expired (preview skew)

**Symptom:** Passport By Validity pie was 100% Expired / table only 2014–2015 dates, while SQL had 4 validity buckets (1740/1512/30/7).

**Cause:** Chart regrouped from `PreviewRows` (`Take(50)` ordered by ascending `ExpirationDate` = oldest expired). `TotalCount` was `PreviewRows.Count`.

**Fix:** Aggregate `StatusBuckets` + `TotalCount` from full view `GroupBy`; Razor uses `Panel.StatusBuckets` / `Panel.TotalCount`; preview prefers non-Expired then soonest expiry.
---

## 2026-07-15 — Passport By Validity: one passport per Person (IssueDate)

**Rule:** Each Person may have multiple Passports; dashboard uses only the **latest by `IssueDate`** (tie-break `ID DESC`), excluding cancelled / soft-deleted.

**Implementation:** `ROW_NUMBER() OVER (PARTITION BY PersonID ORDER BY IssueDate DESC ...)` in `vw_rd_passport` (SS + Postgres). Local verify: 3658 → 3240 rows (346 people had multiple live passports).
---

## 2026-07-15 — Passport By Validity excludes Person.IsArchived

**Rule:** `vw_rd_passport` joins People with `COALESCE(IsArchived, false/0) = false`. Local: view 3240 → ~3053 (187 archived people excluded); live non-archived people = 3124.
---

## 2026-07-15 — Passport By Type promoted (same vw_rd_passport)

**Approach:** No second SQL view — reuse `vw_rd_passport` (already latest-by-IssueDate, `IsArchived` excluded, `TypeLabel` from PassportTypes.NameTm). Hybrid `RealSubReports` adds `(Passport, by-type)`. Loader `LoadPassportFromView` switches Status/buckets to `TypeLabel` + `st-cat-1..5`.

**Data note (local import):** all latest passports currently map to TypeLabel `P - MILLI PASPORT` (3119) — chart may show a single bucket until type diversity appears in source data.
---

## 2026-07-15 — Passport By Citizenship promoted

**View:** same `vw_rd_passport`; `CitizenshipLabel` now from `Person.Nationality` (`Countries.NameTm`), not passport IssuedCountry.
**Rules unchanged:** latest passport by IssueDate; exclude `IsArchived`.
**Hybrid:** `(Passport, by-citizenship)` promoted. Local Employees: 2846 rows, top Türkiýe 2172, then Hindistan/Russiýa/…

## 2026-07-15 — Passport Include archived toggle

**Ask:** Officers may include `Person.IsArchived` passports from Passport category; default excludes archived.

**Approach:** Expose `IsArchived` on `vw_rd_passport` (no hard filter in SQL). Filter in `LoadPassportFromView` / legacy when `includeArchivedPersons` is false. UI checkbox on Passport sub-tabs row (unchecked by default).

**Postgres gotcha:** `CREATE OR REPLACE VIEW` cannot insert/reorder columns — use `DROP VIEW` then `CREATE`. Local verify: 2846 active + 102 archived = 2948 Employees.

**Wire:** `IReportDashboardQueryService.LoadPanel(..., includeArchivedPersons)`; PropertyEditor refreshes panel on checkbox change.

## 2026-07-15 — Work Permit By Validity via vw_rd_work_permit

**View:** `vw_rd_work_permit` (SS + Postgres). One row per Person = current `WorkPermitItem` by latest `StartDate` (tie-break ID), matching `PersonCurrentItems.GetCurrentWorkPermitItem`. Non-cancelled only; soft-delete `COALESCE(GCRecord,0)=0`. Validity buckets match Passport mock: Valid (>90), Valid (31-90), Expiring (<30), Expired, Pending. `IsArchived` exposed for app filter (default exclude).

**Wire:** `VwRdWorkPermit` + DbContext; `LoadWorkPermitFromView`; Hybrid promotes `(WorkPermit, by-validity)`. by-status stays mock.

**Local note:** Demo Postgres had 406 `WorkPermits` headers but **0** `WorkPermitItems` — panel will show empty until items are imported; view itself creates cleanly.

## 2026-07-15 — Work Permit By Validity: Expired only if last WP of employee

**Ask:** Expired work permits in the report only when they are the employee's **last** WorkPermitItem; otherwise exclude.

**Cause:** `vw_rd_work_permit` ranked only **non-cancelled** items. When an employee had a newer **cancelled** extension, the view fell back to the prior expired permit and still showed Expired (example: Mehmet Kaplan — expired NC 2025-03→09, cancelled successor 2025-09→2026-03).

**Fix:** Rank by `StartDate DESC` among **all** non-deleted items (same idea as `PersonCurrentItems.GetCurrentWorkPermitItem`). Emit the row only when that last item is **not** cancelled. Superseded expired/valid predecessors disappear.

**Local verify (Employees, non-archived):** Total 1269 → **987**; Expired 985 → **710**; Valid (>90) stayed 256.

**Files:** `vw_rd_work_permit.sql` / `.postgres.sql`, `SqlViewsUpdater`, `ReportDashboardPostgresViewsUpdater`, `LoadWorkPermitLegacy`.

## 2026-07-15 — Prod PG missing View_VisaExtensionStatus

**Symptom:** Application Error on Report Dashboard (Prod `visa2026_prod`).
**Cause:** Keyless BO view only created by SQL Server `SqlViewsUpdater`; PG greenfield had no tracking views. Column quirk: `"CurrentVisaId"` not `CurrentVisaID`.
**Fix:** Postgres view + `ReportDashboardPostgresViewsUpdater.CreateViewVisaExtensionStatus`; also ensure `vw_rd_*` exist for Passport/WorkPermit panels.

## 2026-07-15 — Work Permit Include archived toggle

**Ask:** Same as Passport — officer can include/exclude `Person.IsArchived` work permits.

**Wire:** UI uses `ReportDashboardCatalog.SupportsIncludeArchivedPersons` (Passport + WorkPermit). Loader/view already filter `IsArchived` when `includeArchivedPersons` is false.

**Local Employees:** active 987 + archived 22 = 1009 when Include archived is on.

## 2026-07-15 — Work Permit By Status mock aligned to Visa State

**Ask:** Prototype `by-status` like Visa State (not Active/Pending).

**Change:** `WorkPermitByStatus()` mock rows use the same extension buckets as `VisaByState()`: Extension Started / to be Started / Not Required / Rejected / Cancelled (same CssClass mapping). Still mock-only; key remains `by-status`.

## 2026-07-15 — Visa Application Progress via vw_rd_visa_app_progress

**Ask:** SQL view for Visa **Application Progress** sub-report — source `ApplicationItem` rows with `CurrentVisa` on visa-extension `ApplicationTypes`.

**View:** `vw_rd_visa_app_progress` (SS + Postgres). One row per `ApplicationItem` where `CurrentVisaId` is set and type ∈ `App_Visa_Ext*` / `App_Visa_and_WP_Ext`. Latest `ApplicationProgress.State` → `ProgressStateLabel` + CssClass from state `Code`.

**Wire:** `VwRdVisaAppProgress` + `LoadVisaAppProgressFromView`; Hybrid promotes `(VisaExtension, app-progress)`.

**Local Employees (non-archived):** 2353 rows; top states RESMILEŞDİRİLDİ / Being Prepared / İŞLENMEKDE.

## 2026-07-15 — Projects Tab via vw_rd_projects

**Defaults:** people per `ProjectContract` (`NameTm`); person type filtered; date range ignored; hide count 0; family uses sponsor project when own is null.

**View:** `vw_rd_projects` (SS + PG) — `(ProjectOid, PersonRoleCode)` + `PersonCount`. `ProjectContracts.Name` not used (column dropped).

**Wire:** `VwRdProject`; `LoadProjectChips` from view; Hybrid `LoadSnapshot` = real projects + mock category counts. `LoadSnapshot` takes `personType`.

**Local Employees:** 55 projects; top Mary ~874, KYC ~465.

## 2026-07-15 — Person-type tabs via vw_rd_person_roles

**Ask:** Show totals in parentheses on Employees / Family Members / Temporary Visitors.

**View:** `vw_rd_person_roles` — `PersonRoleCode` + `PersonCount` for non-archived people (all people in role, project optional).

**Wire:** `VwRdPersonRole`; `Snapshot.PersonRoleCounts`; Hybrid uses real role counts; Razor `PersonTypeTabLabel`.

**Local:** Employees ~2851, Family ~273, Temporary Visitors 0 (if none imported).

## 2026-07-15 — Overview cards use StatusBuckets / TotalCount (not mock PreviewRows)

**Symptom:** Passport (and WP) Overview card showed mock sidebar count (147) and pie bars skewed from `PreviewRows` Take(50), while category detail showed real view data.

**Fix:** `GetOverviewBuckets` prefers `panel.StatusBuckets`; `GetCategoryCount` / `GetOverviewTotal` prefer `AllPanels[].TotalCount` when Overview has loaded panels via Hybrid.

## 2026-07-15 — Visa State Extension Started via vw_rd_visa_state

**Definition (combined analytics):** valid visa (exp ≥ today, not cancelled) + `CurrentVisa` on visa-extension ApplicationTypes + person's last/current visa (`StartDate`/`IssueDate` ranking).

**View:** `vw_rd_visa_state` with `StateLabel = 'Extension Started'` (other states to UNION later).

**Wire:** `VwRdVisaState`; `LoadVisaStateFromView`; Hybrid `(VisaExtension, visa-state)`.

**Local:** ~223 rows; Employees non-archived ~184.

## 2026-07-15 — Extension Started excludes PROCESS_CANCELLED progress

**Add:** `vw_rd_visa_state` NOT EXISTS any `ApplicationProgresses` with `ApplicationStates.Code = PROCESS_CANCELLED` for the parent Application.

**Local note:** existing Extension Started cohort already had 0 cancelled-in-history rows (Employees stayed ~184).

## 2026-07-16 — Visa By Category / By Type views (State · Category/Type)

**Ask:** Wire real **By Visa Category** + new **By Visa Type**; chart buckets = Visa State further split by category/type; all **valid** visas (multi per person OK); separate SQL views.

**Views:**
- `vw_rd_visa_by_category` / `vw_rd_visa_by_type` (`.sql` + `.postgres.sql`)
- One row per valid visa (exp ≥ today, not cancelled); `StatusLabel = StateLabel || ' · ' || Category|Type NameTm`
- State v1: **Extension Started** if visa is `CurrentVisa` on visa-ext ApplicationType and app has no `PROCESS_CANCELLED`; else **Extension Not Required**
- Preview ordered by `PersonName`, then expiry (persons with 2+ valid visas appear multiple times)

**Wire:** EF `VwRdVisaByCategory` / `VwRdVisaByType`; loaders; Hybrid promotes `(VisaExtension, by-category)` and `(VisaExtension, by-type)`; catalog tab `by-type`; updaters SS + PG.

**Local PG verify (import mid-chain; ApplicationItems empty):** non-archived **501** rows; 120 people with 2+ valid visas; all buckets currently `Extension Not Required · …` until Application wave lands (then Extension Started composites appear). Full valid set was 511 including ~10 archived.

## 2026-07-16 — By Category / By Type are not Visa State subtypes

**Ask:** Chart and table Status must show **only** VisaCategory or VisaType (e.g. `köp gezeklik`, `WP-Işçi Wiza`) — not `Extension Not Required · …`. These tabs are independent of Visa State.

**Fix:**
- Views drop StateLabel / composite Status; StatusLabel = CategoryLabel or TypeLabel only
- Loaders group on CategoryLabel / TypeLabel and assign `st-cat-1..5` (Passport by-type pattern)
- Catalog headers: "Visa Category" / "Visa Type"; mocks updated
- Updaters (SS + PG) synced to simplified SQL

## 2026-07-16 — All person-type tab (before Employees)

**Ask:** First tab grouping Employees + Family Members + Temporary Visitors.

**Change:**
- `ReportDashboardPersonType.All` (enum first); default PersonType = All
- Catalog: `IsAllPersonTypes` / `TryGetPersonRole`; ListView criteria skips single-role filter when All
- QueryService: nullable role filter; PersonRoleCounts[All] = sum of three; project chips aggregate across roles
- UI: All tab first in `ReportDashboardComponent.razor`

## 2026-07-16 — Missing View_VisaExtensionStatus on local Postgres

**Symptom:** Open ListView / VisaExtensionStatus ListView → `42P01: relation "View_VisaExtensionStatus" does not exist`.

**Cause:** Postgres path uses `ReportDashboardPostgresViewsUpdater` (not SqlViewsUpdater). Updater did not run on an already-current ModuleInfo DB.

**Fix:** Apply `Visa2026.Module/SqlViews/View_VisaExtensionStatus.postgres.sql` (or one-shot `FORCE_XAF_DB_UPDATE=true` then restart). Local verify: view returns rows after CREATE.

## 2026-07-16 — By Visa Period SQL view (real data)

**Ask:** Wire **By Visa Period** like category/type (not mock).

**View:** `vw_rd_visa_by_period` (SS + PG) — one row per valid visa; buckets `< 10 days` / `< 1 month` / `< 3..6 months` / `≥ 6 months`; CSS st-expiring (<30d) / st-pending (<90d) / st-approved.

**Wire:** EF `VwRdVisaByPeriod`; loader; Hybrid `(VisaExtension, by-period)`; updaters SS + PG. Also registered by-category/by-type calls in `SqlViewsUpdater.UpdateDatabaseAfterUpdateSchema` (methods existed but were not invoked).

**Local PG (non-archived):** 33 / 75 / 119 / 37 / 54 / 54 / 129 across the seven buckets.

## 2026-07-16 — By Visa Period = nearest Start→End duration (not days-to-expiry)

**Ask:** Period means granted length from StartDate to ExpirationDate; show nearest duration (1 month / 3 months / 6 months / 1 year); do not show start/end columns; valid visas only.

**Change:** `vw_rd_visa_by_period` snaps PeriodDays to nearest of 30/90/180/365; Status = PeriodLabel; table keeps Expiry only (not Start). Local: ~58 × 1 month, ~435 × 6 months.
## 2026-07-16 — Remove vertical category tab totals

**Ask:** Sidebar category counts (Visa 106, Invitation 75, …) are confusing vs panel TOTAL RECORDS / sub-report totals.

**Change:** ReportDashboardComponent.razor — vertical 
d-cat-tab shows label only (no count badge). Removed unused .rd-cat-tab-count CSS.

**Note:** Overview card counts and person-type tab counts (Employees (2851)) kept. Phase 4 w_rd_snapshot_counts for sidebar totals is not needed unless Overview/person-type still want a dedicated count view later.

## 2026-07-16 — Sub-report tab counts

**Ask:** Each Visa (and other category) subtype tab should show its total (e.g. valid visas for by-category/type/period).

**Change:** On category detail refresh, load every catalog sub-report panel TotalCount into `SubReportCounts`; sub-tabs render `Label` + count badge (CSS `.rd-sub-tab-count`).

## 2026-07-16 — By Days Remaining sub-report (option A buckets)

**Ask:** New Visa tab after By Visa Period; valid visas; closed remaining-day groups (A).

**Name/place:** **By Days Remaining** (`by-days-remaining`) after By Visa Period.

**View:** `vw_rd_visa_by_days_remaining` — buckets `< 10 days` / `< 1 month` / `< 3..6 months` / `≥ 6 months`; CSS st-expiring (&lt;30d) / st-pending (&lt;90d) / st-approved.

**Wire:** EF `VwRdVisaByDaysRemaining`; loader; Hybrid; catalog headers; mock; updaters.

**Local PG:** 33 / 75 / 119 / 37 / 54 / 54 / 129 (non-archived).
## 2026-07-16 — SqlViewsUpdater brace fix (by-days-remaining)

**Symptom:** `CS1519 Invalid token '}'` after appending `CreateViewRdVisaByDaysRemaining` — class closed early before the new method.

**Fix:** Keep the new method inside the class; only close method/class/namespace at file end. Module build: 0 errors.
## 2026-07-16 — One last valid visa per person checkbox

**Ask:** Distinguish total valid visas vs persons with a valid visa (people can hold 1–2 valid visas).

**UI:** Visa category checkbox **One last valid visa per person** (next to sub-tabs; shared across Visa sub-reports).

**Rule:** When checked, by-category / by-type / by-period / by-days-remaining keep one row per `PersonOid` — latest `ExpirationDate`, then highest visa ID. Visa State / Application Progress unchanged.

**Wire:** `LoadPanel(..., oneLastValidVisaPerPerson)`; catalog `SupportsOneLastValidVisaPerPerson` / `SubReportCountsValidVisas`; sub-tab badges refresh with the filter.
## 2026-07-16 — Work Permit By Days Remaining (+ one-last checkbox)

**Ask:** Replace By Validity; exclude Expired; same days-remaining buckets as Visa; rename tab; checkbox like Visa (default on); By Status unchanged.

**Clarification (#4):** when a person has 2+ valid WPs, “last” = latest ExpirationDate (same as Visa).

**View:** `vw_rd_work_permit` now one row per valid WP (not cancelled, not expired); buckets `< 10 days` … `≥ 6 months`; `DaysRemaining` column.

**Wire:** catalog key `by-days-remaining`; loader + Hybrid; `OneLastValidWorkPermitPerPerson` (default true); Include archived kept.

**Local PG (non-archived all valid):** 12 / 40 / 95 / 31 / 46 / 53 / 126 (~403).

## 2026-07-16 — Application category tab (before Visa)

**Ask:** Add vertical **Application** category before Visa.

**Catalog:** `ReportDashboardCategory.Application` first in enum + `Categories`; label Application; sub-reports `by-progress` / `by-type`; ListView `Application_ListView`; list criteria header BO (`True` + ProjectContract).

**Data:** Mock panels + Overview counts; Hybrid not promoted (stays mock). LoadApplication stub on real QueryService for later SQL view.

**Build:** Module Debug OK.
## 2026-07-16 — Education + Position History vertical categories

**Ask:** Add Education and PositionHistory to the Report Dashboard left category nav.

**Change:**
- Enum: Education, PositionHistory (after Passport)
- Catalog: labels Education / Position History; ListViews Education_ListView / EmployeePositionHistory_ListView; person+project criteria like Passport
- Sub-reports (mock): Education y-level / y-country / y-specialty; Position History y-status / y-position
- Mock panels + Overview cards; Hybrid stays on mock until SQL views
- Real LoadPanel falls through to EmptyPanel until views are wired

## 2026-07-16 — Application real data via vw_rd_application

**Ask:** Promote Application category (suggested after mock tab).

**View:** vw_rd_application (SS + PG) — one row per header Application; latest ApplicationProgress -> ProgressStateLabel/Css; ApplicationTypes -> TypeLabel; Name = first ApplicationItem person (fallback app number); project from Application.ProjectContract.NameTm.

**Wire:** VwRdApplication + DbContext; LoadApplicationFromView; Hybrid RealSubReports for by-progress and by-type; SqlViewsUpdater + ReportDashboardPostgresViewsUpdater.

**Local PG verify:** 12223 apps, 21 types (By Type OK). By Progress all Being Prepared because ApplicationProgresses is empty on this DB (0 rows / LatestProgressId null) — view join is correct; buckets will fill after progress import.

**Build:** Module Rebuild 0 errors.
## 2026-07-16 — Education + Position History SQL views (real data)

**Ask:** Continue after adding vertical tabs — wire real SQL views (suggested).

**Views:**
- `vw_rd_education` (SS + PG) — one row per Education; LevelLabel / CountryLabel / SpecialtyLabel; Institution + GraduationYear; family project via sponsor
- `vw_rd_position_history` (SS + PG) — one row per EmployeePositionHistory; Status Current/Ended; PositionLabel from Positions.NameTm

**Wire:** `VwRdEducation` / `VwRdPositionHistory`; loaders; Hybrid promotes all five sub-reports; SqlViewsUpdater + ReportDashboardPostgresViewsUpdater.

**Filters:** person type + project; non-archived people only; date range ignored (master data).
## 2026-07-16 — Education/PositionHistory 0 records (views missing)

**Symptom:** Education By Level showed 0 TOTAL RECORDS after Hybrid promote; other categories (projects/person roles) worked.

**Cause:** `vw_rd_education` / `vw_rd_position_history` not in local Postgres (`to_regclass` null). Updater did not run on already-current ModuleInfo DB. Data exists: Educations 3177, EmployeePositionHistories 3063.

**Fix:** Applied `SqlViews/vw_rd_*.postgres.sql` via psql. Reload dashboard (no code change). Use FORCE_XAF_DB_UPDATE once on next F5 if views should come from updater.

## 2026-07-16 — Passport subtypes scoped to ApplicationItem + ApplicationDate

**Ask:** Passport category subtypes (by-type / by-citizenship / by-validity) should only include Passports used on ApplicationItems, with parent Application.ApplicationDate inside the dashboard date-range filter (top-right).

**Change:**
- `vw_rd_passport` (SS + PG) rewritten: one row per ApplicationItem with `CurrentPassportID` set (not latest passport per person).
- View exposes `ApplicationDate` + `PassportOid`; soft-delete filters on ApplicationItems / Applications / Passports / People.
- `LoadPassportFromView` filters `ApplicationDate >= cutoff` (same pattern as Application / Visa app-progress).
- Legacy loader mirrors ApplicationItem universe; CountCategory Passport updated.
- Include-archived toggle unchanged (Person.IsArchived).

**Grain:** one ApplicationItem usage per row (same passport can appear more than once if used on multiple apps in range).

**Deploy:** recreate view (Postgres DROP+CREATE; or FORCE_XAF_DB_UPDATE once). Restart app after updater/psql apply.

## 2026-07-16 — Application Include completed / cancelled checkboxes

**Ask:** Dynamically exclude completed and cancelled Application processes (ApplicationProgress latest state).

**Rules (confirmed):** completed = PROCESS_ISSUED only; cancelled = PROCESS_CANCELLED only; defaults exclude both; shared across By Progress and By Type.

**UI:** Include completed / Include cancelled next to Application sub-tabs (unchecked by default).

**Wire:** ProgressStateCode on vw_rd_application; LoadApplicationFromView filters when flags false; LoadPanel params + Hybrid/Mock/PropertyEditor/Model.

**Note:** Local PG still has empty ApplicationProgresses — toggles are no-ops until progress import; filter ready for real state codes.
## 2026-07-16 — Passport 42703 ApplicationDate missing on PG view

**Symptom:** 42703: column v.ApplicationDate does not exist in LoadPassportFromView (Overview refresh).

**Cause:** Local w_rd_passport was stale (pre-ApplicationDate column); C# filters on ApplicationDate.

**Fix:** Recreate view from w_rd_passport.postgres.sql (adds ApplicationDate). Loader also catches UndefinedColumn and falls back to legacy EF query.

**Note:** Run DB updater or psql after view schema changes on dev PG.
## 2026-07-16 — Education Include archived checkbox

**Ask:** Toggle to include/exclude education rows for archived persons on all Education sub-reports.

**Wire:** Added Education to SupportsIncludeArchivedPersons; LoadEducation respects includeArchivedPersons (default exclude via vw_rd_education.IsArchived). Reuses existing Include archived UI checkbox.

**Local PG:** 3002 non-archived / 175 archived education rows (3177 total).
## 2026-07-16 — Loading progress bar on dashboard tab switches

**Ask:** Progress bar / loading feedback when switching between dashboard tabs (Overview + category tabs).

**Change:**
- `RefreshAsync` replaces sync `Refresh`; yields between category/sub-report loads so Blazor can paint.
- Model: `IsLoading`, `LoadingProgressPercent`, `LoadingMessage`; first paint via `InitialLoadRequested` + `OnAfterRenderAsync`.
- Overlay progress bar in `rd-report-area`; Overview fills cards progressively; tabs/filters disabled while loading.
- Generation counter cancels stale overlapping refreshes.

**UX:** Message shows current category (Overview) or sub-report name; percent advances per loaded panel.
## 2026-07-16 — Progress bar missing after first load (centered off-screen)

**Symptom:** First Overview load showed the progress bar; later tab switches only dimmed content with no visible bar.

**Cause:** Overlay used align-items:center over a tall rd-report-area (chart + table), so the panel sat in the vertical middle - often below the viewport. User only saw the white dim layer.

**Fix:** Pin overlay content to the top (flex-start + top padding); paint overlay immediately in the component before parent refresh; Task.Delay(16) before heavy DB work; clear stale Panel on category/sub-report change.

## 2026-07-16 — Education local Last-N months (ApplicationItem.CurrentEducation)

**Ask:** Move global Last-N filter into Education only; filter Education sub-reports + Overview Education card.

**Rule:** Include Education only if used as ApplicationItem.CurrentEducation and Application.ApplicationDate is within Last N months (default **9**). Educations never linked in-range are excluded (no unused bucket).

**UI:** Removed top-right global period picker. Period control on Education sub-tab filters and Overview toolbar (Education-scoped label).

**Wire:** LoadEducation / by-country / legacy filter via ApplicationItems Any(... ApplicationDate >= cutoff). SupportsCategoryDateRange(Education).

## 2026-07-16 — Passport local Last-N months (separate from Education)

**Ask:** Passport category also gets its own Last N months control.

**Change:** PassportDateRangeMonths (default 9) independent of Education DateRangeMonths. SupportsCategoryDateRange includes Passport. Overview toolbar shows Education + Passport pickers. ResolveDateRangeMonths routes LoadPanel cutoff per category (Passport already filtered by Application.ApplicationDate on CurrentPassport rows).

## 2026-07-16 — Position History local Last-N months

**Ask:** Position History gets its own Last N months (like Education/Passport).

**Rule:** Include EmployeePositionHistory only when used as ApplicationItem.CurrentPositionHistory and Application.ApplicationDate is within range (default 9).

**UI:** PositionHistoryDateRangeMonths; Overview toolbar Position · Last; category Last control via SupportsCategoryDateRange.

## 2026-07-16 — Address of Residence category (below Travel)

**Ask:** Add AddressOfResidence vertical category tab below Travel.

**Shipped:** Category after Travel; sub-reports By Validity + By Region; own Last N months (default 9) via ApplicationItem.CurrentAddressOfResidence + Application.ApplicationDate; Include archived; By Region default bar chart. Real Hybrid loaders; mock counts/rows for Overview until snapshot promotion.

## 2026-07-17 — AddressOfResidence FullAddress lazy-load crash (AsNoTracking)

**Symptom:** InvalidOperationException — navigation AddressOfResidence.Lodging cannot be loaded because FK/shadow props and entity is not tracked. Stack: LoadAddressOfResidence → a.FullAddress → Lodging getter.

**Cause:** Dashboard query uses AsNoTracking; FullAddress touches Lodging/Hotel/Hospital/OtherSite via lazy proxies. Shadow FKs only load when tracked.

**Fix:** Eager .Include Lodging, Hotel, Hospital, OtherSite (plus Person/Region) in LoadAddressOfResidence; wrap FullAddress access in try/catch as belt-and-suspenders.

## 2026-07-17 — Address of Residence By City

**Ask:** Add a By City sub-report alongside By Validity and By Region.

**Change:** Group the same date-filtered AddressOfResidence records by the City lookup, show City in the preview status column, and use the categorical bar chart. City is eagerly included because the real loader uses AsNoTracking.

## 2026-07-17 — Valid visa only filter (multi-category)

**Ask:** Checkbox to include only persons with a valid visa, alongside Last N months; default checked.

**Scope:** Registration, Work Permit, Travel, Border Zone, Passport, Education, Position History, Address of Residence.

**Rule:** When checked, filter to Person IDs with at least one Visa where !IsCancelled and ExpirationDate >= today (same as Visa dashboard valid visa). Unchecked = no visa-person filter.

**UI:** Valid visa only checkbox in category filter row; ValidVisaPersonsOnly default true on model.
## 2026-07-17 — Passport Last-N must AND with Valid visa only

**Ask:** Filtering = Last N months AND Valid visa only (when checked). Passport/Education totals need not match (different row types).

**Bug:** Passport allowed ApplicationDate IS NULL through the date filter (
ull || >= cutoff), so Last N was not strict.

**Fix:** Passport view + legacy require ApplicationDate != null && >= cutoff, then AND Valid visa person IDs when checkbox is on. Education already used ApplicationDate >= cutoff + Valid visa AND.
## 2026-07-17 — Passport: one last passport per person (IssueDate)

**Ask:** Passport sub-reports should include only each person's last passport; last = latest IssueDate.

**Change:** Added IssueDate to vw_rd_passport (SS + PG + updaters + VwRdPassport). LoadPassportFromView / Legacy filter Last N + Valid visa, then TakeOneLastPassportPerPerson (IssueDate DESC, PassportOid DESC) — same ranking as PersonCurrentItems.GetCurrentPassport. Totals/charts/preview use the deduped set (one row per person).

**Deploy note:** Restart app (or FORCE_XAF_DB_UPDATE) so SqlViewsUpdater recreates the view with IssueDate; otherwise UndefinedColumn falls back to legacy path which also dedupes.
## 2026-07-17 — Passport Valid visa only ignores Last N months

**Ask:** When Valid visa only is checked on Passport, total must match Visa By Category (one last valid visa per person); Last N months must be ignored.

**Change:** Passport branches: Valid visa only → load Passports for valid-visa person IDs (no ApplicationDate cutoff), one last passport per person by IssueDate. Unchecked → existing Last-N ApplicationItem path. UI disables Passport Last picker while Valid visa only is on.
## 2026-07-17 — Address Valid visa only ignores Last N months

**Ask:** Apply Passport filtering pattern to Address of Residence.

**Change:** When Valid visa only is checked, Address of Residence ignores Last N months / ApplicationItem.CurrentAddressOfResidence usage and loads addresses for valid-visa person IDs, then keeps one current address per person using PersonCurrentItems.GetCurrentAddressOfResidence ranking (valid current address preferred; latest expiration then ID). The Address Last picker is disabled while Valid visa only is on. Unchecked still uses the Last-N ApplicationItem.CurrentAddressOfResidence path.
## 2026-07-17 — Position History Valid visa only ignores Last N months

**Ask:** Apply Passport / Address filtering pattern to Position History.

**Change:** When Valid visa only is checked, Position History ignores Last N months / ApplicationItem.CurrentPositionHistory usage and loads vw_rd_position_history for valid-visa person IDs, then keeps one current position per person (prefer StatusLabel != Ended, then latest StartDate, then ID — same ranking as PersonCurrentItems.GetCurrentPositionHistory). Position Last picker disabled while Valid visa only is on. Unchecked still uses Last-N ApplicationItem path.
## 2026-07-17 — Education Valid visa only ignores Last N months

**Ask:** Apply Passport / Position History filtering pattern to Education so Valid visa only can align closer to Visa By Category person set.

**Change:** When Valid visa only is checked, Education ignores Last N months / ApplicationItem.CurrentEducation usage and loads vw_rd_education (and by-country view / legacy) for valid-visa person IDs, then keeps one current education per person (latest GraduationYear then ID — same ranking as PersonCurrentItems.GetCurrentEducation). Education Last picker disabled while Valid visa only is on. Unchecked still uses Last-N ApplicationItem path. Totals may still be below Visa By Category when some valid-visa people have no education row.
## 2026-07-17 — Subcontractor vertical category

**Ask:** Add Subcontractor tab to Report Dashboard vertical tabs below Position History.

**Change:**
- Enum: `Subcontractor` after `PositionHistory`
- Catalog: label Subcontractor; sub-report `by-company` (By Company); ListView `Person_ListView`; Valid visa only + Include archived; no Last-N (master data on Person)
- Mock Overview counts + preview rows
- Real `LoadSubcontractor` groups `Person` by `Subcontractor.NameTm` (Unassigned when null); Hybrid promotes `(Subcontractor, by-company)`; default chart bar

**Note:** Sidebar category counts still mock via Hybrid snapshot until `vw_rd_snapshot_counts`.
## 2026-07-17 — Medical Records vertical category

**Ask:** Add Medical Records tab below Subcontractor on Report Dashboard vertical tabs.

**Change:**
- Enum: `MedicalRecord` after `Subcontractor`; label Medical Records
- Sub-report `by-validity`; ListView `MedicalRecord_ListView`
- Last-N via `ApplicationItem.CurrentMedicalRecord` + `Application.ApplicationDate` (Medical · Last toolbar); ignored when Valid visa only
- Valid visa only + Include archived; one current medical per person (latest IssueDate then ID)
- Real `LoadMedicalRecord` + Hybrid promote `(MedicalRecord, by-validity)`
## 2026-07-17 — Passport Archived sub-report (replace Include archived)

**Ask:** Refactor Passport category — dedicated Archived sub-report; remove Include archived checkbox for Passport.

**Archived means (any of):**
1. `Person.IsArchived`
2. Passport is not the person's last (latest `IssueDate`, tie-break ID — same as `GetCurrentPassport`)
3. Passport is last and `ExpirationDate <= today - 1 month`

**Active** (By Validity / Type / Citizenship): last passport only; person not archived; not expired for a full month. Recently expired (<1 month) stay in By Validity as Expired.

**Archived chart buckets (reason priority):** Person archived → Superseded → Expired (>1 month).

**Wire:** Catalog sub-report `archived`; remove Passport from `SupportsIncludeArchivedPersons` (WP/Education/etc. keep checkbox); `LoadPassportArchived`; Hybrid promotes `(Passport, archived)`; Open ListView includes archived persons when on Archived tab.

**Person scope for Archived:** same as active (Valid visa only IDs, or persons with ApplicationItem.CurrentPassport in Last N months).
## 2026-07-17 — Address of Residence: Address Type + By Address sub-reports

**Ask:** Add Address Type and By Address tabs next to By Validity / By Region / By City.

**Change:**
- Catalog keys `by-address-type` (Address Type), `by-address` (By Address); table headers; Hybrid promotes both
- Real `LoadAddressOfResidence`: group by `ResidenceType` (Lodging / Hotel / Private House / Hospital / Other / Unknown) or `FullAddress` (Unknown when blank); By Address preview ColumnA = City
- Default chart bar for both; mock preview rows added

## 2026-07-17 — By Address includes Region + City + FullAddress

**Ask:** By Address chart/labels should include FullAddress, Region, and City (not FullAddress alone).

**Change:** Group/chart by `AddressOfResidence.DisplayAddress` (Region, City, FullAddress). Preview ColumnA = Region · City; Status = full display string.
## 2026-07-17 — Address By Validity = Private House only

**Ask:** By Validity is related only to Private House address type.

**Change:** `LoadAddressOfResidence` by-validity path filters `Type == ResidenceType.PrivateHouse` before expiry buckets. Other Address sub-reports (Region/City/Address Type/By Address) still include all types. Fixes Pending-heavy pie when Lodging rows have no ExpirationDate.
## 2026-07-17 — Rename Address By Validity → Private House Validity

**Ask:** Rename tab to represent Private House validity.

**Change:** Catalog key `private-house-validity`, label **Private House Validity** (was by-validity / By Validity). Hybrid promote updated. Query path unchanged (still Private House filter on non-categorical address sub-reports).
## 2026-07-17 — Address sub-tab label: By Private House Validity

**Ask:** Rename the "By Validity" sub-report tab (not the category).

**Change:** Keep key `by-validity`; Label = **By Private House Validity**. Reverted `private-house-validity` key rename.
## 2026-07-17 — Private House Validity states

**Ask:** By Private House Validity states: empty ExpirationDate → ExpirationNotSet; with date → Valid / Expiring / Expired.

**Change:** `PrivateHouseValidityBucket` + CSS; Expiring window from `ExpirationAlertRule` for AddressOfResidence (fallback 30 days, same as Document expiration alerts). No longer uses generic ExpirationBucket (Pending/Approved/Expiring Soon).
## 2026-07-17 — Remove Passport Archived sub-report

**Ask:** Remove Archived sub-report from Passport; not needed right now.

**Change:** Dropped catalog tab `archived`, Hybrid promote, mock rows, `LoadPassportArchived` path, and Open ListView archived special-case. Passport keeps By Validity / Type / Citizenship only (still excludes Person.IsArchived).
## 2026-07-17 — Position History: visa vs actual Position tabs

**Ask:** Rename By Position → Position (visa reports); add Position (actual / company) from EmployeePositionHistory.Position vs ActualPosition.

**Change:**
- Catalog: `by-position` label **Position (visa reports)**; new `by-actual-position` **Position (actual / company)**
- `vw_rd_position_history` (+ SS/PG updaters): `ActualPositionLabel` from ActualPositions.Name
- Load groups by PositionLabel vs ActualPositionLabel; preview ColumnA = visa PositionName
- Hybrid promote + bar default for both
## 2026-07-17 — Remove Position History By Status

**Ask:** Do not need By Status sub-report for Position History.

**Change:** Dropped catalog/Hybrid/mock By Status; default is Position (visa reports). Legacy `by-status` key remaps to `by-position`.
## 2026-07-17 — Remove Invitation Application Progress sub-report

**Ask:** Remove Application Progress sub-report from Invitation category.

**Change:** Catalog keeps only `issued-inv` (Issued Invitations); dropped mock `InvitationByAppProgress` and table-header arm. Visa category `app-progress` unchanged.
## 2026-07-17 — Remove Visa Application Progress sub-report

**Ask:** Remove Application Progress from Visa category sub-reports.

**Change:** Dropped catalog `app-progress`, Hybrid promote, mock `VisaByAppProgress`. Legacy `app-progress` key remaps to Visa State. Application category By Progress unchanged.
## 2026-07-17 — Application: ApplicationType tabs + combined State label

**Ask:** Refactor Application category — tabs = ApplicationTypes in current filters; chart State = combined `State · Ministry depth · Approval leg · Migration SLA`; remove By Progress / By Type; tab order by count desc; hide zero-count types; table keeps App # / App Date with Status = combined label.

**Change:**
- Catalog: static Application entry is overview chip only (`all`); detail tabs from `ListSubReports` (`type:{guid:N}`)
- `IReportDashboardQueryService.ListSubReports` + PropertyEditor `DynamicSubReports`; Hybrid always uses Real for Application
- `LoadApplication` EF path with `FormatApplicationCombinedStateLabel` (CurrentState, FormatMinistryReviewDepthLabel, ApprovalLegProfile name, MigrationSlaStatement)
- Table headers: Name / Project / App # / App Date / State
- Empty types hidden; re-sort on filter change via ListSubReports

**Watch-outs:** Combined labels need Application BO display helpers (not vw_rd_application alone). Overview Application card still loads `all` (all types under filters).
## 2026-07-17 — Application: single Application Status sub-report

**Ask:** Remove ApplicationType sub-report tabs; add Application Status where chart status is the combined label State · Ministry depth · Approval leg · Migration SLA.

**Change:**
- Catalog: only `app-status` / Application Status (no type tabs)
- Dropped dynamic `ListSubReports` ApplicationType listing and UI `DynamicSubReports`
- `LoadApplication` always groups by combined state label; remaps legacy by-progress/by-type/all/type:* → app-status
- Single sub-report → no detail sub-tabs row (Count == 1)

## 2026-07-23 — Registration: Registered Visas + rename By Region (mock)

**Ask:** Drop By Validity; add Registered Visas; rename By Region → Registered By Region. Dummy data only.

**Change:**
- Catalog: keys `registered-visas` / `by-region` (labels Registered Visas / Registered By Region)
- Mock: `RegistrationRegisteredVisas()` (Visa # + Registration State buckets); region mock unchanged
- Default chart for Registered By Region → bar
- Still mock-only (not in Hybrid `RealSubReports`)

**Files:** `ReportDashboardCatalog.cs`, `ReportDashboardMockQueryService.cs`, `ReportDashboardPropertyEditor.cs`, `reference.md`

## 2026-07-23 — Registration: Registered By City (mock)

**Ask:** Add Registered By City sub-report under Registration (dummy data).

**Change:** Catalog key `by-city` / label Registered By City; mock `RegistrationByCity()` (Status = city); default chart bar. Still mock-only.

**Files:** `ReportDashboardCatalog.cs`, `ReportDashboardMockQueryService.cs`, `ReportDashboardPropertyEditor.cs`, `reference.md`

## 2026-07-23 — Registration: Registration Validation (mock)

**Ask:** Add Registration Validation sub-report under Registration (dummy data).

**Change:** Catalog key `registration-validation`; mock `RegistrationValidation()` with Valid / Expiring Soon / Expired / Pending / Not Registered buckets; columns Visa # / Expiry / Validation. Still mock-only.

**Files:** `ReportDashboardCatalog.cs`, `ReportDashboardMockQueryService.cs`, `reference.md`

## 2026-07-23 — Registration sub-report label rename

**Ask:** Rename Registered Visas → Registration Processes; Registered By Region/City → Registration Processes By Region/City.

**Change:** Catalog labels only (keys unchanged: `registered-visas`, `by-region`, `by-city`). Mock comments updated. Registration Validation label unchanged.

## 2026-07-23 — Registration Processes SQL view + Check Out tabs

**Ask:** One row per person; count when sent to migration service (PROCESS_STARTED+); hide anyone with any check-out from Registration Processes; add Check Out Process / By Region / By City.

**Rules encoded in `vw_rd_registration`:**
- ProcessFamily `registration`: Check-In / Ext / Info Change types; latest state in PROCESS_STARTED|ISSUED|REJECTED|CANCELLED; exclude persons with any Check-Out app
- ProcessFamily `checkout`: Check-Out / Check-Out Internal; same sent-to-migration rule; one last app per person
- Chart: progress state (or Region/City for geo sub-reports)

**Also:** Catalog check-out tabs (mock + real via same view); Hybrid promotes all except `registration-validation`; ListView → ApplicationItem.

**Files:** `vw_rd_registration.sql` (+ postgres), `VwRdRegistration.cs`, SqlViewsUpdater, Postgres updater, DbContext, QueryService, Catalog, Mock, Hybrid, PropertyEditor.

## 2026-07-23 — Registration sub-reports = ApplicationType + Process State

**Ask:** Replace Registration category tabs with registration ApplicationType names; chart/table Status = application process state (latest ApplicationProgress).

**Rules:**
- One row per not-expired visa; last registration app via `ApplicationItem.CurrentVisa`
- Sub-report key = `ApplicationType.Name`; label = `NameTm`
- Status = latest `ApplicationState` (NameTm)

**Local PG verify:** App_Reg_ext 327, Check_In 87, Check_Out 67, Info_Change_Address 29, Info_Change_Passport 2, Check_In_Internal 1.

**Files:** catalog, `vw_rd_registration`, `VwRdRegistration`, QueryService, Hybrid (whole Registration → real), Mock, updaters.

## 2026-07-23 — Registration tabs: short labels + order by count

**Ask:** Shorter sub-report tab names; order by total descending.

**Change:** Labels Check-In / Extension / Address Change / …; `OrderedSubReports(category, SubReportCounts)` in Razor sorts Registration by count.

## 2026-07-23 — Registration missing progress = OFISDE (At Office)

**Ask:** Unknown process state means at-office.

**Change:** `vw_rd_registration` fallback ProgressStateLabel `OFISDE`, ProgressStateCode `AT_OFFICE` when no ApplicationProgress/state (same NameTm as ApplicationLocations.AT_OFFICE).
## 2026-07-23 — Expiring State (Registration)

**Confirmed rules:**
1. Count last registration app types: Check-In, Check-In (Internal), Extension, Address Change, Visa Change, Passport Change. Exclude Check-Out / Check-Out (Internal).
2. Chart: days-to-expiry buckets `< 7 days` · `< 14 days` · `< 1 month` · `< 3 months` · `< 6 months` · `≥ 6 months`.
3. Grain: one person → one last valid visa (longest `VisaExpirationDate` among active-type rows).

**Local PG:** 318 persons; buckets `< 1 month` 5, `< 3 months` 36, `< 6 months` 149, `≥ 6 months` 128 (no `< 7` / `< 14` in current data).

**Files:** catalog `expiring-state`, `vw_rd_registration` DaysRemaining/ExpiryBucket*, QueryService TakeOneLastValidVisaPerPerson, updaters synced.
## 2026-07-23 — Expiring State: always show day/week buckets

**Ask:** Chart was missing `< 7 days` / `< 14 days` (only month buckets visible).

**Cause:** Buckets built from GroupBy — empty day/week buckets omitted. Current one-last-visa set often has min remaining ≥ 14 days.

**Fix:** `RegistrationExpiringStateBuckets` fixed list; QueryService always emits all 6 buckets (0 allowed).
## 2026-07-23 — Expiring State tab pinned first

`OrderedSubReports`: Expiring State always first; remaining Registration tabs still by count desc.

## 2026-07-23 — To Be Checked In sub-report

**Rules:**
- Label: To Be Checked In; tab pinned 2nd (after Expiring State)
- Population: not-expired non-cancelled visa with no `ApplicationItem.CurrentVisa` to any `App_Reg_*`
- Grain: one person → one last valid visa
- Chart: days since latest `ExternalArrival.TravelDate` — `< 1 week` · `< 2 weeks` · `< 3 weeks` · `< 4 weeks` · `< 1 month` · `≥ 1 month` (+ `No entry date`); always show all

**Local PG:** 4 persons, all `≥ 1 month`.

**Files:** `vw_rd_to_be_checked_in` (+ postgres), `VwRdToBeCheckedIn`, Catalog, QueryService, PropertyEditor, updaters.
## 2026-07-23 — To Be Checked In: in-country only

Require latest `TravelHistory` (by TravelDate, ID) is `ExternalArrival`. Excludes no history and `ExternalDeparture` (not in country). Entry date = that arrival.

## 2026-07-23 — To Be Checked Out sub-report

Pinned 3rd after To Be Checked In. Valid visa, `DaysRemaining < 7`, no `App_Reg_Check_Out` / `App_Reg_Check_Out_Internal` on CurrentVisa; one last visa/person; chart `< 1 day`…`< 7 days` (always show). In-country not required. Local PG: buckets `< 1 day` 3, `< 6 days` 8, `< 7 days` 6.

## 2026-07-23 — Check in by City

Pinned first. Same active reg types as Expiring State; city from last app `CurrentAddressOfResidence.City` (NameTm); one last visa/person; chart cities with data only. `vw_rd_registration.CityLabel`.

## 2026-07-24 — Invitation category: replace Issued Invitations with 6 sub-reports

**Ask:** Remove current Invitation validity-bucket states; add Ready (by project), In Process (Application Progress states), Rejected (by project), Used, Expired, By Visa Period and Category.

**Change:**
- Catalog: dropped `issued-inv`; added `ready-by-project`, `in-process`, `rejected-by-project`, `used`, `expired`, `by-period-category`
- Mock: separate row sets; Ready/Rejected/Used/Expired chart Status = Project; In Process = Application Progress state; Period·Category combined label
- Real `LoadInvitation`: EF dispatch matching those rules (RejectionItem preferred for rejected; ProcessRejected apps as fallback). Legacy `issued-inv` remaps to Ready
- Default chart = bar for all Invitation tabs
- Hybrid: Invitation still mock (not in `RealSubReports`)

**Definitions used:**
- Ready = InvitationItem not used/cancelled/changed, ExpirationDate ≥ today
- In Process = `CanIssueInvitation` application, no linked Invitation, progress not issued/rejected/cancelled
- Used = `IsUsed`; Expired = unused + ExpirationDate < today

**Files:** `ReportDashboardCatalog.cs`, `ReportDashboardMockQueryService.cs`, `ReportDashboardQueryService.cs`, `ReportDashboardPropertyEditor.cs`, `reference.md`, `IMPLEMENTATION_PLAN.md`
## 2026-07-24 — Invitation Ready: vw_rd_invitation_ready

**Ask:** SQL view for Ready Invitations sub-report (confirmed: InvitationItem grain; IsUsed/Cancelled/Changed = 0; ExpirationDate >= today; project = Application then Person then sponsor).

**Change:**
- `vw_rd_invitation_ready` (+ postgres) — StatusLabel = ProjectName (`(No project)` fallback)
- EF `VwRdInvitationReady` + DbContext; SqlViewsUpdater + ReportDashboardPostgresViewsUpdater
- `LoadInvitationReadyFromView`; Hybrid promote `(Invitation, ready-by-project)`; legacy EF fallback if view missing
- IssuedDate filter uses `Invitations.StartDate` (IssuedDate column mapping)

**Files:** SqlViews, VwRdInvitationReady.cs, Visa2026DbContext, SqlViewsUpdater, ReportDashboardPostgresViewsUpdater, ReportDashboardQueryService, ReportDashboardHybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-24 — Invitation In Process: vw_rd_invitation_in_process

**Ask:** SQL view for Invitations In Process (confirmed: one row per Application; CanIssueInvitation + no linked Invitation; exclude PROCESS_ISSUED/REJECTED/CANCELLED; simple progress Status; ministry rejects stay in-process).

**Change:**
- `vw_rd_invitation_in_process` (+ postgres); EF `VwRdInvitationInProcess`; updaters; `LoadInvitationInProcessFromView`; Hybrid promote
- Person-type filter uses any ApplicationItem role (not only first-person column)
- Fallback to legacy EF `LoadInvitationInProcess` if view missing

**Files:** SqlViews, VwRdInvitationInProcess.cs, DbContext, SqlViewsUpdater, ReportDashboardPostgresViewsUpdater, QueryService, HybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-24 — Invitation Rejected: vw_rd_invitation_rejected

**Ask:** SQL view for Invitations Rejected — union RejectionItems + PROCESS_REJECTED apps without Rejection header; Status = Project.

**Change:**
- `vw_rd_invitation_rejected` (+ postgres); composite key `(SourceKind, ID)`; EF `VwRdInvitationRejected`
- Hybrid promote `(Invitation, rejected-by-project)`; legacy EF fallback also unions both sources
- App-leg of union excludes apps that already have a `Rejections` row (no double-count)

**Files:** SqlViews, VwRdInvitationRejected.cs, DbContext, updaters, QueryService, HybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-24 — Invitation Used: vw_rd_invitation_used

**Ask:** SQL view for Used Invitations (InvitationItem IsUsed = 1; Status = Project).

**Change:** `vw_rd_invitation_used` (+ postgres); `VwRdInvitationUsed`; `LoadInvitationUsedFromView` (ColumnB = IssuedDate); Hybrid promote `(Invitation, used)`.

**Files:** SqlViews, VwRdInvitationUsed.cs, DbContext, updaters, QueryService, HybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-24 — Invitation Valid Until (rename Expired + view)

**Ask:** Rename Expired Invitations → Invitation Valid Until; SQL view grouped by days/weeks/months remaining; valid unused only; show only buckets with rows.

**Buckets:** `< 1 day` · `< 1 week` · `< 2 weeks` · `< 3 weeks` · `< 1 month` · `< 2 months` · `< 3 months` · `≥ 3 months`

**Change:** Catalog key `valid-until` (legacy `expired` remaps); `vw_rd_invitation_valid_until`; `VwRdInvitationValidUntil`; `LoadInvitationValidUntilFromView`; Hybrid promote; mock updated.

**Files:** Catalog, Mock, PropertyEditor, SqlViews, entity, DbContext, updaters, QueryService, HybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-24 — Invitation Valid Until showed 0 while Ready had 281

**Cause:** `vw_rd_invitation_valid_until` not created yet; loader fell back to `EmptyPanel` instead of EF. Ready had EF fallback so it still showed data.

**Fix:** `LoadInvitationValidUntilLegacy` (same filters + remaining buckets); missing-view catch walks exception chain and uses legacy. Restart app; optional `FORCE_XAF_DB_UPDATE` to create the view.
## 2026-07-24 — Ready By Project + Ready By VisaPeriod

**Ask:** Rename Ready Invitations → Ready By Project; add Ready By VisaPeriod after it (same Ready population, chart by VisaPeriod).

**Change:**
- Catalog: 
eady-by-project label "Ready By Project"; new 
eady-by-period "Ready By VisaPeriod"
- w_rd_invitation_ready adds `VisaPeriodLabel`; both sub-reports share the view
- Loader groups by `StatusLabel` (project) or `VisaPeriodLabel` (period); legacy EF fallback for period
- Hybrid promote `(Invitation, ready-by-period)`; bar chart default

**Files:** Catalog, Mock, PropertyEditor, SqlViews (+ postgres), VwRdInvitationReady, updaters, QueryService, HybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-24 — Ready By VisaCategory (drop Period+Category)

**Ask:** Remove By Visa Period and Category; add Ready By VisaCategory after Ready By VisaPeriod (same Ready population).

**Change:**
- Removed catalog/mock/loader `by-period-category`
- Added `ready-by-category`; `vw_rd_invitation_ready` adds `VisaCategoryLabel` (join VisaCategories)
- Loader groups by category; Hybrid promote; bar default

**Files:** Catalog, Mock, PropertyEditor, SqlViews (+ postgres), VwRdInvitationReady, updaters, QueryService, HybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-24 — Merge Ready Period + Category into one tab

**Ask:** Combine Ready By VisaPeriod and Ready By VisaCategory into one sub-report (combined Status).

**Change:**
- Removed tabs `ready-by-period` / `ready-by-category`
- Added `ready-by-period-category` ("Ready By Period · Category"); Status = `VisaPeriod · VisaCategory`
- Same Ready population / `vw_rd_invitation_ready` (uses both label columns); legacy keys remap
- Hybrid promote; bar default

**Files:** Catalog, Mock, PropertyEditor, QueryService, HybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-24 — Ready By Period · Category · Type

**Ask:** Add VisaType to Ready By Period · Category; prefer · over / in label.

**Change:**
- Tab/Status: `Ready By Period · Category · Type` / `period · category · type`
- `VisaTypeLabel` from `Invitation.Application.VisaType` (standalone invitations → `(No type)`)
- View/EF/updaters/loader/legacy/mock updated

**Files:** SqlViews (+ postgres), VwRdInvitationReady, updaters, Catalog, QueryService, Mock, learnings

## 2026-07-24 — Layer A localization (chrome + catalog + fixed status buckets)

**Scope:** UI chrome, Catalog labels (categories / sub-reports / person types / table headers), Home → Report Dashboard nav, and fixed English status/bucket labels at display time. Lookup/`NameTm` chart segments unchanged.

**Approach:**
- `ReportDashboard.*` keys in `UiStrings.messages.json` (en / tr-TR / tk-TM / ru-RU) → `VisaUiMessageCatalog.g.cs`
- Nav: `UiStrings.json` → `navigation.Home` + `ReportDashboard`
- Helper: `ReportDashboardLocalization` (`Status` maps exact English keys; Application Status combined labels localize leading segment only)
- Catalog: `CategoryLabel` / `PersonTypeLabel` / `SubReports` / `TableHeaders` resolve via helper; English keys remain in RawSubReports / EnglishTableHeaders / SQL
- Razor + PropertyEditor: display localized; `OnListView(bucket.Label)` keeps English for criteria

**Watch-outs:**
- Do not localize Status at query time — breaks ListView filters
- Prefer `title='@Get("key")'` (single-quoted attr) to avoid nested double quotes in Razor
- Merge messages with Node/UTF-8; PowerShell string rewrite can wipe `UiStrings.messages.json`

**Files:** UiStrings.messages.json, UiStrings.json, ReportDashboardLocalization.cs, ReportDashboardCatalog.cs, ReportDashboardComponent.razor, ReportDashboardPropertyEditor.cs, generated catalog/xafml, docs/REPORT_DASHBOARD.md

## 2026-07-25 — Visa: merge Category / Type / Period into one tab

**Ask:** Replace By Visa Category, By Visa Type, By Visa Period with a single sub-report; label order Period · Category · Type (Invitation).

**Change:**
- Catalog: one tab `by-period-category-type` ("By Period · Category · Type"); keep Visa State + By Days Remaining
- Status = `{Period} · {Category} · {Type}`; table header Period · Category · Type
- Loader joins `vw_rd_visa_by_period` + category/type sibling views by visa ID (no new SQL view)
- Legacy keys `by-category` / `by-type` / `by-period` remap; Hybrid promote; default chart bar
- Localization: `ReportDashboard.SubReport.by-period-category-type`

**Files:** Catalog, Mock, QueryService, HybridQueryService, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md

## 2026-07-25 — Visa By Days Remaining default Bar Chart

**Ask:** Make Bar Chart the default for Visa By Days Remaining.

**Change:** `DefaultChartViewFor(VisaExtension, by-days-remaining)` → `bar` (was pie via fallback).

**Files:** ReportDashboardPropertyEditor.cs

## 2026-07-25 — Visa On Extension sub-report (first tab)

**Ask:** First Visa tab ""On Extension""; Status = latest ApplicationProgress state; ApplicationItem on visa-ext types with CurrentVisa; include in-flight + completed/cancelled; default bar.

**Change:**
- Catalog key `on-extension` first; headers App # / App Date / Process State
- Reuse `vw_rd_visa_app_progress` + `LoadVisaAppProgressFromView` (no PROCESS_* filter)
- Hybrid promote; legacy `app-progress` / default → on-extension
- Mock `VisaOnExtension`; localization `ReportDashboard.SubReport.on-extension`

**Files:** Catalog, Mock, QueryService, HybridQueryService, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md

## 2026-07-25 — On Extension process labels = ApplicationProgress StatusListLabel

**Ask:** On Extension chart/table process names must match ApplicationProgress Progress Status (not ApplicationState.NameTm).

**Cause:** `vw_rd_visa_app_progress` preferred `NameTm` (e.g. RESMİLEŞTİRİLDİ / İŞLEM MERKEZİ).

**Fix:**
- View adds `ApplicationOid` + `ProgressStateCode`; SQL fallback prefers `LatestProgressDisplay` then `Name`
- Loader resolves Status via `LookupLocalization.GetDisplayName` + `ApplicationProgressListLabelHelper.FormatStatusLabel` + ministry short name (same as `ApplicationProgress.StatusListLabel`)

**Deploy:** recreate view (SqlViewsUpdater / Postgres updater, or FORCE_XAF_DB_UPDATE once) then restart.

**Files:** vw_rd_visa_app_progress (+ postgres), SqlViewsUpdater, ReportDashboardPostgresViewsUpdater, VwRdVisaAppProgress, ReportDashboardQueryService

## 2026-07-25 — Postgres 42703 ApplicationOid on On Extension

**Symptom:** `column v.ApplicationOid does not exist` opening On Extension (local PG).

**Cause:** EF entity/view SQL updated; DB still had old `vw_rd_visa_app_progress` (ModuleInfo current → updater skipped).

**Fix:** Recreate view via `vw_rd_visa_app_progress.postgres.sql` (psql). Loader also catches `UndefinedColumn` → legacy fallback.

## 2026-07-25 — On Extension chart = Project · Period · Category · Type · State

**Ask:** Group On Extension by Project + Visa Period/Category/Type; Process State in chart label.

**Change:** Status = `Project · Period · Category · Type · ProcessState`; Period/Category/Type from Application (`LookupLocalization`); ProcessState = `StatusListLabel`; categorical CSS; table header updated. No SQL view change (Include on Application).

**Files:** ReportDashboardQueryService, Catalog, Mock, ReportDashboardLocalization, UiStrings.messages.json (+ regenerate)

## 2026-07-25 — On Extension: Project · State; exclude Issued

**Ask:** Drop Period · Category · Type from On Extension Status; exclude Issued processes.

**Change:** Status = `Project · ProcessState`; filter out `PROCESS_ISSUED`; table header Project · State; mock updated.

**Files:** ReportDashboardQueryService, Catalog, Mock, ReportDashboardLocalization, UiStrings.messages.json (+ regenerate)

## 2026-07-25 — Visa Extension Result sub-report

**Ask:** New Visa tab "Extension Result" after By Days Remaining; same On Extension population (visa-ext apps with CurrentVisa); only terminal PROCESS_ISSUED / PROCESS_CANCELLED / PROCESS_REJECTED; chart by result only (localized process-state labels, not forced "Issued (Complete)"); Project · Result not used.

**Change:**
- Catalog key `extension-result` last; headers App # / App Date / Process State
- Reuse `vw_rd_visa_app_progress` via `LoadVisaAppProgressFromView` + `VisaAppProgressPanelMode.ExtensionResult`
- Status = ProcessState (`StatusListLabel` pattern); fixed CSS st-approved / st-expiring
- Hybrid promote; mock `VisaExtensionResult`; bar default; localization `ReportDashboard.SubReport.extension-result`

**Files:** Catalog, Mock, QueryService, HybridQueryService, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md

## 2026-07-25 — On Extension split: By Project + By Period · Category · Type

**Ask:** Rename On Extension → On Extension By Project; add On Extension By Period · Category · Type after it; same population (exclude Issued); Period · Category · Type · State grouping; consistent Period · Category · Type order and tab casing.

**Change:**
- Catalog labels/keys: `on-extension` (By Project), `on-extension-by-period-category-type` (second tab)
- Modes: `OnExtensionByProject` / `OnExtensionByPeriodCategoryType`; Status = Project · State vs Period · Category · Type · State (Application lookups)
- Hybrid promote; bar default; header `Period · Category · Type · State`; localization updated

**Files:** Catalog, Mock, QueryService, HybridQueryService, PropertyEditor, ReportDashboardLocalization, UiStrings.messages.json (+ regenerate), reference.md

## 2026-07-25 — Active By Project + rename Active By Period · Category · Type

**Ask:** Rename By Period · Category · Type → Active By Period · Category · Type; add Active By Project before it; same valid-visa population + one-last-valid toggle; chart by Project only.

**Change:**
- Catalog: `active-by-project` before `by-period-category-type`; labels updated
- Loader `LoadVisaActiveByProjectFromView` (`vw_rd_visa_by_period`, Status = Project); Hybrid promote; bar default; SubReportCountsValidVisas includes active-by-project

**Files:** Catalog, Mock, QueryService, HybridQueryService, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md

## 2026-07-25 — Visa tabs: Active first; remove Visa State

**Ask:** Move Active By Project + Active By Period · Category · Type to start; remove Visa State.

**Change:** Catalog order Active → On Extension → By Days Remaining → Extension Result; drop `visa-state` tab; default/legacy `visa-state` / `app-progress` / empty → `active-by-project`.

**Files:** Catalog, Mock, QueryService, HybridQueryService, PropertyEditor, reference.md

## 2026-07-27 — Active Visa (P) / Active Visa (V) rename

**Ask:** Rename Active By Project → Active Visa (P); Active By Period · Category · Type → Active Visa (V).

**Change:** Catalog labels + `ReportDashboard.SubReport.*` localization; keys unchanged (`active-by-project`, `by-period-category-type`).

**Files:** Catalog, UiStrings.messages.json (+ regenerate)

## 2026-07-27 — Visa Extension (P) / Visa Extension (V) rename

**Ask:** Rename On Extension By Project → Visa Extension (P); On Extension By Period · Category · Type → Visa Extension (V).

**Change:** Catalog labels + localization; keys unchanged (`on-extension`, `on-extension-by-period-category-type`).

**Files:** Catalog, UiStrings.messages.json (+ regenerate)

## 2026-07-27 — Visa By Days Remaining → Visa Validity

**Ask:** Rename Visa tab By Days Remaining → Visa Validity.

**Change:** Visa catalog label; category-specific `ReportDashboard.SubReport.VisaExtension.by-days-remaining` so Work Permit keeps shared By Days Remaining.

**Files:** Catalog, UiStrings.messages.json (+ regenerate)

## 2026-07-27 — Overview card totals match first sub-report

**Ask:** Visa Overview card (502) should match Active Visa (P) with one-last-valid (372).

**Cause:** Overview `LoadPanel` hardcoded `oneLastValidVisaPerPerson: false` (and other toggles) while category default is true.

**Fix:** Overview uses model toggle values when `Supports*` (same as category detail) for first `DefaultSubReport` panel.

**Files:** ReportDashboardPropertyEditor.cs

## 2026-07-27 — All Overview cards share first-subreport LoadPanel

**Ask:** All category cards follow the same pattern as Visa (match first sub-report snapshot).

**Change:**
- Shared `LoadPanelFor` for Overview + category detail (identical filter args)
- `GetOverviewTotal` / `GetCategoryCount` use `AllPanels[cat].TotalCount` whenever loaded (no mock when panel exists)

**Files:** ReportDashboardPropertyEditor.cs, ReportDashboardComponent.razor

## 2026-07-27 — Extension Result (P) / (V)

**Ask:** Rename Extension Result → Extension Result (P) (Project · ProcessState); add Extension Result (V) (Period · Category · Type · ProcessState); same terminal population; after Visa Validity.

**Change:** Modes `ExtensionResultByProject` / `ExtensionResultByPeriodCategoryType`; catalog + Hybrid + mock + localization; key `extension-result-by-period-category-type`.

**Files:** Catalog, QueryService, Mock, HybridQueryService, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md

## 2026-07-27 — Invitation Active Invitation (P) / (V) rename

**Ask:** Rename Ready By Project → Active Invitation (P); Ready By Period · Category · Type → Active Invitation (V).

**Change:** Catalog labels + localization; keys unchanged (`ready-by-project`, `ready-by-period-category`).

**Files:** Catalog, UiStrings.messages.json (+ regenerate)

## 2026-07-27 — Invitation Process / Rejected / Used (P)/(V) twins

**Ask:** Rename Invitation tabs to short (P)/(V) pattern (like Visa); add Period·Category·Type twins for Process, Rejected, Used; Validity stays singular.

**Change:**
- Catalog: Invitation Process/Rejected/Used (P)/(V) + Invitation Validity; new keys *-by-period-category-type
- Loaders: (P) Project [· ProcessState]; (V) Period · Category · Type [· ProcessState] via Application / Invitation includes after view load
- Hybrid promote new keys; mock + PropertyEditor bar defaults; UiStrings (+ regenerate)

**Files:** Catalog, QueryService, Mock, HybridQueryService, PropertyEditor, UiStrings.messages.json, reference.md
## 2026-07-27 — Invitation Process excludes completed + review rejects

**Ask:** Invitation Process only unfinished apps; Rejected is completed; also exclude 1st/2nd Review Rejected. Completed = Issued / Rejected / Cancelled (+ Issued duplicate).

**Change:** View + EF exclude PROCESS_ISSUED, PROCESS_REJECTED, PROCESS_CANCELLED, 1_REVIEW_REJECTED, 2_REVIEW_REJECTED. Loader also filters by ProgressStateCode for stale views. Prior rule “ministry rejects stay in-process” superseded for Invitation Process.

**Files:** vw_rd_invitation_in_process(.sql/.postgres.sql), SqlViewsUpdater, ReportDashboardPostgresViewsUpdater, QueryService, VwRdInvitationInProcess.cs
## 2026-07-27 — Invitation Process Result (rename Rejected)

**Ask:** Rename Invitation Rejected → Process Result (P)/(V); population like Extension Result: CanIssueInvitation apps with terminal latest progress; include 1st/2nd Review Rejected; use same localized ProcessState labels as Extension Result; Status = Project · ProcessState / Period · Category · Type · ProcessState.

**Change:** Keys process-result / process-result-by-period-category-type (legacy 
ejected-by-* remap); EF Application loader (not RejectionItem union); AssignExtensionResultCss; Hybrid/mock/localization.

**Files:** Catalog, QueryService, Mock, Hybrid, PropertyEditor, UiStrings (+ regenerate), reference.md
## 2026-07-27 — Process Result 0 / Rejected still in Invitation Process

**Symptom:** Process Result (P)/(V) = 0; Invitation Process still showed Project · Rejected.

**Cause:** Loaders keyed off Application.LatestProgress FK (often null). Display/view use progress history + LatestPrimaryStateCode (PROCESS_REJECTED → en ""Rejected""), so Rejected stayed in Process and never entered Process Result.

**Fix:** Filter/include via LatestPrimaryStateCode (fallback LatestProgress.State.Code); In Process also cross-checks Application scalars after view load; completed codes include 3–5_REVIEW_REJECTED.

**Files:** ReportDashboardQueryService.cs, vw_rd_invitation_in_process (+ updaters)
## 2026-07-27 — Registration: ignore Visa.IsCancelled

**Ask:** Cancelling a visa must not drop someone from registration / checked-in. Until Check-Out, still considered checked in. Cancelled vs not does not matter.

**Change:** Removed IsCancelled = 0 from w_rd_registration, w_rd_to_be_checked_in, w_rd_to_be_checked_out (SS + Postgres + updaters). Kept ExpirationDate >= today.

**Files:** SqlViews + SqlViewsUpdater + ReportDashboardPostgresViewsUpdater, VwRdRegistration.cs
## 2026-07-27 — Registration: keep expired visas until Check-Out

**Ask:** Expired visas stay in registration / checked-in population until Check-Out (same as cancelled).

**Change:** Removed ExpirationDate >= today from w_rd_registration, w_rd_to_be_checked_in, w_rd_to_be_checked_out. Expiring State / To Be Checked Out add **Expired** bucket when DaysRemaining < 0.

**Files:** SqlViews + updaters, ReportDashboardCatalog.cs, VwRdRegistration.cs
## 2026-07-27 — Check in by Project (P)/(V)

**Ask:** Add Check in by Project as first Registration sub-report with (P)/(V) twin; same population as Check in by City; projects with data only; pin before City.

**Change:** Keys check-in-by-project / check-in-by-period-category-type; Status = Project / Period · Category · Type (PCT from Application via ApplicationItem); shared check-in population path with City; DefaultSubReport becomes Project (P).

**Files:** Catalog, QueryService, Mock, PropertyEditor, UiStrings (+ regenerate), reference.md
## 2026-07-27 — Active Registered rename

**Ask:** Rename Check in by Project (P)/(V) → Active Registered (P)/(V); City unchanged; keys unchanged.

**Files:** Catalog, UiStrings (+ regenerate), Mock comments, reference.md
## 2026-07-27 — Active Registered (C) first

**Ask:** Rename Check in by City → Active Registered (C); place as first Registration tab (before P/V).

**Files:** Catalog, UiStrings (+ regenerate), Mock comment, reference.md
## 2026-07-27 — Remove Registration ApplicationType tabs

**Ask:** Remove Check-In, Check-Out, Address Change, Passport Change, Check-In (Internal), Check-Out (Internal), Visa Change sub-reports.

**Change:** Catalog keeps only App_Reg_ext (Extension) among ApplicationType tabs. Active Registered / Expiring / To Be Checked* unchanged (population still uses active-reg type names).

**Files:** ReportDashboardCatalog.cs, reference.md
## 2026-07-27 — Registration default chart = bar

**Ask:** Bar chart default for all Registration sub-reports.

**Change:** DefaultChartViewFor uses (Registration, _) => bar (was pie for Active Registered C/P/V).

**Files:** ReportDashboardPropertyEditor.cs

## 2026-07-27 — Active Registered (V) used Application.VisaType

**Symptom:** All Active Registered (V) rows showed WP — Work visa (one chart bucket).

**Cause:** Period · Category · Type was built from registration `Application.VisaPeriod/Category/Type`. Registration apps default/copy WP even when `CurrentVisa.VisaType` is FM (etc.).

**Fix:** Keep Period from Application (Visa has no VisaPeriod); take Category + Type from `ApplicationItem.CurrentVisa`.

**Verify:** Local PG — ~254 WP + ~58 FM in Valid visa only population (was 100% WP via Application).

## 2026-07-27 — Remove Registration Extension sub-report

**Ask:** Remove Extension tab from Registration category.

**Change:** Dropped `RegistrationApplicationTypeSubReports` / `App_Reg_ext` from catalog tabs and loader dispatch. `App_Reg_ext` remains in Active Registered / Expiring State population type list. Removed `ReportDashboard.SubReport.App_Reg_ext` UI string.

## 2026-07-27 — Registration On process sub-report

**Ask:** New Registration tab "On process" (last): all App_Reg_* ApplicationItems whose Application is unfinished (same terminal excludes as Invitation Process); Status = ApplicationType · ProcessState; grain = ApplicationItem.

**Change:** Catalog key `on-process`; EF loader `LoadRegistrationOnProcess` (LatestPrimaryStateCode; StatusListLabel via ResolveInvitationProcessResultStateLabel); mock + localization; pin after To Be Checked Out.

## 2026-07-27 — Remove Valid visa only from Registration

**Ask:** Drop Valid visa only checkbox/filter for Registration category.

**Change:** Removed Registration from `SupportsValidVisaPersonsOnly` (hides chrome + skips `validVisaPersonIds` in loaders).

## 2026-07-27 — Active WorkPermit (P)

**Ask:** New WorkPermit first tab Active WorkPermit (P); rename By Days Remaining → WorkPermit Validity; only (P) twin; same population as Validity; Status = Project; extend one-last-valid toggle; mock-first then SQL.

**Change:**
- Catalog: `active-by-project` first, `by-days-remaining` = WorkPermit Validity, `by-status` kept
- Mock `WorkPermitActiveByProject`; PropertyEditor bar default; localization `WorkPermit.active-by-project` / `WorkPermit.by-days-remaining`
- Real: `LoadWorkPermitActiveByProjectFromView` reuses `vw_rd_work_permit` (Status = Project + AssignCategoricalCss); Hybrid promotes `(WorkPermit, active-by-project)`; Overview default remaps to active-by-project
- `SubReportCountsValidWorkPermits` includes active-by-project

**Files:** Catalog, Mock, QueryService, HybridQueryService, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-27 — vw_rd_work_permit_active for Active WorkPermit (P)

**Ask:** Dedicated SQL view for Active WorkPermit sub-report (not reuse validity view).

**Change:**
- `vw_rd_work_permit_active` (+ postgres): same valid-item population as `vw_rd_work_permit`; `StatusLabel` = Project (Person then sponsor); `(No project)` fallback
- EF `VwRdWorkPermitActive` + DbContext; SqlViewsUpdater + ReportDashboardPostgresViewsUpdater
- `LoadWorkPermitActiveByProjectFromView` queries new view; Validity still uses `vw_rd_work_permit`

**Files:** SqlViews, VwRdWorkPermitActive.cs, Visa2026DbContext, SqlViewsUpdater, ReportDashboardPostgresViewsUpdater, ReportDashboardQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-27 — Active WorkPermit (P) showed validity buckets

**Symptom:** Chart/table Status = Valid (>90 days) / Valid (31-90 days) instead of Project.

**Cause:** `vw_rd_work_permit_active` missing → catch fell through to `LoadWorkPermitLegacy`, which always buckets with `PassportValidityBucket`.

**Fix:** Active loader always Status = Project: primary active view → fallback `vw_rd_work_permit` (Project status) → `LoadWorkPermitActiveByProjectLegacy`; use `IsMissingReportDashboardView` (PG + SQL Server).

**Files:** ReportDashboardQueryService.cs, learnings.md
## 2026-07-27 — WorkPermit Extension (P) mock

**Ask:** WorkPermit Extension (P) like Visa Extension (P); only (P); mock first.

**Agreed rules:**
- Types: `App_WP_Ext` + `App_Visa_and_WP_Ext`; require `CurrentWorkPermitItem`
- Exclude PROCESS_ISSUED / CANCELLED / REJECTED + 1st/2nd Review Rejected
- Status = Project · ProcessState; tab after Active, before Validity

**Change:** Catalog key `on-extension`; mock `WorkPermitOnExtensionByProject`; bar default; localization `WorkPermit.on-extension`; `WorkPermitExtensionApplicationTypeNames` for later SQL. Not in Hybrid RealSubReports yet.

**Files:** Catalog, Mock, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md
## 2026-07-27 — WorkPermit Extension Result (P) mock

**Ask:** Extension Result (P) like Visa; only (P); mock first.

**Agreed rules:**
- Same types + `CurrentWorkPermitItem` as WorkPermit Extension
- Outcomes: PROCESS_ISSUED / CANCELLED / REJECTED + 1_REVIEW_REJECTED / 2_REVIEW_REJECTED
- Status = Project · ProcessState; tab after Extension, before Validity

**Change:** Catalog key `extension-result`; mock `WorkPermitExtensionResultByProject`; `WorkPermitExtensionResultStateCodes`; bar default; localization. Not in Hybrid RealSubReports yet.

**Files:** Catalog, Mock, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md
## 2026-07-27 — vw_rd_work_permit_app_progress for Extension Result (P)

**Ask:** SQL view for WorkPermit Extension Result (P).

**Change:**
- `vw_rd_work_permit_app_progress` (+ postgres): ApplicationItems on `App_WP_Ext` / `App_Visa_and_WP_Ext` with `CurrentWorkPermitItem`; latest progress state
- EF `VwRdWorkPermitAppProgress`; updaters; `LoadWorkPermitExtensionResultFromView` filters Issued/Cancelled/Rejected + 1st/2nd Review Rejected; Status = Project · ProcessState; Hybrid promote `(WorkPermit, extension-result)`
- Shared view ready for WorkPermit Extension (P) later (exclude those codes)

**Files:** SqlViews, VwRdWorkPermitAppProgress.cs, DbContext, SqlViewsUpdater, ReportDashboardPostgresViewsUpdater, QueryService, HybridQueryService, reference.md, IMPLEMENTATION_PLAN.md
## 2026-07-28 — Visa Extension Required (P)/(V) mock

**Ask:** Extension Required (P)/(V) for Visa; mock only.

**Agreed rules:**
- Valid last visa per person not already on unfinished Visa Extension app
- (P) Status = Project; (V) = Period · Category · Type; columns like Active Visa
- Tab order: after Active, before Visa Extension

**Change:** Catalog keys `extension-required` / `extension-required-by-period-category-type`; mock row sets; bar defaults; localization. Not in Hybrid RealSubReports.

**Files:** Catalog, Mock, PropertyEditor, UiStrings.messages.json (+ regenerate), reference.md
## 2026-07-28 — vw_rd_visa_extension_required for Extension Required (P)/(V)

**Ask:** SQL view for Visa Extension Required (P)/(V).

**Change:**
- `vw_rd_visa_extension_required` (+ postgres): last valid visa per person (ExpirationDate DESC); exclude people with unfinished Visa Extension app (`App_Visa_Ext*` / `App_Visa_and_WP_Ext`, latest progress <> PROCESS_ISSUED); Period/Category/Type labels for (V)
- EF `VwRdVisaExtensionRequired`; `LoadVisaExtensionRequiredFromView`; Hybrid promote both keys
- (P) Status = Project; (V) = Period · Category · Type

**Files:** SqlViews, VwRdVisaExtensionRequired.cs, DbContext, SqlViewsUpdater, ReportDashboardPostgresViewsUpdater, QueryService, HybridQueryService, reference.md
## 2026-07-28 — Extension Required empty on Postgres (CurrentVisaId)

**Symptom:** Extension Required (P)/(V) Total Records = 0; Active Visa ~340; local Postgres (`visa2026`).

**Cause:** `vw_rd_visa_extension_required` never created. Postgres DDL used `"CurrentVisaID"` but the column is `"CurrentVisaId"` (same as `vw_rd_visa_app_progress`). Create failed / was skipped; loader fell through to `EmptyPanel`.

**Fix:**
- Correct `"CurrentVisaId"` in `.postgres.sql` + `ReportDashboardPostgresViewsUpdater`
- Created view locally via psql → **342** rows (last valid visa minus unfinished extension people)
- Loader fallback: if view missing, derive from `vw_rd_visa_by_period` + unfinished people from `vw_rd_visa_app_progress`

**Watch-out:** On Postgres always use `ApplicationItems."CurrentVisaId"` (not `CurrentVisaID`). After adding views, create via psql immediately if ModuleInfo already current (updater may not re-run).
## 2026-07-28 — Extension Required Status = exact days remaining

**Ask:** Group Extension Required (P)/(V) by days left; no Project / Period grouping; every distinct day count is its own state; sort fewest days first.

**Change:** Loader Status = `"N days"` from `ExpirationDate - today` (exact); buckets/list ordered by days ascending; table header "Days Remaining"; mock aligned. (P) and (V) share the same Status grain.

**Files:** ReportDashboardQueryService.cs, Catalog, MockQueryService, reference.md
## 2026-07-28 — Extension Required: drop (P)/(V) project tabs

**Ask:** Remove project grouping / (P) for Extension Required; days-remaining status already in place.

**Change:** Single tab `extension-required` labeled "Extension Required"; removed `Extension Required (V)` from catalog; localization without (P)/(V). Legacy V key still loads same panel.

**Files:** Catalog, Hybrid, PropertyEditor, Mock, UiStrings + VisaUiMessageCatalog.g.cs, reference.md
## 2026-07-28 — Extension Required nearest days milestone

**Ask:** Exact day buckets too many; snap to closest match.

**Change:** Status snaps to nearest of `0 · 7 · 14 · 30 · 60 · 90 · 180 · 365` (0 only when remaining is 0; else 7+; tie → lower/more urgent). Bars still urgent-first.

**Files:** ReportDashboardQueryService.cs, MockQueryService, reference.md
## 2026-07-28 — Extension Required preview: exact Days Remaining column

**Ask:** Add DaysRemaining column to Extension Required list (preview table).

**Change:** Headers `Name · Project · Visa # · Expiry · Days Remaining · Status`; `ColumnC` = exact day count; Status remains nearest milestone (chart grouping). Razor renders ColumnC when headers ≥ 6.

**Files:** ReportDashboardModels, Catalog, QueryService, Mock, ReportDashboardComponent.razor
## 2026-07-28 — Days Remaining column on more Visa subreports

**Ask:** Same exact Days Remaining column as Extension Required for Active Visa (P)/(V), Visa Extension (P)/(V), Visa Validity.

**Change:** Headers insert Days Remaining before Status; `ColumnC` from expiry (Active/Validity) or `CurrentVisa.ExpirationDate` (On Extension). Razor already renders ColumnC when headers ≥ 6.

**Files:** Catalog, QueryService, MockQueryService
## 2026-07-28 — Preview table pagination

**Ask:** Paginate dashboard preview list (Prev/Next + page size); table lacked all rows / scroll.

**Change:** `PreviewLimit` raised to 10_000; client-side pager (25/50/100/200) with range label; scrollable sticky-header table body. Page resets on category/sub-report/filter change.

**Files:** ReportDashboardQueryService.cs, ReportDashboardComponent.razor, report-dashboard.css, UiStrings + VisaUiMessageCatalog.g.cs
## 2026-07-28 — Preview actions toolbar + subreport-aware Open ListView/Excel

**Ask:** Keep Total Records in report header; move Open in Excel / Open ListView above the preview table; make actions follow the active sub-report (Visa BO vs VisaExtensionStatus).

**Change:**
- Buttons relocated to `.rd-preview-actions` above the preview table (header keeps Total only).
- `ResolveListViewTarget` / `UsesVisaBoListView`: Active Visa, Extension Required, Visa Validity → `Visa_ListView`; On Extension / Extension Result → `VisaExtensionStatus_ListView`.
- `ExcelTemplateNameHint(category, subReport)` ready for per-subreport templates (Visa still `433_gurlusyk_uzt` until separate seeds).
- `BuildListCriteria(..., subReport)`: Visa path via `Passport.Person`, valid-visa base filter, milestone/validity/project/category·type status windows; extension status parses `Project · State` composites.
- PropertyEditor Excel/ListView handlers use `ComponentModel.SubReport`; panel `ListViewId` from `ResolveListViewTarget`.

**Files:** ReportDashboardComponent.razor, report-dashboard.css, ReportDashboardCatalog.cs, ReportDashboardPropertyEditor.cs, QueryService + Mock BuildPanel, reference.md
## 2026-07-28 — Unify On Extension preview + Open ListView on vw_rd_visa_app_progress

**Ask:** Preview and Open ListView must share the same SQL view; source of truth is dashboard `vw_rd_*` (not View_VisaExtensionStatus).

**Change:**
- Enriched `vw_rd_visa_app_progress` with ExpiringVisaID, PassportID, CurrentStateID, StatusDate, DaysRemainingOnVisa (SQL Server + Postgres + updaters).
- Promoted `VwRdVisaAppProgress` to XAF read-only BO (`NavigationItem(false)`) with FKs; ListView `VwRdVisaAppProgress_ListView` curated columns (hide GUID/raw fields).
- `ResolveListViewTarget` / `BuildListCriteria` for on-extension / extension-result → `VwRdVisaAppProgress` + population filters matching loaders.
- Preview still loads `VwRdVisaAppProgress`; Days Remaining from view column.
- Users role: Read on `VwRdVisaAppProgress`. Nav Ministry1/2 stays on `VisaExtensionStatus`.

**Files:** VwRdVisaAppProgress.cs, SqlViews, SqlViewsUpdater, ReportDashboardPostgresViewsUpdater, DbContext, Catalog, QueryService, Model.xafml, Updater.cs, IMPLEMENTATION_PLAN.md
## 2026-07-28 — Active Visa Open ListView uses vw_rd_visa_by_period (not Visa BO)

**Symptom:** Open ListView from Active Visa (P) opened editable `Visa_ListView` (521 rows) while preview Total was 380 (`vw_rd_visa_by_period` + one-last-valid).

**Change:** Promote `VwRdVisaByPeriod` / `VwRdVisaExtensionRequired` / `VwRdVisaByDaysRemaining` as dashboard ListView BOs. Enrich `vw_rd_visa_by_period` with PassportID, DaysRemaining, IsOneLastValidPerPerson. `ResolveListViewTarget` + `BuildListCriteria(..., oneLastValidVisaPerPerson)` align Open ListView with preview filters.

**Files:** VwRdVisa*.cs, vw_rd_visa_by_period*.sql, updaters, Catalog, PropertyEditor, Model.xafml, Updater, DbContext
## 2026-07-28 — Postgres 42703 DaysRemaining missing on vw_rd_visa_by_period

**Symptom:** `PostgresException 42703: column v.DaysRemaining does not exist` in `LoadVisaActiveByProjectFromView` — EF mapped new columns but live Postgres view was stale (ModuleUpdater skipped while ModuleInfo current).

**Fix:** Reapplied `vw_rd_visa_by_period.postgres.sql` + `vw_rd_visa_app_progress.postgres.sql` to local `visa2026`. Added host-start heal `ReportDashboardPostgresViewsHealSql.ApplyIfMissing` (sentinel column check → recreate from embedded `.postgres.sql`) wired in `Startup.cs` for Postgres.

**Lesson:** After enriching `vw_rd_*` views, either bump ModuleInfo / FORCE_XAF_DB_UPDATE or keep a startup heal — otherwise Demo Postgres keeps the old view definition.
## 2026-07-28 — Skill: Preview ↔ SQL view ↔ XAF ListView contract (all categories)

**Rules:** SQL view = source of truth for Open ListView; one subreport → one `vw_rd_*` + one ListView; ListView columns/Total match Preview; caption = Label; Excel same population. Scope: all Report categories.

**(P)/(V):** shared population via base + thin public wrappers (no hand-duplicated business SQL); still two public views + two ListViews.

**Naming:** prefer key-aligned `vw_rd_{category}_{subreport_key}`.

**Transitional debt:** existing shared Visa ListViews (e.g. Active P/V on `vw_rd_visa_by_period`) noted in SKILL; split when those tabs are reworked — skill update only this pass.

**Files:** SKILL.md, reference.md, IMPLEMENTATION_PLAN.md

---

## 2026-07-28 — Visa per-subreport dedicated BOs wired

**Change:** Wired six new Visa Report Dashboard BOs (Active P/V, On Extension P/V, Extension Result P/V) end-to-end:
- DbContext `DbSet` + `ToView` + FKs (mirror ByPeriod / AppProgress)
- `ResolveListViewTarget` maps each visa subreport key to its dedicated ListView (legacy by-category/by-type/by-period → Active V)
- `BuildListCriteria`: Active* = IsArchived + role + optional IsOneLastValidPerPerson; OnExtension*/ExtensionResult* = IsArchived + role only (population in SQL wrappers); status clicks use `StatusLabel`
- Query loaders read new dedicated views; Status = `StatusLabel` (no sibling joins / no ResolveVisaAppProgressStatusLabels on dedicated paths)
- Officer + Users Read permissions on all 6 types
- Postgres + SQL Server updaters create wrapper views after bases; HealSql recreates missing bases (sentinel) then wrappers (`to_regclass`); csproj embeds new `*.postgres.sql`
- Model.xafml: 6 ListViews with captions = catalog Labels; Extension Required / Visa Validity captions updated; DaysRemaining on Extension Required

**Kept:** Base `VwRdVisaByPeriod` / `VwRdVisaAppProgress` mapped (fallback / sibling heal / extension-required fallback).

**Verify:** `dotnet build Visa2026.slnx -c Debug` — 0 errors. Runtime: restart app / FORCE_XAF_DB_UPDATE or rely on HealSql so wrappers exist before opening Active Visa (P)/(V) / Extension tabs.

**Files:** `Visa2026DbContext.cs`, `ReportDashboardCatalog.cs`, `ReportDashboardQueryService.cs`, `Updater.cs`, `ReportDashboardPostgresViewsUpdater.cs`, `SqlViewsUpdater.cs`, `ReportDashboardPostgresViewsHealSql.cs`, `Visa2026.Module.csproj`, `Model.xafml`.

## 2026-07-28 — Visa category: one subreport → one view → one ListView

**Contract:** Per SKILL Preview ↔ `vw_rd_*` ↔ XAF ListView.

**Change:**
- Base population kept: `vw_rd_visa_by_period`, `vw_rd_visa_app_progress`.
- Public wrappers + BOs + ListViews (caption = Label):
  - Active Visa (P)/(V): `vw_rd_visa_active_by_project` / `…_by_period_category_type`
  - Visa Extension (P)/(V): `vw_rd_visa_on_extension` / `…_by_period_category_type` (excludes PROCESS_ISSUED)
  - Extension Result (P)/(V): `vw_rd_visa_extension_result` / `…_by_period_category_type` (terminal codes)
- Extension Required / Visa Validity: dedicated ListViews already; captions fixed; `DaysRemaining` added to extension-required view.
- Catalog `ResolveListViewTarget` + criteria; loaders read dedicated DbSets; Status from view `StatusLabel` where wired.
- Postgres heal embeds wrappers; local Postgres views applied.

**Verify:** Restart app; for each Visa sub-tab, Preview Total == Open ListView Total; captions match Labels.

## 2026-07-28 — DxGridListEditor AddColumnCore NRE on dashboard ListViews

**Symptom:** Opening Report Dashboard Visa Open ListView → `NullReferenceException` in `DxGridListEditorBase.AddColumnCore`.

**Cause:** `Model.xafml` `ColumnInfo` nodes for members marked `[Browsable(false)]` (FK ids, navigations, StatusCssClass, etc.). XAF does not create `ModelMember` for those → `AddColumnCore` NREs. Same risk when a ListView column targets a navigation that was hidden with `[Browsable(false)]` while `ColumnInfo Id="Person"` remained.

**Fix:**
- Dedicated Visa ListViews: only browsable scalar columns (`PersonName` caption Person, Project, Visa # / App #, dates, Days Remaining, StatusLabel).
- Do **not** emit `Index="-1"` ColumnInfo for non-browsable members.
- ~~Keep Person/Passport/Application navigations `[Browsable(false)]`~~ **Superseded 2026-07-28:** use browsable navigations for DetailView links; hide Preview scalars instead (see later entry + `reference.md` Column contract). Temporary scalar-only columns avoided `AddColumnCore` NRE until navigations were made browsable.
- Also slimmed legacy `VwRdVisaAppProgress_ListView` / `VwRdVisaByPeriod_ListView` (removed Index=-1 browsable-false columns).

**Verify:** Restart F5 (Model.xafml), Open ListView on each Visa sub-tab — no NRE; Preview Total == ListView Total.

**Files:** `Visa2026.Blazor.Server/Model.xafml`, `VwRdVisaActive*` / `OnExtension*` / `ExtensionResult*` / `ExtensionRequired` / `ByDaysRemaining` BOs.

## 2026-07-28 — Visa On Extension rename + terminal population split

**Ask:** Rename Visa Extension (P)/(V) → Visa On Extension (P)/(V). On Extension must not include final Application Process states; `*_REVIEW_REJECTED` counts as final and belongs in Extension Result.

**Change:**
- Catalog / localization / BO / ListView captions → **Visa On Extension (P)/(V)** (keys unchanged).
- On Extension wrappers (`vw_rd_visa_on_extension*`) exclude `PROCESS_ISSUED` / `PROCESS_CANCELLED` / `PROCESS_REJECTED` / `*_REVIEW_REJECTED` (null/empty still in-flight).
- Extension Result wrappers include those terminal codes (incl. `RIGHT(...,16) = '_REVIEW_REJECTED'`).
- Extension Required “unfinished” exclusion uses the same terminal set (so cancelled/rejected people reappear).
- `ApplicationProgressStateCodes.IsTerminalOutcome` + `BuildVisaAppProgressPopulationCriteria` aligned.

**Verify (local PG):** `on_ext(151) + ext_result(2916) = base(3067)`; zero terminal rows in On Extension; zero non-terminal in Extension Result.

**Files:** SqlViews `vw_rd_visa_on_extension*`, `vw_rd_visa_extension_result*`, `vw_rd_visa_extension_required*`; SqlViewsUpdater / PostgresViewsUpdater; Catalog; QueryService; Model.xafml; UiStrings + VisaUiMessageCatalog.g.cs.

## 2026-07-28 — On Extension still showed Cancelled/Issued (LatestPrimaryStateCode)

**Symptom:** After terminal-code filter, Visa On Extension chart still showed `Project · Cancelled` / `Issued`.

**Cause:** `vw_rd_visa_app_progress.ProgressStateCode` used **latest ApplicationProgresses row** `ApplicationStates.Code`, which can lag (e.g. still `PROCESS_STARTED`) while `Applications.LatestPrimaryStateCode` / `LatestProgressDisplay` already say `PROCESS_CANCELLED` / Cancelled. StatusLabel preferred `LatestProgressDisplay`, so UI showed Cancelled while the WHERE on ProgressStateCode missed it.

**Fix:** Prefer `COALESCE(LatestPrimaryStateCode, ast.Code)` for ProgressStateCode (+ CSS); Extension Required unfinished uses `LatestPrimaryStateCode`; heal recreates app-progress dependents when On Extension still has terminal primary codes.

**Verify (local PG):** On Extension **91** all `PROCESS_STARTED`/`Processing`; Result **2976**; `91+2976=3067`; zero Cancelled/Issued labels in On Extension.

## 2026-07-28 — Visa Validity Preview 380 ≠ Open ListView (one-last toggle)

**Symptom:** Visa Validity Preview Total **380** (One last valid visa per person checked) did not match Open ListView.

**Cause:** Preview applied one-last in C# (`TakeOneLastValidVisaPerPerson`). `vw_rd_visa_by_days_remaining` / `VwRdVisaByDaysRemaining` had **no** `IsOneLastValidPerPerson`, and `BuildListCriteria` only applied that flag for Active Visa views — ListView opened all valid visas (**512**).

**Fix:** Add `IsOneLastValidPerPerson` to `vw_rd_visa_by_days_remaining` (same window as by-period); BO + heal sentinel; criteria `(usesVisaActive || usesByDays)`; loader filters `Where(IsOneLastValidPerPerson)`.

**Verify (local PG):** all **512** / one-last **380**.
## 2026-07-28 — Passport column on all Visa category Preview + ListViews

- **Request:** Add Passport to every Visa category Preview table and Open ListView.
- **Approach:** Scalar `PassportNumber` (caption Passport / header Passport #) — not browsable Passport navigation (avoids `AddColumnCore` NRE).
- **SQL:** `PassportNumber` on `vw_rd_visa_by_period`, active wrappers, `by_days_remaining`, `extension_required`, `app_progress` (+ `LEFT JOIN Passports` on `CurrentPassportID`), on_extension / extension_result wrappers. `by_period` PG drop uses `CASCADE`.
- **Preview:** Headers insert Passport # after Project; `ColumnD` for 7-col layouts (Active / Ext Required / Validity / On Extension). Extension Result stays 6-col (Passport # + App # + App Date + Status).
- **ListView:** `PassportNumber` after `ProjectName` on all dedicated Visa `VwRd*` ListViews in `Model.xafml`.
- **Heal:** Sentinel `PassportNumber`; `NeedsVisaPassportNumberHeal` recreates by_period + days + app_progress chains. Embedded `vw_rd_visa_by_days_remaining.postgres.sql`.
- **Verified:** Local PG views reapplied; Active PassportNumber sample rows present; Module build 0 errors.
## 2026-07-28 — Native XAF DetailView links on Visa dashboard ListViews

- **Request:** Click Passport / Visa / Person (any linked object) on Open ListView to open domain DetailView.
- **Approach:** Native reference columns — browsable navigations (`Person`, `Passport`, `Visa`, `Application`), not scalar strings. Preview keeps reading `PersonName` / `PassportNumber` / `VisaNumber` / `ApplicationNumber` (now `[Browsable(false)]`).
- **Visa-row views** (Active, Validity, Extension Required, ByPeriod): added `Visa` nav with FK = row `ID` (view key is Visa.ID); EF `HasOne(...Visa).HasForeignKey(t => t.ID)`.
- **App-progress views** (On Extension, Extension Result): browsable `Application` / `Person` / `Passport`; `ApplicationNumber` hidden.
- **Model.xafml:** `PersonName`→`Person`, `PassportNumber`→`Passport`, `VisaNumber`→`Visa`, `ApplicationNumber`→`Application`. Never `ColumnInfo` on `[Browsable(false)]` (`AddColumnCore` NRE).
- **Verified:** Module build 0 errors. Restart Blazor host to pick up DLL + model.

## 2026-07-28 — Loading feedback: Open ListView + DetailView links

- **Open ListView:** `OnOpenListView` sets `IsLoading` + `LoadingProgressPercent = -1` (indeterminate bar) + `OpeningListView` message, `await Task.Delay(16)` so overlay paints, then `ShowView`. Excel / Open ListView buttons disabled while `ShowLoading`.
- **DetailView from Visa ListViews:** `ReportDashboardListViewOpenFeedbackController` on `VwRdVisa*_ListView` — toast via `ShowMessage` on `ListViewProcessCurrentObjectController.Executing` and on `Application.ViewShowing` (DetailView, SourceFrame = ListView frame) for reference-column navigations. `OpenObjectController` is not in ExpressApp 25.2; ViewShowing covers object links.
- **Loc:** `ReportDashboard.Chrome.OpeningListView`, `OpeningDetail` in messages.json + catalog.
- **Files:** PropertyEditor, Component.razor, report-dashboard.css, Model LoadingProgressPercent comment, new Module controller.
## 2026-07-28 — Reference-column open toast needs Blazor NavigationManager

- **Symptom:** Open ListView overlay worked; clicking Person/Passport/Visa/Application links showed no toast.
- **Cause:** Blazor ListView reference cells are HTML `ShowLink` navigations (`LookupPropertyEditor`/`ObjectPropertyEditor`), not `OpenObjectController` / `ViewShowing` with SourceFrame. ExpressApp 25.2 has no OpenObjectController hook for this path.
- **Fix:** Move feedback to Blazor `ReportDashboardListViewOpenFeedbackController` using `NavigationManager.RegisterLocationChangingHandler` when target path contains `_DetailView`. Keep ProcessCurrentObject.Executing for row activation. Removed Module ViewShowing controller.
## 2026-07-28 — Removed reference DetailView Opening toast (defer)

- Toast via `NavigationManager.LocationChanging` appeared **after** the DetailView was already visible — not useful as loading feedback.
- Removed `Visa2026.Blazor.Server/Controllers/ReportDashboardListViewOpenFeedbackController.cs`. Open ListView overlay kept. Revisit reference-column busy UI later (likely needs earlier/client-side intercept).

## 2026-07-29 — Application (via ministry) Invitation on Process (P) real wire-up

**Ask / population (A):** Same as Invitation Process: `CanIssueInvitation` + `ViaMinistries` + non-terminal + no linked Invitation. Grain = Application header. Chart Status = `Project · StatusListLabel` (ministry-aware in C#). View `StatusLabel` = process alone for ListView.

**Shipped:**
- View `vw_rd_application_via_ministry_invitation_on_process` (SS + PG) + EF `VwRdApplicationViaMinistryInvitationOnProcess`
- Loader + EF fallback; Hybrid `RealSubReports` promote for `invitation-on-process`
- ListView `VwRdApplicationViaMinistryInvitationOnProcess_ListView`; Read permissions; Postgres heal StandaloneViews
- `BuildListCriteria`: skip Application route criteria on dedicated view; split chart Status into ProjectName + StatusLabel (tolerate ministry ` - ` suffix)

**Files:** SqlViews + updaters/heal, BO/DbContext, QueryService/Catalog/Hybrid, Updater, Model.xafml, csproj embed

## 2026-07-29 — Application (via ministry) remaining SQL views (phase 1)

**Ask:** SQL views for the nine remaining via-ministry subreports (not EF/Hybrid yet).

**Design:** Bases + thin `SELECT *` (P)/(V) wrappers; invitation on-process (V) wraps existing (P) and adds Period/Category/Type labels. StatusLabel = process alone. Terminal/non-terminal match invitation-on-process. Visa-ext types: `App_Visa_Ext`, `App_Visa_Ext_According_to_WP`, `App_Visa_Ext_FM`, `App_Visa_and_WP_Ext`. Other = ViaMinistries + not CanIssueInvitation + not those types. Invitation completed = CanIssueInvitation + ViaMinistries + terminal (no “no Invitation” filter).

**Views:** `vw_rd_application_via_ministry_{invitation_on_process_by_period_category_type|invitation_completed[_base|_by_period…]|visa_extension_on_process[_base|_by_period…]|visa_extension_completed[_base|_by_period…]|other_on_process|other_completed}` (SS + PG).

**Register:** SqlViewsUpdater + PostgresViewsUpdater via `ReportDashboardSqlViewResource`; heal `StandaloneViews` (bases before wrappers); csproj embeds `.sql` + `.postgres.sql`.

**Next:** EF BO → loader → ListView → Hybrid promote per subreport.

## 2026-07-29 — Application (via ministry) full wire-up (remaining 9)

**Ask:** Proceed after SQL phase — EF BO → loader → ListView → Hybrid for all via-ministry subreports.

**Shipped:**
- 9 BOs + `IVwRdApplicationViaMinistryRow`; DbContext `ToView`; Read permissions; 9 ListViews in Model.xafml
- Unified `LoadApplicationViaMinistryFromView` (chart P=`Project · StatusListLabel`, V=`Period · Category · Type · StatusListLabel`; completed uses extension-result CSS)
- Catalog `UsesApplicationViaMinistryRdListView` + ResolveListViewTarget + BuildListCriteria (P/V status split)
- Hybrid `RealSubReports` promotes all 10 via-ministry keys

**Files:** BusinessObjects/VwRdApplicationViaMinistry*, QueryService, Catalog, Hybrid, Updater, Model.xafml, learnings/reference

## 2026-07-29 — Application (via ministry): ApplicationItem grain + Position / App Type

**Ask:** Switch all via-ministry subreports from one row per Application (first person) to **one row per ApplicationItem** (every person). Add Employee Position (CurrentPositionHistory → Position NameTm) and Application type (NameTm) to Preview + Excel + ListView.

**Shipped:**
- SQL w_rd_application_via_ministry_* (SS + PG): ID = ApplicationItemOid; columns PositionLabel, ApplicationTypeLabel
- BOs / IVwRdApplicationViaMinistryRow + ListView columns Person · Project · Position · App Type · App # · App Date · State
- Preview/Excel headers: Name, Project, Position, App Type, App #, App Date, Status (chart P=Project · State, V=Period · Category · Type · State)
- Role filter uses row PersonRoleCode; chart ministry lookup uses ApplicationOid
- Legacy invitation-on-process expands ApplicationItems

**Files:** SqlViews, BusinessObjects/VwRdApplicationViaMinistry*, QueryService, Catalog, Mock, Model.xafml, ReportDashboardLocalization + UiStrings / VisaUiMessageCatalog

## 2026-07-29 — Via-ministry Invitation/Visa Extension: Visa Period + Visa Type

**Ask:** Add Application VisaPeriod / VisaType (NameTm) to Invitation + Visa Extension via-ministry subreports (P/V, On Process/Completed) with Preview + Excel + ListView parity. Not Other, not Direct Migration.

**Shipped:**
- SQL VisaPeriodLabel / VisaTypeLabel on invitation + visa-ext views (Other also has columns for unified EF mapping; not shown in UI)
- Preview columns A-F: Position, App Type, Visa Period, Visa Type, App #, App Date (+ Status)
- ListViews: same order; Catalog headers for 8 invitation/visa-ext keys
- UsesApplicationViaMinistryInvitationOrVisaExtListView; ColumnE/F on ReportDashboardPreviewRow

**Files:** SqlViews, BOs, QueryService, Catalog, Mock, Model.xafml, ReportDashboardComponent, localization

## 2026-07-29 — Via-ministry empty tabs: views missing on SQLEXPRESS

**Symptom:** Only Invitation on Process (P) had a Total; other via-ministry subreports showed 0.

**Cause:** App DB `localhost\SQLEXPRESS` / `Visa2026` had no `vw_rd_application_via_ministry_*` views (ModuleInfo current → SqlViewsUpdater skipped). P alone had an EF legacy fallback. Also missing `ApplicationTypes.CanIssue*` columns required by the views.

**Fix:** Created views + `CanIssue*` on SQLEXPRESS. Added `ReportDashboardSqlServerViewsHealSql` + `ApplicationTypeCapabilityFlagsSchemaSql.ApplyIfMissing` at host start; Postgres standalone heal recreates when item-grain sentinels missing.

## 2026-07-29 — Visa Extension Completed: Visa on extension + Issued Visa

**Ask:** On Visa Extension Completed (P)/(V) only, add Visa on extension (`ApplicationItem.CurrentVisa`) and Issued Visa (`Visa.IssuingApplicationItem`) with Preview + Excel + ListView parity (numbers in Preview; navigable Visa objects in ListView).

**Shipped:**
- SQL `VisaOnExtensionOid/Number` + `IssuedVisaOid/Number` on visa-ext completed base (SS joins fixed; PG already had LATERAL)
- BOs + ListViews for Completed P/V only; Catalog 11-column headers; Preview `ColumnE`–`H`; loader maps numbers → App #/Date into G/H
- Localization `Visa on extension` / `Issued Visa`; Postgres heal sentinel `IssuedVisaNumber`

**Note:** Local SQLEXPRESS has ~2978 Visa-on-extension numbers; Issued Visa count is 0 until visas have `IssuingApplicationItemID` populated.

**Files:** SqlViews base SS/PG, completed BOs, QueryService, Catalog, Mock R11, Models ColumnG/H, ReportDashboardComponent, Model.xafml, localization, Postgres heal

## 2026-07-29 — Local app DB is PostgreSQL (not SQLEXPRESS)

**Fact:** Blazor host (`appsettings` / launch profile "Visa2026 - PostgreSQL") uses `Host=localhost;Database=visa2026;EFCoreProvider=Postgres`. Report Dashboard heal at startup is Postgres-only (`ReportDashboardPostgresViewsHealSql`).

**Action:** Applied visa-ext completed base + wrappers on Postgres `visa2026` so `VisaOnExtension*` / `IssuedVisa*` columns exist. Do not treat SQLEXPRESS verification as the app data source for this workstation.


## 2026-07-29 - Preview/chart visual pass: segmented state label, donut, table polish

**Ask:** Make subreport state labels, charts and Preview tables look more professional (reference: multi-segment status label separated by middle dots).

**Shipped (presentation only - no data/parity change):**
- `StateLabel(rawLabel, cssClass, pill)` RenderFragment in `ReportDashboardComponent.razor`: localizes, splits on ` . `, renders leading status dot + muted context segments + colour-weighted trailing segment (`.rd-state-value`). Used by bar / pie legend / list chart and the Preview Status cell.
- CSS `.rd-state` token set (`--rd-state-color/-tint/-edge`): validity families (st-approved/pending/expiring) tint the pill; `st-cat-1..5` colour dot + trailing segment only, neutral pill (a tint per category floods dense tables).
- Pie -> **donut** (`.rd-pie-figure` + radial mask) with total in the centre; bar rows get hover, gradient fills, `count + %`; list rows get a mini bar.
- Preview table: sticky tinted header, zebra + hover rows, `tabular-nums`, bold `.rd-cell-name`, softer `.rd-project-tag`; chart wrapped in `.rd-chart-block` card.

**Gotcha:** capping `.rd-legend-item .rd-state` width made **every** segment ellipsize at once (flex-shrink applies to all children on overflow). Legend pills must size to content and wrap; only `.rd-cell-state` caps width. Context segments use `flex-shrink: 4` vs `1` on the value so the state survives truncation.

**Verified:** `dotnet build` OK; static markup rendered headlessly (Edge `--screenshot`) against the real CSS. Not yet confirmed in a running app session.

**Files:** `ReportDashboardComponent.razor`, `report-dashboard.css`
## 2026-07-29 - Visual pass part 2: overview cards, bucket cap, bar axis, column alignment

**Ask:** Continue the presentation work - bring Overview cards up to the new look, align numeric columns, keep charts readable when a grouping produces many buckets, give bars an axis.

**Shipped (presentation only - no data/parity change):**
- **Overview cards:** mini pie -> wider-hole donut + top-4 legend rows (dot, `StateTail` trailing segment, count, share, `+N more`). Replaced `.rd-overview-mini-bars` (100px labels ellipsized every compound label to nothing). Card is now white with hover lift and a rule under the header.
- **Bucket cap:** `ChartBucketLimit = 12`; `VisibleChartBuckets` caps the **legend / rows only** - the donut keeps drawing every slice so it still totals 100%. `ChartBucketsExpander()` reuses existing `Chrome.MoreProjects` / `Chrome.ShowLess` keys (no new localization). Resets with `_previewContextKey`.
- **Bar axis:** `.rd-bar-axis` shares `--rd-bar-grid` / `--rd-bar-gap` with `.rd-bar-row` so ticks line up with tracks; gridlines are a `background-image` on the track at thirds (visible in the unfilled part).
- **Column alignment:** `ReportDashboardCatalog.EnglishTableHeaders` made **public** - localized captions cannot be matched back to a column meaning. `rd-col-num` (right, nowrap) for Days Remaining / Days Since Entry / Grad Year, `rd-col-date` (nowrap) for date headers.

**Gotchas:**
- `ColumnClass` must be **cached** (`RebuildColumnClasses` in `OnParametersSet`): the switch expression allocates a new array, and it is called per cell (200 rows x 11 cols). Also skip alignment when catalog header count != panel header count.
- Late `.rd-legend-dot.st-cat-N, .rd-status-dot.st-cat-N, .rd-bar-row-fill.st-cat-N { background: <hex> }` rules silently **overrode** the new categorical bar gradients (same specificity, later in file). Dropped `.rd-bar-row-fill` from those selectors; keep bar fill colours in the bar section only.

**Verified:** `dotnet build` OK, no lints; markup rendered headlessly (Edge `--screenshot`) against the real CSS. Not yet confirmed in a running app session.

**Files:** `ReportDashboardComponent.razor`, `report-dashboard.css`, `ReportDashboardCatalog.cs`
## 2026-07-29 - Overview card labels: trailing segment is NOT the distinguishing part

**Symptom (from a real Overview screenshot, after the part-2 visual pass):**
- **Application (via ministry)** card listed four legend rows all reading **"Processing"** (68 / 14 / 2 / 1).
- **Application (direct migration)** card listed rows labelled **"-"** (em dash).

**Cause:** the card legend used `StateTail` (last ` . ` segment) to fit one short line per bucket.
- Via-ministry (P) groups by `Project . StatusListLabel` -> every tail is the same state.
- `FormatApplicationCombinedStateLabel` builds `State . depth . leg . migration` and pads unset legs with a literal `"-"` -> the tail is a placeholder.

**Rule (do not reintroduce a blind tail):** `OverviewBucketLabels(buckets)` drops placeholder segments (`- / – / -`), then uses tails **only when they are non-empty and distinct across the visible buckets**; otherwise it falls back to the full (placeholder-stripped) label. Validity / single-segment cards keep the short form; project-grouped cards get the full label truncated with the whole string in the row `title`.

**Also:** sub-report chips capped at `OverviewSubChipLimit = 6` + `+N more` chip - the via-ministry card's ten chips stretched it far taller than the rest of the grid row. Donut trimmed 68 -> 58px and gap 14 -> 12px to buy label width.

**Note (not changed):** the Preview table / chart pills still render the raw `... . - . -` padding for direct migration. Stripping placeholders there is a display change beyond styling - confirm with the user before doing it.

**Build note:** `dotnet build` fails with MSB3021/MSB3027 DLL copy locks while the Blazor host is running; `error CS` count is the signal that actually matters.

**Files:** `ReportDashboardComponent.razor`, `report-dashboard.css`
## 2026-07-29 - Theme-following palette (dark mode) + placeholder segments stripped

**Ask:** strip the `-` padding segments from status labels everywhere; add dark-theme support.

**Placeholders:** `DisplayStateSegments()` (used by `StateLabel` and `OverviewBucketLabels`) drops `- / – / -` segments, but falls back to the raw segments when a status is *only* placeholders so a cell never renders empty. Full untouched string stays in the `title`.

**Dark theme - how the switch is detected (important):**
DevExpress swaps a **whole stylesheet** per theme; there is **no** `dxbl-mode-dark` class and XAF does not reliably set `data-bs-theme` - verified against the 25.2.5 packages:
- `themes.fluent/.../bootstrap/fluent-dark.bs5.min.css` -> `:root,[data-bs-theme=light]{--bs-body-bg:#282828;--bs-body-color:#fff}` (the dark file redefines **:root itself**)
- `fluent-light.bs5.min.css` -> `#fff` / `#161616`; `blazing-dark.bs5.min.css` -> `#37353d` / `#fff`
So **`var(--bs-body-bg)` / `var(--bs-body-color)` are the only trustworthy signals.** `--bs-secondary-bg` / `--bs-tertiary-bg` / `--bs-border-color` stay **light** in the dark themes - do **not** build on them.

**Consequence - there is NO dark override block.** The whole palette is *derived* in `.report-dashboard`:
- `--rd-surface: var(--bs-body-bg)` / `--rd-text: var(--bs-body-color)`
- neutrals = `color-mix(in srgb, var(--rd-surface) N%, var(--rd-text))` (surface stepped toward text -> darker on light, lighter on dark, one declaration)
- status **tints/edges** = `color-mix(hue N%, var(--rd-surface))`
- status **text/dots** = `color-mix(hue 70%, var(--rd-text))` - pushes toward the foreground, so contrast rises in **both** themes (also lifted the light theme past 4.5:1, which the raw hues were just missing)
- `--rd-on-accent: var(--rd-surface)` - white on dark-blue chip in light, dark on light-blue chip in dark

**Gotchas:**
- **Chart fills must use the raw `--rd-hue-*`, not the contrast-pushed token.** Pushing `--rd-amber` toward the dark text colour turned light-theme bars olive/muddy. Fills keep full saturation; only text and dots get the push.
- **Shadows stay black rgba.** `color-mix(... var(--rd-text) ...)` for a shadow glows **white** on a dark theme.
- Prerequisite: every rule had to be tokenized first (~40 hex literals removed). Only the donut `mask` `#000` and black drop-shadows remain literal, both theme-neutral by design.

**Verified:** `dotnet build` clean (0 CS/RZ); rendered headlessly under `--bs-body-bg:#ffffff` and `#282828` - overview cards, donut, bar axis/gridlines, list rows, preview table and state pills all correct in both. Not yet confirmed in a running app session.

**Files:** `ReportDashboardComponent.razor`, `report-dashboard.css`
---

## Invitation Completed (P)/(V) - "Invitation #" proof-of-issue column (2026-07-29)

**Ask:** show, on each Invitation Completed row, the invitation that the application process actually produced.

**The relationship is `Application.Invitations` -> `InvitationItems.Person` - NOT `ApplicationItem.CurrentInvitationItem`.**
`CurrentInvitationItem` is *input* data (the person's pre-existing invitation fed into the application); the issued
invitation hangs off the parent `Application` via `Invitation.Application` (optional FK, inverse `Application.Invitations`).
Physical: `Invitations.ApplicationID` / `InvitationItems.InvitationID` / `InvitationItems.PersonID`.
`Invitation.IssuedDate` is stored in column **`StartDate`** (`[Column("StartDate")]`) - order by that, not `IssuedDate`.

**Person-precise match matters - measured, not assumed.** A preview row is one `ApplicationItem` (one person), so the
join is filtered by `ii.PersonID = ai.PersonID`. On the local dev DB (5661 rows) this left **179 PROCESS_ISSUED rows blank
where the application *does* have an invitation the person is simply not on** - invitations routinely cover a subset
(sampled applications: 50 people -> 32 invited, 27 -> 17, 16 -> 12). An application-level join would have falsely told
the officer those 179 people had an invitation. Blank is the correct answer and is the point of a proof column.

**"Completed" does not imply "issued".** Unlike `..._invitation_on_process` (defined by
`NOT EXISTS (SELECT 1 FROM Invitations WHERE ApplicationID = a.ID)`), the completed base view has **no** invitation join
at all - it is "type CanIssueInvitation + terminal progress state", which includes `PROCESS_REJECTED` / `PROCESS_CANCELLED`.
Measured: REJECTED 169 rows / 0 invitations, CANCELLED 319 / 9, ISSUED 5173 / 4994. Blanks are expected, not a bug.

**Row fan-out guard.** Business rule is one invitation per ApplicationItem, but the join still uses `OUTER APPLY ... TOP 1`
(SS) / `LEFT JOIN LATERAL ... LIMIT 1` (PG). A plain join would multiply rows on any data anomaly and silently break the
Preview/ListView total parity gate. Verified after the change: 5661 rows / 5661 distinct `ApplicationItemOid`.

**Postgres heal is the easy step to miss.** `invitation_completed*` lives in `StandaloneViews`, which only heals when the
relation is **missing** - on an existing DB the stale view would survive and EF would throw "column InvitationNumber does
not exist". Fix mirrors the `IssuedVisaNumber` precedent: add a sentinel check to `NeedsViaMinistryStandaloneHeal`, which
re-runs the whole ordered list (bases before wrappers). Use the **(P)** wrapper name for the sentinel - Postgres truncates
identifiers at 63 chars and the (V) name becomes `..._invitation_completed_by_period_c`.

**Header reuse over a new key.** `"Invitation #"` already exists in `ReportDashboardLocalization.Header()`; adding a new
`"Invitation"` key would have shipped an untranslated header in tk/ru/tr. "Completed" already implies issued, so the
`"Issued Visa"`-style naming was unnecessary.

**Column plumbing (the full chain, all of it required):**
base view x2 (SS + PG; the four wrappers are `SELECT *` and need no edit) -> `InvitationOid` / `Invitation` nav /
`InvitationNumber` on both read-only BOs (Oid + scalar `[Browsable(false)]`, nav is the ListView column) ->
`AppViaMinistryRow` gains a slot (positional record: all 10 call sites updated, 8 pass `null`) -> `showInvitationCol`
in the preview mapping -> catalog headers split out of the shared on-process arms -> `R10` mock helper ->
both `Model.xafml` ListViews (Index 6, shift App #/Date/State to 7/8/9). Preview renderer is header-count driven
(`TableHeaders.Count >= 10` renders `ColumnG`), so 10 headers map cleanly onto Name..ColumnG + Status. Excel needs no
work - "Open in Excel" exports the ListView.

**Verified:** `dotnet build` clean (0 CS/RZ, 0 lint); all three PG views applied to local `visa2026` and queried;
column at ordinal 16 between `VisaTypeLabel` and `ApplicationNumber`; counts above. SQL Server view updated for parity
but not executed (repo is Postgres-only at runtime). Not yet confirmed in a running app session.

**Files:** `vw_rd_application_via_ministry_invitation_completed_base.sql` (+ `.postgres.sql`),
`VwRdApplicationViaMinistryInvitationCompleted.cs` (+ `...ByPeriodCategoryType.cs`), `ReportDashboardQueryService.cs`,
`ReportDashboardCatalog.cs`, `ReportDashboardMockQueryService.cs`, `ReportDashboardPostgresViewsHealSql.cs`, `Model.xafml`

## 2026-07-29 — Application (direct migration): On Process (A) + Process Complete (mock)

**Ask:** Replace Application Status with On Process (A) and Process Complete. Chart Status = Application Type · ApplicationProgress StatusListLabel. Preview adds App Type column. Grain = ApplicationItem. Process Complete = terminal states only.

**Keys:** `on-process-a`, `process-complete` (legacy `app-status` remaps to On Process (A)).

**Hybrid:** Direct migration panels/sub-report list now use mock (Registration stays real).

**Files:** Catalog, MockQueryService, HybridQueryService, UiStrings.messages.json, VisaUiMessageCatalog.g.cs, reference.md

## 2026-07-29 — Application (direct migration): SQL views On Process (A) / Process Complete

**Ask:** Wire real SQL views for the two mock tabs.

**Views:** `vw_rd_application_direct_migration_on_process_a`, `vw_rd_application_direct_migration_process_complete` (SS + PG). Filter `ApplicationProgressRoute = 1`; grain ApplicationItem; On Process = non-terminal; Complete = terminal.

**Local PG counts:** on-process ~99, complete ~11669.

**Shipped:** BOs + ListViews + QueryService loader + Hybrid RealSubReports + permissions + Postgres heal/updater + csproj embeds.

**Files:** SqlViews, BOs, DbContext, Catalog, QueryService, Hybrid, Updater, SqlViewsUpdater, Postgres updater/heal, Model.xafml, Module.csproj, reference.md

## 2026-07-29 — Direct migration Project from Person.ProjectContract

**Symptom:** On Process (A) / Process Complete showed `(No project)` for every row.

**Cause:** Views joined `Application.ProjectContract`; direct-migration apps typically leave that unset.

**Fix:** Resolve project from `Person.ProjectContract`, else sponsoring employee`s project (same as other person-grain RD views).

**Verify:** on-process 99/99 with project (was 0).


## 2026-07-29 — Direct migration ListView: hide Application Item

**Symptom:** Open ListView showed an extra **Application Item** column (`Person - App#`) between Project and App Type; Preview did not.

**Cause:** `ApplicationItem` navigation was browsable; XAF auto-added it despite Model column list.

**Fix:** `[Browsable(false)]` on both Direct Migration RD BOs; Model `ColumnInfo Id=""ApplicationItem"" Index=""-1""`.

## 2026-07-29 — Direct migration ListView NRE from Index=-1 ColumnInfo

**Symptom:** Open ListView NullReferenceException in `DxGridListEditorBase.AddColumnCore`.

**Cause:** Model ColumnInfo for `ApplicationItem` after property was `[Browsable(false)]` — member unresolved.

**Fix:** Remove ApplicationItem ColumnInfo nodes; keep Browsable(false) only.
## 2026-07-29 — Direct migration On Process (A): Preview Total vs Open ListView

**Symptom:** On Process (A) badge/chart Total **97** did not match Open ListView Total (e.g. **33**).

**Cause:** Preview applied a silent **9-month** `ApplicationDate` cutoff (`ResolveDateRangeMonths` default) while Application categories have **no Last-N UI** and `BuildListCriteria` has **no** `ApplicationDate` clause. Local PG: view 99; within 9mo 97. ListView **33** matched Employees + `ApplicationDate <= today` (person-type / date filter skew on drill-down).

**Fix:** `IsApplicationCategory` → `ResolveDateRangeMonths` returns **0**; `LoadPanel` treats `dateRangeMonths <= 0` as `cutoff = DateTime.MinValue` so Preview population matches ListView (`IsArchived` + person/project only).

**Verify:** Restart; All + On Process (A): Preview Total == Open ListView Total (~99 non-archived). Same person-type tab when opening ListView.

## 2026-07-29 — Incomplete persons category + Person Mark incomplete

**Ask:** Soft incomplete flag on Person (notes + missing-area checkboxes); DetailView Mark incomplete / Mark complete; Report Dashboard category Incomplete persons with one sub-report grouped by missing-area (chart counts each flag; Preview one row per person).

**Shipped:**
- Person fields: `IsDataIncomplete`, nine `IncompleteMissing*` flags, Notes, MarkedOn/By; read-only on DetailView (Appearance when complete); actions via `PersonIncompleteDataController` + `PersonIncompleteMarkOptions` popup
- Dashboard: `IncompletePersons` / `by-missing-area` → `vw_rd_incomplete_persons_by_missing_area` + `VwRdIncompletePersonsByMissingArea` + ListView; Hybrid Real; chart buckets from flags (Total = person count)
- Soft flag only (no application gate); Mark complete clears all

**Verify:** Module + Blazor.Server Debug build 0 errors. Runtime: restart app so EF adds People columns + SQL view updater/heal runs.

**Files:** Person*.cs, PersonIncomplete*, ReportDashboard* Catalog/Mock/Query/Hybrid/Models, SqlViews/vw_rd_incomplete_persons_by_missing_area*.sql, Updater permissions, Model.xafml ListView + Employee IncompleteData layout, UiStrings

## 2026-07-29 — Incomplete persons PG heal: pc.Name missing

**Symptom:** Startup heal `42703: column pc.Name does not exist` on `vw_rd_incomplete_persons_by_missing_area`.

**Cause:** `ProjectContracts` has `NameTm` only (no `Name`); view copied wrong COALESCE pattern.

**Fix:** Use `NameTm` only in `.sql` and `.postgres.sql`.

## 2026-07-29 — Incomplete persons heal blocked on missing People columns

**Symptom:** `42703: column p.IncompleteMissingPersonalData does not exist` during Startup view heal.

**Cause:** ModuleInfo current → EF skips schema add; heal CREATE VIEW ran before People incomplete columns existed.

**Fix:** `PersonIncompleteDataSchemaSql.ApplyIfMissing` in Startup before heal; ModuleUpdater; incomplete view healed separately only when People columns exist.

## 2026-07-29 — Person Incomplete data DetailView tab

**Ask:** Show incomplete fields on Person DetailView in a separate tab (not main form / not ListView).

**Shipped:** `IncompleteData` LayoutGroup as last tab in `PersonRecordTabs` on Employee / FamilyMember / TemporaryVisitor typed DetailViews. Appearance `PersonIncompleteTab_HideWhenComplete` hides tab when `IsDataIncomplete = False`. Fields remain read-only; set via Mark incomplete actions.

**Verify:** Model.xafml has three `Id="IncompleteData"` groups under typed Person DetailViews.

## 2026-07-29 — Incomplete persons: remove Valid visa / Include archived toggles

**Ask:** Remove "Valid visa only" and "Include archived" checkboxes from Incomplete persons (they hid flagged people without a valid visa).

**Fix:** Drop `IncompletePersons` from `SupportsValidVisaPersonsOnly` and `SupportsIncludeArchivedPersons`. `LoadIncompletePersons` no longer filters by visa person IDs or archived — shows all `IsDataIncomplete` rows (person type + project only).

**Verify:** Rebuild/restart; Incomplete persons chrome has no those two checkboxes; Preview lists incomplete persons regardless of visa.

## 2026-07-29 — Report Dashboard localization refresh (placeholders, Incomplete, Excel, ListView)

**Ask:** Re-review Layer A localization after catalog/UI drift; implement gaps.

**Shipped:**
- Status placeholders `(No period)` / `(No type)` / `(No status)` + multi-segment `Status()` (localize every ` · ` part; comma lists for Incomplete missing areas when any segment is keyed)
- Incomplete chart/preview: area labels + person-role English keys mapped; Preview Person type / Missing areas localize only for IncompletePersons
- Excel toasts: `Chrome.ExcelNotConfiguredBody` / `Chrome.ExcelTemplateMissing`
- Open ListView: caption = localized SubReport Label; column captions via `Header()` on English Model captions
- `ReportDashboardHost` class caption in `UiStrings.json` → localization xafml
- Registration sub-report EN `On Process` (casing)

**Watch-outs:** Do not run `Status()` on Project/ColumnA for all categories — keys like `Education`/`Passport` collide with project names. Keep English status keys in loaders for ListView criteria.

**Files:** UiStrings.messages.json, ReportDashboardLocalization, Catalog, PropertyEditor, Component.razor, UiStrings.json, generated VisaUiMessageCatalog + Model.*.xafml

## 2026-07-29 — Turkmen: işjeň → aktiw

**Ask:** Prefer Turkmen "aktiw" instead of "işjeň" in localization.

**Change:** Replaced `işjeň`/`Işjeň` (and `işjeňleşdir` → `aktiwleşdir`) across Layer A JSON sources; regenerated message catalog + tk-TM xafml.

**Files:** UiStrings*.json (messages, entities, security, person-detail, documents-views, lookup-enums), Module UiStrings.json, generated outputs

## 2026-07-30 - Person search category (free-text) + row click opens dossier

**Ask:** Officers search a person (name / personal number / passport #), pick a result row, and land on the read-only person dossier for company directors.

**Shipped:**
- `vw_rd_person_search` (`.sql` + `.postgres.sql`) - one row per Person; current passport/visa via `OUTER APPLY` / `LEFT JOIN LATERAL` over non-cancelled rows; `SearchText` = lowercased name parts + personal number + **all** passport numbers
- `VwRdPersonSearch` BO + `ToView` + `Updater` read permission + PG updater/heal + csproj embed
- Catalog: `PersonSearch` category, single sub-report `by-name`, headers Name/Project/Personal number/Passport #/Visa expiry/Status, `PersonSearchTokens` + `BuildPersonSearchCriteria`
- `searchTerm` plumbed through `IReportDashboardQueryService` + all three implementers; Hybrid promoted to Real
- UI: `.rd-search-box` in the filter chrome (gated by `SupportsPersonSearch`), `.rd-row-clickable` rows -> `PersonSelected` -> `PersonDossierOpenHelper`

**Key decisions:**
- **Empty term lists everything** (person-type + project filters only) so the Overview card shows a real count instead of 0
- **Chart buckets = current visa status** (Valid / Expiring <30 / Expired / No visa) - a search still owes the dashboard a chart, and this reuses existing status CSS
- Rows open the **dossier**, not `Person_DetailView` - the category exists for the director hand-over story

**Parity gate (verified):** term `akku` -> Preview total 7, chart 5 Valid + 2 Expired, ListView `Total: 7`, identical six columns. Empty term -> 3333 / 332 Valid / 54 Expiring / 1579 Expired / 1368 No visa. Same tokenizer feeds Preview and ListView criteria - do not let the two drift.

**Watch-outs:**
- `ProjectContracts` has **`NameTm` only** (no `Name`) - same trap as the Incomplete persons view
- Selenium: `Close all tabs` also closes the dashboard; re-click the **Report Dashboard** nav item before asserting on dashboard DOM
- New/Delete stay visible on the drill-down ListView despite `AllowNew/AllowDelete=False` - pre-existing on every `vw_rd_*` ListView, not specific to this one

**Files:** SqlViews/vw_rd_person_search*.sql, VwRdPersonSearch.cs, Visa2026DbContext, SqlViewsUpdater, ReportDashboardPostgresViewsUpdater/HealSql, Updater.cs, ReportDashboardCatalog/Models/Query/Hybrid/Mock, ReportDashboardPropertyEditor/Model/Component.razor, report-dashboard.css, Model.xafml, UiStrings.messages.json
## 2026-07-30 - Person search: stuck "Opening dossier… 0%" after closing dossier

**Symptom:** Click a Person search row → dossier opens → close dossier tab → Report Dashboard shows a permanent overlay "Opening dossier…" at 0%.

**Root cause:** `OnRowSelected` called `BeginLocalLoadingAsync` but never cleared `_localLoading`. Category/Search clear it indirectly when the parent sets `IsLoading` (see `OnParametersSet`). `PersonSelected` only calls `ShowView` and never flips parent `IsLoading`, so the MDI-kept dashboard tab woke up still loading.

**Fix:** `try` / `finally { _localLoading = false; }` around `PersonSelected.InvokeAsync`.

**Prevent:** Any `BeginLocalLoadingAsync` whose callback does **not** drive parent `IsLoading` must clear `_localLoading` itself.

**Files:** `ReportDashboardComponent.razor`
## 2026-07-30 - Hide PROJECT chips + person-type tabs (deprecated)

**Ask:** Simplify Report Dashboard UI; hide the two filter rows (PROJECT chips and All/Employees/Family/Temporary tabs); mark deprecated.

**Fix:** ReportDashboardCatalog.ShowProjectAndPersonTypeFilters = false gates both rows in ReportDashboardComponent.razor. Filters stay locked to All/All. Documented in docs/DEPRECATED.md + docs/REPORT_DASHBOARD.md. Flip the const to true to restore.

**Files:** ReportDashboardCatalog.cs, ReportDashboardComponent.razor, DEPRECATED.md, REPORT_DASHBOARD.md

## 2026-07-30 - Person search diacritic fold

**Ask:** Less restrictive search so ASCII typed letters match accented names (e.g. `u` matches `ü`, `gul` matches `Gül`).

**Approach:** Fold both sides — `PersonSearchTextNormalizer.Fold` on query tokens (`PersonSearchTokens`) and SQL `translate` on `vw_rd_person_search.SearchText` after `lower`. Keep AND + Preview/ListView parity.

**Pitfalls:**
- Postgres `E'\u00e0...'` does **not** decode Unicode escapes — use `U&'\00E0...'` (or literal UTF-8).
- `SqlFoldFrom` / `SqlFoldTo` lengths must match; `ýÿñç` maps to `yync` (four chars), not `ync`.
- Heal previously only created the person-search view when missing — existing DBs need `NeedsPersonSearchFoldHeal` (`pg_get_viewdef` lacks `translate(`) to recreate.

**Files:** PersonSearchTextNormalizer.cs, ReportDashboardCatalog.cs, vw_rd_person_search*.sql, ReportDashboardPostgresViewsHealSql.cs, REPORT_DASHBOARD.md, PersonSearchTextNormalizerTests.cs

## 2026-07-30 — Incomplete persons tk-TM category label

**Ask:** Rename Turkmen Report Dashboard category from "Doly däl şahslar" to "Maglumary doly däl şahslar".

**Fix:** `ReportDashboard.Category.IncompletePersons` `tk-TM` in `UiStrings.messages.json`; regenerate `VisaUiMessageCatalog.g.cs`. Sidebar + overview card use `ReportDashboardLocalization.Category`.

## 2026-07-30 — Work-permit dashboard views missing on Postgres (heal gap)

**Symptom:** `42P01: relation "vw_rd_work_permit_active" does not exist` when opening Work Permit dashboard; EF logs `fail` even though loader has fallback.

**Cause:** View created only in `ReportDashboardPostgresViewsUpdater` (ModuleUpdater). Startup heal (`ReportDashboardPostgresViewsHealSql`) had no work-permit entries; `.postgres.sql` files were not embedded resources.

**Fix:** Embed `vw_rd_work_permit*.postgres.sql`; `HealWorkPermitViewsIfNeeded` recreates missing `vw_rd_work_permit`, `vw_rd_work_permit_active`, `vw_rd_work_permit_app_progress` at host start.

**Verify:** Restart app (Postgres); `\dv vw_rd_work_permit*` shows three views; Work Permit → Active (P) loads without 42P01.

**Files:** Visa2026.Module.csproj, ReportDashboardPostgresViewsHealSql.cs

## 2026-07-30 — Incomplete persons category labels (tr/en/ru)

**Ask:** Align category wording with tk-TM “data incomplete persons”: tr `Verileri eksik kişiler`; update en/ru too.

**Fix:** `ReportDashboard.Category.IncompletePersons` → en `Persons with incomplete data`, tr `Verileri eksik kişiler`, ru `Лица с неполными данными`; regenerate catalog.

## 2026-07-31 — Report Dashboard nav is top-level (not under Home)

**Ask:** Place Report Dashboard navigation item as top-level, not nested under Home.

**Change:**
- `ReportDashboardModelUpdater` adds `ReportDashboard` on root `NavigationItems` (Index -100, startup item); removes legacy `Home/ReportDashboard` and empty `Home`
- Role nav permissions: `Application/NavigationItems/Items/ReportDashboard` (dropped `Items/Home` + nested path)
- Loc: `UiStrings.json` `navigation.ReportDashboard`; DesignedDiffs localization xafml leaf at root
- Docs: `REPORT_DASHBOARD.md`, `reference.md`, baseline note

**Files:** `ReportDashboardModelUpdater.cs`, `Updater.cs`, `UiStrings.json`, `Model.DesignedDiffs.Localization.*.xafml`, `docs/REPORT_DASHBOARD.md`, `reference.md`
