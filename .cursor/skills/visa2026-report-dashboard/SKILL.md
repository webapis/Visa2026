---
name: visa2026-report-dashboard
description: >-
  Develop and evolve the Visa2026 Report Dashboard: add categories, sub-reports,
  wire mock to real SQL views, style status buckets, and tune the Overview card grid.
  Use when the user asks to add a dashboard report, wire real data to the dashboard,
  create a SQL view for a dashboard category, change chart appearance, or fix
  dashboard layout.
---

# Visa2026 Report Dashboard

Canonical doc: `docs/REPORT_DASHBOARD.md`
Experience log: `learnings.md` (read before starting, append after every verified change).
File map and SQL view schema: `reference.md`.

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
| 1 - SQL view | `Visa2026.Module/SqlViews/vw_rd_[category].sql` | View returns correct rows |
| 2 - EF entity | keyless entity + `Visa2026DbContext` mapping | `DbContext.VwRd[Category]` compiles |
| 3 - Real loader | `ReportDashboardQueryService.Load[Category]()` | Parity with mock output |
| 4 - Switch | `Startup.cs`: swap `MockQueryService` to `ReportDashboardQueryService` | One category at a time |

Keep `ReportDashboardMockQueryService` registered until ALL categories have real views. Swap per category by checking `subReport` in the real service and falling back to mock rows for unfinished sub-reports.

---

## Adding a new category (checklist)

1. `ReportDashboardCategory` enum - add new member
2. `ReportDashboardCatalog.cs` - add arms for:
   - `CategoryLabel()`
   - `SubReports()` - list with Key + Label (use Key="default" if only one sub-report)
   - `DefaultSubReport()` - first key
   - `TableHeaders()` - one arm per sub-report key
   - `ListViewId()` - existing XAF ListView ID
   - `ExcelTemplateNameHint()` - empty string until template is seeded
3. `ReportDashboardMockQueryService.LoadPanel` - add dispatch arm + private mock method
4. `ReportDashboardQueryService.LoadSnapshot` - add `CountCategory(...)` call
5. `ReportDashboardQueryService.LoadPanel` - add dispatch arm calling `Load[Category]()`

---

## Adding a sub-report to an existing category

1. `ReportDashboardCatalog.SubReports()` - add `new() { Key = "my-key", Label = "My Label" }`
2. `ReportDashboardCatalog.TableHeaders()` - add arm for `(category, "my-key")`
3. `ReportDashboardMockQueryService.LoadPanel` - add dispatch arm + private method
4. `ReportDashboardQueryService.LoadPanel` - add dispatch arm (filter SQL view by SubReportKey)

---

## SQL view convention

One view per category. File: `Visa2026.Module/SqlViews/vw_rd_[category].sql`

Category view names:
- `vw_rd_visa`
- `vw_rd_invitation`
- `vw_rd_registration`
- `vw_rd_work_permit`
- `vw_rd_travel`
- `vw_rd_border_zone`
- `vw_rd_passport`

### Required columns

| Column | Type | Maps to |
|--------|------|---------|
| `PersonOid` | uniqueidentifier | `ReportDashboardPreviewRow.RecordId` |
| `PersonName` | nvarchar | `.Name` |
| `ProjectName` | nvarchar | `.Project` |
| `ColumnA` | nvarchar | `.ColumnA` (category-specific label) |
| `ColumnB` | nvarchar | `.ColumnB` (category-specific label) |
| `StatusLabel` | nvarchar | `.Status` (chart grouping key) |
| `StatusCssClass` | nvarchar | `.StatusCssClass` |
| `SubReportKey` | nvarchar | filter by `subReport` parameter |
| `RecordDate` | datetime2 | cutoff filter (`>= @cutoff`) |

### Bucket CASE pattern (in the view)

```sql
CASE
  WHEN ExpirationDate IS NULL                          THEN 'Pending'
  WHEN ExpirationDate  < GETDATE()                    THEN 'Expired'
  WHEN ExpirationDate <= DATEADD(day, 30, GETDATE())  THEN 'Expiring (<30 days)'
  WHEN ExpirationDate <= DATEADD(day, 90, GETDATE())  THEN 'Expiring Soon'
  ELSE 'Valid'
END AS StatusLabel,
CASE
  WHEN ExpirationDate IS NULL                          THEN 'st-pending'
  WHEN ExpirationDate  < GETDATE()                    THEN 'st-expiring'
  WHEN ExpirationDate <= DATEADD(day, 30, GETDATE())  THEN 'st-expiring'
  WHEN ExpirationDate <= DATEADD(day, 90, GETDATE())  THEN 'st-pending'
  ELSE 'st-approved'
END AS StatusCssClass
```

### EF mapping

```csharp
// In Visa2026DbContext:
public DbSet<VwRdVisa> VwRdVisa => Set<VwRdVisa>();

// In OnModelCreating:
modelBuilder.Entity<VwRdVisa>().HasNoKey().ToView("vw_rd_visa");
```

The keyless entity class mirrors the view columns exactly.

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
Use `st-cat-1..5` for categorical groupings (type, citizenship, region) where color represents identity, not urgency.

---

## Service registration

`Visa2026.Blazor.Server/Startup.cs` line:

```csharp
// Prototype (mock):
services.AddScoped<IReportDashboardQueryService, ReportDashboardMockQueryService>();

// Production (real):
services.AddScoped<IReportDashboardQueryService, ReportDashboardQueryService>();
```

Swap when all categories have working SQL views.

---

## Key files

See `reference.md` for the complete file map with class names and line count hints.