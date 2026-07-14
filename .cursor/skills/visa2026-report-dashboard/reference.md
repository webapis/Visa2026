# Report Dashboard — Reference

## Complete file map

### Visa2026.Module

| File | Role |
|------|------|
| `Services/ReportDashboard/ReportDashboardModels.cs` | All DTOs and enums: `ReportDashboardCategory`, `ReportDashboardPersonType`, `ReportDashboardPreviewRow`, `ReportDashboardPanelData`, `ReportDashboardSnapshot`, `ReportDashboardSubReport`, `ReportDashboardStatusBucket`, `ReportDashboardProjectChip` |
| `Services/ReportDashboard/ReportDashboardCatalog.cs` | Static catalog: `CategoryLabel`, `SubReports`, `DefaultSubReport`, `TableHeaders`, `ListViewId`, `ExcelTemplateNameHint`, `BuildListCriteria`, `ToPersonRole`, `Categories` list |
| `Services/ReportDashboard/IReportDashboardQueryService.cs` | Interface: `LoadSnapshot(objectSpace, months)` and `LoadPanel(objectSpace, personType, category, projectKey, months, subReport)` |
| `Services/ReportDashboard/ReportDashboardMockQueryService.cs` | Prototype implementation with hardcoded rows. Add new category/sub-report mock data here first. |
| `Services/ReportDashboard/ReportDashboardQueryService.cs` | Real EF implementation. One `Load[Category]()` private method per category. |
| `BusinessObjects/ReportDashboard/ReportDashboardHost.cs` | Non-persistent XAF host object. No changes needed for new reports. |
| `Controllers/ReportDashboardNavigationController.cs` | Navigates to the Dashboard detail view on startup. |
| `DatabaseUpdate/ReportDashboardDetailViewUpdater.cs` | Hides the property label in the XAF DetailView. |
| `DatabaseUpdate/ReportDashboardModelUpdater.cs` | Registers the Dashboard DetailView in the XAF model. |
| `Editors/ReportDashboardEditorAliases.cs` | String constant `Dashboard` for the editor alias. |
| `SqlViews/` | (planned) One `.sql` file per category view. |

### Visa2026.Blazor.Server

| File | Role |
|------|------|
| `Editors/ReportDashboardModel.cs` | `ComponentModelBase` subclass. Properties: `PersonType`, `Category`, `SubReport`, `ProjectKey`, `ChartView`, `DateRangeMonths`, `ShowAllView`, `Panel`, `AllPanels`, `Snapshot`, plus EventCallbacks for each. |
| `Editors/ReportDashboardPropertyEditor.cs` | `BlazorPropertyEditorBase` + `IComplexViewItem`. Creates the model, calls `Refresh()` on every user action. `Refresh()` loads `AllPanels` (overview) or single `Panel` (detail). |
| `Editors/ReportDashboardComponent.razor` | UI: overview grid, category nav, sub-report tabs, bar/pie/list chart, preview table, project chips, period picker. |
| `wwwroot/css/report-dashboard.css` | All styles. Key sections: `.rd-overview-grid`, `.rd-overview-card`, `.rd-overview-mini-chart`, `.rd-bar-rows`, `.rd-pie-wrap`, `.rd-sub-tabs`, `.rd-cat-nav`. |
| `Startup.cs` | Service registration. Swap mock/real here. |

---

## ReportDashboardPreviewRow fields

```csharp
public Guid?   RecordId       // person or document Oid
public string  Name           // person full name
public string  Project        // project contract name
public string  ColumnA        // category-specific (e.g. visa number, invitation number)
public string  ColumnB        // category-specific (e.g. expiry date)
public string  Status         // chart grouping label (e.g. "Expiring (<30 days)")
public string  StatusCssClass // st-approved | st-pending | st-expiring | st-cat-1..5
```

The `Status` field is what drives chart buckets. The Razor component groups rows by `Status` to build bar/pie/list chart data. Make sure mock and real data set `Status` to the **display label**, not a code.

---

## Mock service pattern

```csharp
// In LoadPanel switch:
(ReportDashboardCategory.MyCategory, "my-sub") => Build(personType, category, subReport, MySubRows(), projectKey),
(ReportDashboardCategory.MyCategory, _)        => Build(personType, category, subReport, MyDefaultRows(), projectKey),

// Row helper shorthand (R is defined in the mock service):
private static List<ReportDashboardPreviewRow> MyDefaultRows() =>
[
    R("Full Name", "Project Name", "ColA value", "ColB value", "Status Label", "st-approved"),
    R("Full Name", "Project Name", "ColA value", "ColB value", "Status Label", "st-pending"),
];
```

Status label must exactly match what you want shown in the chart. Group similar statuses under the same label string.

---

## Real query service pattern

