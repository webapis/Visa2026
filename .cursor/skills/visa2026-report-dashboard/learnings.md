# Report Dashboard — Learnings (append-only)

Date format: `YYYY-MM-DD`

---

## 2026-07-17 — Application Status: default Bar Chart + equal label/bar width

**Change:** `DefaultChartViewFor(Application, _)` → `bar` (was pie). Bar row grid `140px 1fr 34px` → `1fr 1fr 48px` so label and bar track share width equally; label text `nowrap` + ellipsis with `title` for overflow.

**Why:** Application Status combined labels (state · location · depth · profile) were wrapping in a 140px column and hard to scan.

**Files:** `ReportDashboardPropertyEditor.cs`, `report-dashboard.css`, `ReportDashboardComponent.razor`.

---
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
- Catalog: eady-by-project label "Ready By Project"; new eady-by-period "Ready By VisaPeriod"
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
