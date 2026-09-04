# Report Dashboard — Reference

## Complete file map

### Visa2026.Module

| File | Role |
|------|------|
| `Services/ReportDashboard/ReportDashboardModels.cs` | All DTOs and enums: `ReportDashboardCategory`, `ReportDashboardPersonType`, `ReportDashboardPreviewRow`, `ReportDashboardPanelData`, `ReportDashboardSnapshot`, `ReportDashboardSubReport`, `ReportDashboardStatusBucket`, `ReportDashboardProjectChip` |
| `Services/ReportDashboard/ReportDashboardCatalog.cs` | Static catalog: `CategoryLabel`, `SubReports`, `DefaultSubReport`, `TableHeaders`, `ResolveListViewTarget` / `UsesVisaAppProgressListView` / `UsesVisaBoListView`, `ExcelTemplateNameHint(category, subReport)`, `BuildListCriteria(..., subReport)`, `ToPersonRole`, `Categories` list |
| `Services/ReportDashboard/IReportDashboardQueryService.cs` | Interface: `LoadSnapshot(objectSpace, months)` and `LoadPanel(objectSpace, personType, category, projectKey, months, subReport)` |
| `Services/ReportDashboard/ReportDashboardMockQueryService.cs` | Prototype implementation with hardcoded rows. Add new category/sub-report mock data here first. |
| `Services/ReportDashboard/ReportDashboardQueryService.cs` | Real EF implementation. One `Load[Category]()` private method per category. |
| `BusinessObjects/ReportDashboard/ReportDashboardHost.cs` | Non-persistent XAF host object. No changes needed for new reports. |
| `Controllers/ReportDashboardNavigationController.cs` | Navigates to the Dashboard detail view on startup. |
| `DatabaseUpdate/ReportDashboardDetailViewUpdater.cs` | Hides the property label in the XAF DetailView. |
| `DatabaseUpdate/ReportDashboardModelUpdater.cs` | Registers the Dashboard DetailView in the XAF model. |
| `Editors/ReportDashboardEditorAliases.cs` | String constant `Dashboard` for the editor alias. |
| `SqlViews/` | One `.postgres.sql` **per subreport** (`vw_rd_{category}_{subreport}`); shared base/wrapper allowed for (P)/(V). PostgreSQL only — the SQL Server `.sql` twins and `SqlViewsUpdater` are removed. Roster-bodied views are placeholders redirected by `ReportDashboardSqlViewResource.Load` to `ReportDashboardPostgresRosterSql`. |

### Visa2026.Blazor.Server

| File | Role |
|------|------|
| `Editors/ReportDashboardModel.cs` | `ComponentModelBase` subclass. Properties: `PersonType`, `Category`, `SubReport`, `ProjectKey`, `ChartView`, `DateRangeMonths`, `ShowAllView`, `Panel`, `AllPanels`, `Snapshot`, plus EventCallbacks for each. |
| `Editors/ReportDashboardPropertyEditor.cs` | `BlazorPropertyEditorBase` + `IComplexViewItem`. Creates the model, calls `Refresh()` on every user action. `Refresh()` loads `AllPanels` (overview) or single `Panel` (detail). |
| `Editors/ReportDashboardComponent.razor` | UI: overview grid, category nav, sub-report tabs, bar/pie/list chart, preview table, project chips, period picker. |
| `wwwroot/css/report-dashboard.css` | All styles. Key sections: `.rd-overview-grid`, `.rd-overview-card`, `.rd-overview-mini-chart`, `.rd-bar-rows`, `.rd-pie-wrap`, `.rd-sub-tabs`, `.rd-cat-nav`. |
| `Startup.cs` | Service registration. Swap mock/real here. |

---


---

## Preview ↔ SQL view ↔ XAF ListView contract (all categories)

Canonical short form: `SKILL.md` § Preview ↔ SQL view ↔ XAF ListView.

### Rules (verbatim intent)

- SQL view created for a subreport is the **source of truth** for that subreport’s XAF ListView.
- Each subreport has its **own** SQL view and **own** XAF ListView.
- ListView **columns** and **total returned items** must match that subreport’s Preview table (same filters).
- ListView **caption** = subreport Label.
- Open in Excel = **same population** as Preview / ListView.

### Column contract

`ReportDashboardCatalog.TableHeaders(category, subReport)` defines the officer-facing column set.

- Preview table headers must match those labels (order matters for UX).
- XAF ListView layout columns must match the same set (hide raw GUIDs / helper fields used only for criteria or FKs).
- **DetailView links (native XAF):** Open ListView columns for domain objects must be **browsable navigations** (`Person`, `Passport`, `Visa`, `Application`, …), not scalar strings. XAF only renders clickable object links for reference properties.
  - Preview loaders keep reading scalars (`PersonName`, `PassportNumber`, `VisaNumber`, `ApplicationNumber`) — mark those `[Browsable(false)]` so they do not appear as ListView columns.
  - ListView `ColumnInfo` must point at the navigations (`Person` / `Passport` / `Visa` / `Application`), which **replace** the matching Preview text column (same meaning).
  - Never put `ColumnInfo` on a `[Browsable(false)]` member (`DxGridListEditor.AddColumnCore` NRE — no `ModelMember`).
  - When the view row key **is** the domain object (e.g. Active/Validity/Extension Required row `ID` = `Visa.ID`), expose `Visa` with FK = `ID` and wire EF `HasOne(...Visa).HasForeignKey(t => t.ID)`.
  - Do **not** invent custom hyperlink editors for this; use navigations.

