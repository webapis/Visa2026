---
name: visa2026-report-dashboard
description: >-
  Develop and evolve the Visa2026 Report Dashboard: add categories, sub-reports,
  wire mock to real SQL views, XAF ListView + Preview Table parity per subreport,
  style status buckets, and tune the Overview card grid. Use when the user asks
  to add a dashboard report, wire real data, create or rename a vw_rd_* view,
  Open ListView / Preview totals or columns, or fix dashboard layout.
---

# Visa2026 Report Dashboard

Canonical doc: `docs/REPORT_DASHBOARD.md`
Experience log: `learnings.md` (read before starting, append after every verified change).
File map, ListView/Preview contract detail: `reference.md`.
SQL view implementation plan and status tracker: `IMPLEMENTATION_PLAN.md`.

---

## Preview ↔ SQL view ↔ XAF ListView (required — all categories)

Applies to **every** Report Dashboard category and subreport from day one (not Visa-only).

```
Preview table  ←── same SQL view / same EF BO ──→  XAF ListView
                 (source of truth for population)
Open in Excel  ←── same population + same filters ──┘
```

### Non-negotiable rules

1. **SQL view is the source of truth** for that subreport’s row population and officer-facing fields. Do **not** open editable domain ListViews (`Visa_ListView`, `VisaExtensionStatus_ListView`, etc.) as the dashboard drill-down.
2. **One subreport → one dedicated `vw_rd_*` SQL view → one dedicated XAF ListView** (read-only BO + `*_ListView`). Catalog key aligns with view/BO naming (see Naming below).
3. **Parity gate:** ListView **visible columns** and **Total** must match the Preview table for the same subreport under the same filters (person type, project chip, archived / one-last-valid / other dashboard toggles).
4. **ListView caption** = catalog subreport **Label** (e.g. `Active Visa (P)`), not a shared “Dashboard” caption.
5. **Open in Excel** uses the **same population** as Preview / ListView (same view + same filters).

### (P) vs (V) when Totals are identical (chart axis only)

Do **not** duplicate business SQL twice by hand.

**Preferred pattern:**

- One **base** view (or shared SQL script fragment) owns the population once.
- Each subreport still gets its **own public** `vw_rd_*` name for XAF `ToView` — either a thin wrapper (`SELECT * FROM base` plus a different `StatusLabel` expression) or two wrappers over the same base.
- Each subreport still gets its **own** XAF ListView (caption = Label).

Only chart regrouping (Status) may differ between (P) and (V); **Total** and identity of rows stay the same.

### Naming (preferred)

Key-aligned, category-prefixed:

| Piece | Pattern | Example |
|-------|---------|---------|
| SQL view | `vw_rd_{category}_{subreport-key-with-underscores}` | `vw_rd_visa_active_by_project` |
| EF / XAF BO | `VwRd{Category}{PascalSubReport}` | `VwRdVisaActiveByProject` |
| ListView Id | `{BoName}_ListView` | `VwRdVisaActiveByProject_ListView` |

Short legacy names (`vw_rd_visa_by_period`) may remain until a subreport is split to the new contract; new work uses key-aligned names.

### What stays app-side (not a second view)

Dashboard toggles (include archived, one last valid visa/permit, completed/cancelled processes, valid-visa-persons-only, date range where applicable) filter the **same** subreport view. Do not create a view per toggle combination.

### Promotion checklist (per subreport)

1. SQL view (SS + Postgres) — population + columns for Preview/`TableHeaders`
2. EF BO mapped `ToView` + DbContext + permissions (Read)
3. Preview loader reads **that** view only
4. Dedicated ListView in `Model.xafml` — caption = Label; columns match `TableHeaders`
5. ListView object columns = **browsable navigations** (Person/Passport/Visa/Application…); Preview scalars stay `[Browsable(false)]` — see `reference.md` Column contract
6. `ResolveListViewTarget` → that ListView; `BuildListCriteria` matches loader filters
7. **Verify:** Preview Total == ListView Total; column set matches; Excel same population; object cells open domain DetailViews
8. Postgres: extend startup heal / updater when columns change (ModuleInfo may skip updaters)

### Transitional debt (existing code)

Some Visa (and other) subreports still **share** one view / one ListView across tabs (e.g. Active Visa (P)/(V) → `vw_rd_visa_by_period` / `VwRdVisaByPeriod_ListView`). That is **not** yet compliant with “one subreport → one view → one ListView.”

- Skill documents the **target** contract for all categories.
- Do **not** expand shared ListView patterns.
- When touching a non-compliant subreport (or when implementing remaining Visa ListViews), **split** to dedicated view + ListView per the rules above.
- Until split, Preview ↔ shared ListView parity still applies for that shared surface.

---

## Architecture

```
IReportDashboardQueryService
  LoadSnapshot()  ->  ReportDashboardSnapshot   (category counts, project chips)
  LoadPanel()     ->  ReportDashboardPanelData  (chart buckets + preview rows)
        |
        v
ReportDashboardPropertyEditor  (XAF Blazor property editor)
        |
        v
ReportDashboardComponent.razor  (bar / pie / list chart, sub-tabs, Overview grid)
```

