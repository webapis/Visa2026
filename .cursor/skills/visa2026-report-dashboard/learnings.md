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