### Population / Total parity

Same inputs must yield the same Total:

| Input | Must match |
|-------|------------|
| Person type tab | Yes |
| Project chip | Yes |
| Subreport key | Yes |
| One last valid visa/permit (when shown) | Yes |
| Include archived / process flags / valid-visa-only (when shown) | Yes |

Charts may regroup rows via `Status` / `StatusCssClass`; they must **not** change Total relative to Preview/ListView.

### (P)/(V) shared population

When two subreports differ only by chart axis (Status) but share the same people/rows:

1. Implement shared population once (base view or shared SQL fragment).
2. Still create **two public** views with key-aligned names (thin wrappers / different `StatusLabel` expression as needed).
3. Still create **two** ListViews (captions = each Label).

### Forbidden Open ListView targets

Do not use editable/domain ListViews as dashboard drill-down for a promoted subreport:

- `Visa_ListView`, `VisaExtensionStatus_ListView`, `ApplicationItem_ListView`, etc. — unless the category is still mock and explicitly temporary.

### Wiring

- `ResolveListViewTarget(category, subReport)` → dedicated `*_ListView` + BO type for that subreport.
- `BuildListCriteria(...)` must mirror the preview loader filters for that subreport (bake fixed population filters into the view when possible so ListView criteria cannot drift).
- Permissions: Users/officer roles need **Read** on each dashboard BO.

### Transitional debt

Shared Visa surfaces (`vw_rd_visa_by_period` for Active P+V, `vw_rd_visa_app_progress` for Extension + Result, etc.) are **legacy sharing**. New work and any rework of those tabs must split to one view + one ListView per subreport.

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
| `ApplicationViaMinistry` | `invitation-on-process` / `…-by-period-category-type`, `visa-extension-on-process` / `…-V`, `other-on-process`, `invitation-completed` / `…-V`, `visa-extension-completed` / `…-V`, `other-completed` | **Real:** all 10 tabs via `vw_rd_application_via_ministry_*` + dedicated ListViews; Status=`Project · StatusListLabel` / (V) Period·Category·Type·State. |
| `ApplicationDirectMigration` | `on-process-a` (On Process (A)), `process-complete` (Process Complete); legacy `app-status` → On Process (A) | **Real:** `vw_rd_application_direct_migration_*` + dedicated ListViews; item grain; Status = Application Type · StatusListLabel; Completed = terminal |
| `VisaExtension` (displayed as "Visa") | `active-by-project`, `by-period-category-type`, `extension-required`, `on-extension`, `on-extension-by-period-category-type`, `by-days-remaining`, `extension-result`, `extension-result-by-period-category-type` | Real: active + validity (`vw_rd_visa_by_*`); Extension Required (single tab, Status = nearest days milestone 0/7/14/30/60/90/180/365); Visa On Extension + Extension Result (P)/(V) (`vw_rd_visa_app_progress`). Legacy `visa-state` → active-by-project. |
| `Invitation` | `ready-by-project` (Active Invitation (P)), `ready-by-period-category` (Active Invitation (V)), `in-process` / `in-process-by-period-category-type` (Invitation Process (P)/(V)), `process-result` / `process-result-by-period-category-type` (Process Result (P)/(V); legacy `rejected-by-*`), `used` / `used-by-period-category-type` (Used (P)/(V)), `valid-until` (Invitation Validity) | Real: all invitation tabs. Process Result = CanIssueInvitation apps with terminal progress (Issued/Cancelled/Rejected + 1st/2nd Review Rejected); Status like Extension Result |
| `Registration` | `check-in-by-city` (Active Registered (C)), `check-in-by-project` / `check-in-by-period-category-type` (Active Registered (P)/(V)), `expiring-state`, `to-be-checked-in` / `to-be-checked-out`, `on-process` (On process) | Real: `vw_rd_registration` (+ to-be-checked views); On process = App_Reg_* ApplicationItems not terminal (Issued/Cancelled/Rejected/review rejects), Status = ApplicationType · ProcessState. Cancel/expiry ignored until Check-Out for check-in population. ApplicationType tabs removed |
| `WorkPermit` | `active-by-project` (Active WorkPermit (P)), `on-extension` (Extension (P) — mock), `extension-result` (Extension Result (P) — real), `by-days-remaining` (WorkPermit Validity), `by-status` | Real: active (`vw_rd_work_permit_active`); Result (`vw_rd_work_permit_app_progress`); validity (`vw_rd_work_permit`); Extension mock; by-status mock/legacy |
| `Travel` | `default` | Mock only |
| `BorderZone` | `default` | Mock only |
| `Passport` | `by-type`, `by-citizenship`, `by-validity` | Mock only |

Update this table in `learnings.md` as categories are promoted. Target: one `vw_rd_*` + ListView per subreport (see Preview `↔` ListView contract). Shared Visa views are transitional debt.
## Localization

- Helper: `Visa2026.Module/Localization/ReportDashboardLocalization.cs`
- Messages: `ReportDashboard.*` in `tools/GenerateModelLocalization/UiStrings.messages.json`
- Nav: `navigation.ReportDashboard` in `Visa2026.Module/Localization/UiStrings.json` (top-level; not under Home)
- Display-only status map: keep English keys for ListView; see `docs/REPORT_DASHBOARD.md` § Localization