The component never queries the database. All data comes through the service interface.

---

## Mock to real data workflow

| Phase | Where to work | Gate |
|-------|--------------|------|
| 0 - Prototype | `ReportDashboardMockQueryService.cs` | UX agreed with user |
| 1 - SQL view | `Visa2026.Module/SqlViews/vw_rd_{category}_{subreport}.sql` (+ `.postgres.sql`) | View returns correct rows |
| 2 - EF entity | read-only BO + `Visa2026DbContext` `ToView` | Compiles; ListView creatable |
| 3 - Real loader | `ReportDashboardQueryService` reads **that** view | Preview Total/columns correct |
| 4 - ListView | `Model.xafml` + `ResolveListViewTarget` + criteria | Parity gate (Total + columns) |
| 5 - Switch | Hybrid `RealSubReports` promote | Verified in UI |

Prefer `ReportDashboardHybridQueryService`: register concrete Mock + Real, bind interface to Hybrid. Promote via `RealSubReports` as `(category, subReport)` pairs. Keep mock registered until all sub-reports are verified.

See `IMPLEMENTATION_PLAN.md` for status tracker (update when views split).

---

## Adding a new category (checklist)

1. `ReportDashboardCategory` enum - add new member
2. `ReportDashboardCatalog.cs` - add arms for:
   - `CategoryLabel()`
   - `SubReports()` - list with Key + Label (use Key="default" if only one sub-report)
   - `DefaultSubReport()` - first key
   - `TableHeaders()` - one arm per sub-report key (**column contract** for Preview + ListView)
   - `ResolveListViewTarget` / dedicated ListView per subreport (not a generic domain ListView)
   - `ExcelTemplateNameHint()` - empty string until template is seeded
3. `ReportDashboardMockQueryService.LoadPanel` - add dispatch arm + private mock method
4. `ReportDashboardQueryService.LoadSnapshot` - add `CountCategory(...)` call
5. `ReportDashboardQueryService.LoadPanel` - add dispatch arm calling loader for **that** subreport view
6. Dedicated `vw_rd_*` + BO + ListView per subreport (see contract above)

---

## Adding a sub-report to an existing category

1. `ReportDashboardCatalog.SubReports()` - add `new() { Key = "my-key", Label = "My Label" }`
2. `ReportDashboardCatalog.TableHeaders()` - add arm for `(category, "my-key")`
3. New SQL view + EF BO + ListView (caption = Label) — do **not** reuse another subreport’s view as the Open ListView target
4. `ReportDashboardMockQueryService.LoadPanel` - add dispatch arm + private method
5. `ReportDashboardQueryService.LoadPanel` - loader for that view
6. Parity gate: Preview Total/columns == ListView; Excel same population

---

## SQL view convention

**One public view per subreport** (see Preview ↔ ListView contract). Shared base/wrapper SQL is allowed only to avoid duplicated logic when (P)/(V) share population.

File location: `Visa2026.Module/SqlViews/vw_rd_{category}_{subreport}.sql` (+ `.postgres.sql`)

EF mapping pattern:
```csharp
public DbSet<VwRdVisaActiveByProject> VwRdVisaActiveByProject => Set<VwRdVisaActiveByProject>();
// OnModelCreating:
modelBuilder.Entity<VwRdVisaActiveByProject>().ToView("vw_rd_visa_active_by_project");
// Prefer keyed read-only BO when Open ListView needs stable Oid / FKs
```

---

## Status CSS classes

| Class | Color | Use for |
|-------|-------|---------|
| `st-approved` | Green `#2f8f4e` | Valid / approved / active |
| `st-pending` | Amber `#c1841f` | In progress / pending / soon |
| `st-expiring` | Red `#c05a3a` | Expiring / expired / rejected |
| `st-cat-1` | Blue `#3b7cc9` | Categorical group 1 |
| `st-cat-2..5` | Green/Amber/Purple/Red | Categorical groups 2-5 |
| (no class) | Gray `#8896a8` | Unknown / unclassified |

Use `st-approved/pending/expiring` for validity states.
Use `st-cat-1..5` for categorical groupings (type, citizenship, region).

---

## Service registration

`Visa2026.Blazor.Server/Startup.cs`:

```csharp
// Prototype (mock):
services.AddScoped<IReportDashboardQueryService, ReportDashboardMockQueryService>();

// Production (real) / Hybrid — prefer Hybrid while promoting:
services.AddScoped<IReportDashboardQueryService, ReportDashboardHybridQueryService>();
```

Postgres view drift: `ReportDashboardPostgresViewsHealSql.ApplyIfMissing` (startup) when ModuleUpdater is skipped.

---

## Key files

See `reference.md` for the complete file map and ListView/Preview detail.
See `IMPLEMENTATION_PLAN.md` for the SQL view specifications and promotion status.