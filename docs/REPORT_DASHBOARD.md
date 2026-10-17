# Report Dashboard (home)

> **Canonical** officer home page for visa-related business intelligence.  
> Supersedes the removed **State Dashboard** (`StateDashboardComponent` / state-tile UI).  
> Prototype: repo-root [`tab_controls.html`](../tab_controls.html).

## Purpose

After login, officers land on this dashboard to grasp process state (invitation, visa extension, work permit, registration, travel, border zone, passport), then act via:

- **Charts** (list / pie / bar) of status buckets
- **Open in Excel** — invitation tabs use a company workbook (`ReportDashboardExcelTemplate`); other categories use a mapped `UserReportTemplate` when one is configured
- **Open ListView** — filtered XAF ListView matching the current selection (and chart segment when clicked)

## Information architecture

| Layer | Values |
|-------|--------|
| Project filter | **Deprecated (hidden):** `All` + `ProjectContract` chips. Locked to `All` while `ReportDashboardCatalog.ShowProjectAndPersonTypeFilters` is `false`. |
| Person type | **Deprecated (hidden):** All / Employees / Family Members / Temporary Visitors. Locked to `All` while the same flag is `false`. |
| Overview period pickers | **Removed** from Overview (Education / Passport / Position / Address / Medical · Last). Defaults remain for queries; per-category **Last** pickers on Education/Passport/etc. detail views are unchanged. |
| Refresh | Top-right **Refresh** on the dashboard card — always visible for **Overview and every category**; reloads snapshot / panels from the database. |
| Category | Application (via ministry) / Application (direct migration) / Visa / Invitation / Registration / WorkPermit / Travel / Address of Residence / BorderZone / Passport / Education / Position History / Subcontractor / Medical Records / Incomplete persons / Person search |

## Hosting

| Piece | Location |
|-------|----------|
| Host BO | `ReportDashboardHost` (non-persistent) |
| Blazor UI | `ReportDashboardComponent` + property editor |
| Navigation | Root **Report Dashboard** folder (startup item; not under Home). The folder opens Overview and lists each category. The in-page category column is removed. Sub-report tabs stay in the page. |
| Queries | `IReportDashboardQueryService` |


## Localization (Layer A)

Officer UI strings (chrome, category/sub-report/person-type/table headers, fixed validity/bucket labels, Report Dashboard nav) use **`VisaUiMessages`** via **`ReportDashboardLocalization`**.

| Source | Role |
|--------|------|
| `tools/GenerateModelLocalization/UiStrings.messages.json` (`ReportDashboard.*`) | Runtime message catalog |
| `Visa2026.Module/Localization/UiStrings.json` (`navigation.ReportDashboard`) | Model nav captions |
| `ReportDashboardLocalization.cs` | Resolve labels; `Status(english)` for display-only bucket text |

**Invariant:** SQL / loaders keep English status keys; Razor localizes at render so Open ListView criteria still match. Lookup/`NameTm` segments are not translated (Layer B).

`ReportDashboardLocalization.Status` localizes exact fixed labels and each recognized ` · ` segment (placeholders such as `(No period)` / `(No type)`). Incomplete missing-area CSV lists are localized the same way. Do **not** run `Status` on arbitrary Project/ColumnA cells outside Incomplete persons — English keys like `Education` can collide with project names.

Open ListView applies the localized catalog sub-report Label as the view caption and maps English column captions through `Header()`.

After editing message JSON, regenerate: `dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj`.

## Person search category

The only category that takes free text. It answers "find this person, then show me everything about them".

| Aspect | Behaviour |
|--------|-----------|
| Chrome | Search box (`.rd-search-box`) rendered next to the filter chips when `ReportDashboardCatalog.SupportsPersonSearch(Category)`; clear `×` resets the term |
| Empty term | Lists **all** persons (person-type / project filters are currently locked to All while those chrome rows are deprecated/hidden) |
| Haystack | `SearchText` on the view — lowercased + **diacritic-folded** first/middle/last name + personal number + **every** passport number (`PersonSearchTextNormalizer` / SQL `translate`; typing `u` matches `ü`) |
| Chart buckets | Current visa status: `Valid` / `Expiring (<30 days)` / `Expired` / `No visa` (reuses the green-amber-red status CSS) |
| Row click | Opens the **person dossier** (`PersonDossierOpenHelper`), not the Person DetailView — see [`PERSON_DOSSIER.md`](PERSON_DOSSIER.md) |
| Source | `vw_rd_person_search` → `VwRdPersonSearch` → `VwRdPersonSearch_ListView` |

