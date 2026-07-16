# Report Dashboard — Learnings (append-only)

Date format: `YYYY-MM-DD`

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

**Change:** ReportDashboardComponent.razor — vertical d-cat-tab shows label only (no count badge). Removed unused .rd-cat-tab-count CSS.

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
