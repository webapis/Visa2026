# Report Dashboard (home)

> **Canonical** officer home page for visa-related business intelligence.  
> Supersedes the removed **State Dashboard** (`StateDashboardComponent` / state-tile UI).  
> Prototype: repo-root [`tab_controls.html`](../tab_controls.html).

## Purpose

After login, officers land on this dashboard to grasp process state (invitation, visa extension, work permit, registration, travel, border zone, passport), then act via:

- **Charts** (list / pie / bar) of status buckets
- **Open in Excel** — mapped `UserReportTemplate` (when configured)
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
| Navigation | Top-level **Report Dashboard** (startup item; not under Home) |
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
| **Resminamalar / Document copies** | Unchanged; Excel action may reuse `UserReportTemplate` seeds |

## Removed

The old State Dashboard UI (tiles per state code 뿯↽ filtered list) is **removed** and must not be reintroduced. See superseded notes in `STATE_SPECIFICATIONS.md` and related docs.