```csharp
private static ReportDashboardPanelData LoadMyCategory(
    IObjectSpace objectSpace, PersonRecordRole role, string projectKey,
    ReportDashboardPersonType personType, string subReport,
    string? excelHint, bool excelConfigured, DateTime cutoff)
{
    // Query the EF keyless view entity:
    var dbContext = ((EFCoreObjectSpace)objectSpace).DbContext as Visa2026DbContext
        ?? throw new InvalidOperationException("DbContext unavailable");

    var query = dbContext.VwRdMyCategory
        .Where(r => r.RecordDate >= cutoff);

    if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        query = query.Where(r => r.ProjectName == projectKey);

    // Filter by sub-report:
    if (subReport != "default")
        query = query.Where(r => r.SubReportKey == subReport);

    var rows = query
        .Take(PreviewLimit)
        .AsEnumerable()
        .Select(r => new ReportDashboardPreviewRow
        {
            Name           = r.PersonName ?? string.Empty,
            Project        = r.ProjectName ?? string.Empty,
            ColumnA        = r.ColumnA ?? string.Empty,
            ColumnB        = r.ColumnB ?? string.Empty,
            Status         = r.StatusLabel ?? string.Empty,
            StatusCssClass = r.StatusCssClass ?? string.Empty
        })
        .ToList();

    return BuildPanel(personType, ReportDashboardCategory.MyCategory, subReport,
        rows, excelHint, excelConfigured);
}
```

---

## SQL view template

```sql
-- Visa2026.Module/SqlViews/vw_rd_my_category.sql
CREATE OR ALTER VIEW vw_rd_my_category AS
SELECT
    p.Oid                               AS PersonOid,
    p.FullName                          AS PersonName,
    COALESCE(pc.Name, pc.NameTm, '')    AS ProjectName,

    -- ColumnA: document identifier
    doc.DocumentNumber                  AS ColumnA,

    -- ColumnB: key date formatted
    FORMAT(doc.ExpirationDate, 'MMM dd, yyyy') AS ColumnB,

    -- SubReportKey: which sub-report this row belongs to
    CASE
        WHEN doc.SomeFlag = 1 THEN 'sub-a'
        ELSE 'sub-b'
    END                                 AS SubReportKey,

    -- StatusLabel: human-readable bucket for chart grouping
    CASE
        WHEN doc.ExpirationDate IS NULL                          THEN 'Pending'
        WHEN doc.ExpirationDate  < GETDATE()                    THEN 'Expired'
        WHEN doc.ExpirationDate <= DATEADD(day,  30, GETDATE()) THEN 'Expiring (<30 days)'
        WHEN doc.ExpirationDate <= DATEADD(day,  60, GETDATE()) THEN 'Expiring (<60 days)'
        WHEN doc.ExpirationDate <= DATEADD(day,  90, GETDATE()) THEN 'Expiring (<90 days)'
        ELSE 'Valid'
    END                                 AS StatusLabel,

    -- StatusCssClass: colour token
    CASE
        WHEN doc.ExpirationDate IS NULL                          THEN 'st-pending'
        WHEN doc.ExpirationDate  < GETDATE()                    THEN 'st-expiring'
        WHEN doc.ExpirationDate <= DATEADD(day,  30, GETDATE()) THEN 'st-expiring'
        WHEN doc.ExpirationDate <= DATEADD(day,  90, GETDATE()) THEN 'st-pending'
        ELSE 'st-approved'
    END                                 AS StatusCssClass,

    -- RecordDate: used for date-range cutoff filter
    COALESCE(doc.ExpirationDate, doc.CreatedOn) AS RecordDate

FROM PersonRecord pr
JOIN Person p ON p.Oid = pr.PersonOid
LEFT JOIN ProjectContract pc ON pc.Oid = pr.ProjectContractOid
JOIN SomeDocument doc ON doc.PersonRecordOid = pr.Oid
WHERE pr.GCRecord IS NULL
  AND doc.GCRecord IS NULL;
```

---

## EF keyless entity template

```csharp
// In Visa2026.Module/BusinessObjects/ (or a new SqlViews/ folder)
public class VwRdMyCategory
{
    public Guid?   PersonOid       { get; set; }
    public string? PersonName      { get; set; }
    public string? ProjectName     { get; set; }
    public string? ColumnA         { get; set; }
    public string? ColumnB         { get; set; }
    public string? SubReportKey    { get; set; }
    public string? StatusLabel     { get; set; }
    public string? StatusCssClass  { get; set; }
    public DateTime? RecordDate    { get; set; }
}

// In Visa2026DbContext.OnModelCreating:
modelBuilder.Entity<VwRdMyCategory>().HasNoKey().ToView("vw_rd_my_category");

// In Visa2026DbContext:
public DbSet<VwRdMyCategory> VwRdMyCategory => Set<VwRdMyCategory>();
```

---

## Current category / sub-report registry

| Category | Sub-report keys | Status |
|----------|----------------|--------|
| `VisaExtension` (displayed as "Visa") | `visa-state`, `app-progress`, `by-category`, `by-period` | Mock only |
| `Invitation` | `issued-inv`, `app-progress` | Mock only |
| `Registration` | `default` | Mock only |
| `WorkPermit` | `default` | Mock only |
| `Travel` | `default` | Mock only |
| `BorderZone` | `default` | Mock only |
| `Passport` | `by-type`, `by-citizenship`, `by-validity` | Mock only |

Update this table in `learnings.md` as categories are promoted to real SQL views.