**Parity invariant:** the term is tokenized once by `ReportDashboardCatalog.PersonSearchTokens` (whitespace split after diacritic fold) and consumed by both the Preview loader (`LoadPersonSearch`) and the drill-down criteria (`BuildPersonSearchCriteria`), so Preview total always equals **Open ListView** total. Keep SQL `translate` maps in sync with `PersonSearchTextNormalizer.SqlFoldFrom` / `SqlFoldTo`.

## Relationship to other features

| Feature | Relationship |
|---------|----------------|
| **Bo State Notifications** (header bell) | Separate; kept |
| **Person incomplete data** | Manual officer flag; category **Incomplete persons** — see [`PERSON_INCOMPLETE_DATA.md`](PERSON_INCOMPLETE_DATA.md) |
| **Person dossier** | Category **Person search** is the dashboard entry point; rows open the read-only dossier — see [`PERSON_DOSSIER.md`](PERSON_DOSSIER.md) |
| **State specifications / evaluators** | Still define BO state criteria for colors and notifications; **not** the home UI |
| **Resminamalar / Document copies** | Separate. Their Word/Excel files stay on `UserReportTemplate`. Dashboard invitation export does not read that table. |
| **Invitation Excel template** | Company formatting workbook for the five invitation tabs — see [Excel templates](#excel-templates-invitation) below |

## Excel templates (invitation)

Invitation **Open in Excel** fills a company workbook. An administrator downloads that workbook, changes its look, and uploads it. Every officer then exports into that file. This store is only for the Report Dashboard. It is not `UserReportTemplate` and it is not the Application Profile Resminamalar package.

### Who may change it

| Action | Who |
|--------|-----|
| **Open in Excel** | Any officer who can open the tab. The button is disabled when the loaded preview has no rows. |
| **Download template** / **Upload template** | An administrator only (`ApplicationRuntimeLogAdminHelper.IsCurrentUserAdministrator`). The buttons are hidden for other users. |

There is one stored file per tab. It is not per officer.

### Tabs that use it

`ReportDashboardExcelTemplateService.CanonicalKey` maps the visible tab and its hidden period/category twin onto one key:

| Stored key | Also accepts |
|------------|----------------|
| `ready-by-project` | `ready-by-period-category` |
| `expired-invitation` | — |
| `cancelled-invitation` | — |
| `in-process` | `in-process-by-period-category-type` |
| `used` | `used-by-period-category-type` |

The officer tab for `cancelled-invitation` is **Cancelled Invitation**. Columns are the invitation (Full name, Invitation #, Issued date, Expires, Period) and the cancellation application (App #, Date, Migration). There are no approval columns. A row is one invitation line cancelled by a completed cancellation application. The nine-month dashboard window does not apply.

The officer tab for `expired-invitation` is **Expired Invitation**. Its columns match Active Invitation. The row is an issued invitation that expired before a visa was issued. A visa cancelled later stays on Used Invitations. The nine-month dashboard window does not apply.

The officer tab for `used` is **Used Invitations**. Its columns are the Active Invitation columns, then Visa #, Issued date, Place, Start date, and End date. One invitation line has one visa. A visa cancelled later stays on the report.

`issued-inv` is not mapped. Other Report Dashboard categories still use `UserReportTemplate` when `ExcelConfigured` is true. Add a future tab by extending `CanonicalKey` and the cell writer. The table already stores any `Category` + `SubReportKey`. The Blazor buttons stay on Invitation until that gate is widened (`IsInvitationTemplateReport`).

### What the administrator may change

Formatting only: column widths, fonts, colors, header labels, and a title block above the header.

The export does not let the file hide, reorder, or add columns. Values are written by column index in `TableHeaders` order. Header text already in the file is left as the administrator wrote it.

### Workbook contract

Defined names (workbook scope), written by `ReportDashboardExcelTemplateWorkbook`:

| Name | Meaning |
|------|---------|
| `DashboardHeader` | The header row. Column count must equal the current `TableHeaders` count. |
| `DashboardSample` | The empty row directly under the header. Its style is copied onto every data row. |

The starter sheet is named `Report`. Row 1 is a merged title (localized tab label, bold 16pt). Row 2 is the localized headers. Row 3 is the blank sample row. Freeze is on row 2. Upload accepts another sheet name.

Upload is rejected when the file is not `.xlsx`, is empty, is over 8 MB, is missing either defined name, or the two ranges do not have the same column count as the current headers. A hidden `(V)` twin that has one extra chart column will not match the `(P)` file; export then builds a fresh starter for that download and does not overwrite the stored file.

On export, rows below the sample are deleted, the sample row is copied for each data row, and cells are filled by index. The title and header labels are not rewritten.

### Which rows are exported

**Open in Excel** writes the rows the table is showing. On Active Invitation, Expired Invitation, Cancelled Invitation, Invitation Process, and Used Invitations that is the search-box result: an empty box exports the loaded preview, and a typed search exports only the matching rows (the same `n / total` count on screen). The list is still capped by `PreviewLimit` (10_000). **Download template** stays the blank format file. A chart click does not filter this download.

Cell text:

- Active Invitation, Expired Invitation, and Invitation Process use `ReportDashboardInvitationTableExcel.Cells` (the same stacked cells as the preview). Expired Invitation uses the Active Invitation columns. Cancelled Invitation uses `CancelledCells`: invitation columns, then the cancellation application's App #, Date, and Migration.
- Used Invitations uses the Active Invitation cells, then Visa #, Issued date, Place, Start date, and End date.

### A. Maglumat

The Work Permit tab `active-by-project` is **A. Maglumat**. It uses the same company workbook as the Invitation tabs (`ReportDashboardExcelTemplate`, key `active-by-project` on category Work Permit). An administrator downloads the starter, changes formatting, and uploads it. Officers only export. **Open in Excel** keeps that formatting and writes `ReportDashboardWorkPermitMaglumatExcel.Cells` by column index. The first `dd.MM.yyyy` in the title row above the header becomes the day the file is created. The search box limits this file the same way it limits the table. Until a 10-column workbook is uploaded, export builds the starter.

One row is one person. Every valid work permit stays in the work-permit cell: not cancelled, and the expiration date is today or later. Newest start date is first. Each number opens that permit's scanned copies. The chart is by project and starts collapsed. Project is not a table column.

Columns: № (screen order of the rows on display), F.A.A, birth date and citizenship code, passport number and expiry (text), education level and institution, current actual job title, current Turkmenistan address, work-permit number with start and end (the number opens that permit's scanned copies), current valid visa number with start and end (the number opens visa copies), Bellik (empty). Passport text does not open copies.

**A. Maglumat 2** (`a-maglumat-2`) is the same report. It adds **Rugsat berlen ýerler** immediately after the work-permit dates. A person with several valid permits gets one location block per permit, in that same order, with a blank line between blocks. The text is `WorkPermitItem.WorkPermittedLocations`. Its list is `VwRdWorkPermitActiveLocations_ListView` on `vw_rd_work_permit_active_locations`. Its company workbook is separate (`a-maglumat-2`, 11 columns, sheet `A.Maglumat 2`) and uses the same title, with the first `dd.MM.yyyy` replaced by the export day.

**Goşmaça barjak ýerleri** (`a-maglumat-3`) is the same report plus **Goşmaça barjak ýerleri** after **Rugsat berlen ýerler**. Each issued application of profile `change_workpermit` (**Iş Rugsatnama goşmaça barjak ýeri**) linked to that permit is its own block: application number, application date, then `MovementPermitLocation`. Newest application is first. A permit with none keeps an empty block so the column stays lined up with the permits. Its list is `VwRdWorkPermitMaglumat3_ListView` on `vw_rd_work_permit_a_maglumat_3`. Its workbook is 12 columns, sheet `Goşmaça barjak ýerleri`, and uses the same title.

**Sanaw** (`sanaw`) uses the same people. Columns are №, Familiýasy, Ady, Doglan senesi we ýeri, Jynsy, Raýatlygy, Pasport belgesi we möhleti, Bilimi we okan ýeri, Bilimine görä hünäri, Wezipesi, Möhleti we gazanji (current salary start, end, and amount), Türkmenistandaky salgysy, Daşary ýurtdaky salgysy, Taslamasy, Maşgala agzalary, and Adam sany (the person plus linked family members, blank when there are none). The list is `VwRdWorkPermitSanaw_ListView`. The workbook title is `Daşary ýurt raýatlarynyň sanawy`. Work Permit shows A. Maglumat, A. Maglumat 2, Goşmaça barjak ýerleri, and Sanaw.

### Where it is stored

`ReportDashboardExcelTemplate` (`ReportDashboardExcelTemplates`):

| Column | Role |
|--------|------|
| `Category` | `ReportDashboardCategory` |
| `SubReportKey` | Canonical key, max 80 characters |
| `TemplateFile` | Aggregated `FileData` (file name, bytes, size) |

Do not add `[FileAttachment]`. That attribute sends `FileData.Content` through Blazor browser storage and the server save does not keep the workbook. Do not add `[DomainComponent]`. `[NavigationItem(false)]` keeps it out of the officer menu.

The unique index is `(Category, SubReportKey)`. The screen saves through `INonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<ReportDashboardExcelTemplate>()`. The administrator check stays in `ReportDashboardExcelTemplateService.Save`, because that object space does not apply security.

### Creating the table on an existing database

XAF skips schema creation when `ModuleInfo` is already current, so adding the entity does not create the table. `ReportDashboardExcelTemplateSchemaSql.ApplyIfMissing` runs from `Startup.cs` in both the compatibility callback and `Configure`, and only after `People` exists. A missing table (`42P01`) makes **Download template** return the starter workbook and makes upload report that the file was not stored. Restart the site after this code is deployed so the startup script creates `ReportDashboardExcelTemplates`.

`GCRecord` and `OptimisticLockField` are `integer NOT NULL DEFAULT 0`. The startup unique index has no `GCRecord IS NULL` filter. The EF index filter `[GCRecord] IS NULL` does not match these rows.

## Application Profile join model

Report Dashboard SQL does **not** read the removed `Applications` / `ApplicationItems` tables or `ApplicationTypes` for new logic.

| Concept | Join |
|---------|------|
| Case | `"ApplicationProfileInstances"` |
| Type / config | `"ApplicationProfiles"` (`ProgressRoute`, `Produce*`, `ActionFamily`, `Name`/`Code`) |
| Roster | `"ApplicationProfileInstancePeople"` + `"ApplicationProfileInstancePersonResolvedLinks"` |
| Progress | `"ApplicationProfileInstanceProgresses"` |

C# loaders that still query `ApplicationProfileInstance` use **profile-first** filters (`ReportDashboardProfileInstanceQuery`), with deprecated `ApplicationType` only when the profile FK is null (dual-read). Column aliases `ApplicationTypeLabel` / `ApplicationItemOid` on some `VwRd*` BOs are **profile name** / synthetic roster line id — not the old tables.

Person/document categories (visa validity, work permit items, education, position, incomplete persons, person search) are independent of ApplicationItem.

See [`.cursor/skills/visa2026-report-dashboard/IMPLEMENTATION_PLAN.md`](../.cursor/skills/visa2026-report-dashboard/IMPLEMENTATION_PLAN.md) § Application Profile residual sync.

## Removed

The old State Dashboard UI (tiles per state code 뿯↽ filtered list) is **removed** and must not be reintroduced. See superseded notes in `STATE_SPECIFICATIONS.md` and related docs.
