# Report Dashboard (home)

> **Canonical** officer home page for visa-related business intelligence.  
> Supersedes the removed **State Dashboard** (`StateDashboardComponent` / state-tile UI).  
> Prototype: repo-root [`tab_controls.html`](../tab_controls.html).

## Purpose

After login, officers land on this dashboard to grasp process state (invitation, visa extension, work permit, registration, travel, border zone, passport) by **project** and **person type**, then act via:

- **Charts** (list / pie / bar) of status buckets
- **Open in Excel** — mapped `UserReportTemplate` (when configured)
- **Open ListView** — filtered XAF ListView matching the current selection (and chart segment when clicked)

## Information architecture

| Layer | Values |
|-------|--------|
| Project filter | `All` + `ProjectContract` chips (overflow “+N more”) |
| Person type | Employees 뿯½ Family Members 뿯½ Temporary Visitors (`PersonRecordRole`) |
| Category | Application (via ministry) / Application (direct migration) / Visa / Invitation / Registration / WorkPermit / Travel / Address of Residence / BorderZone / Passport / Education / Position History / Subcontractor / Medical Records / Incomplete persons |

## Hosting

| Piece | Location |
|-------|----------|
| Host BO | `ReportDashboardHost` (non-persistent) |
| Blazor UI | `ReportDashboardComponent` + property editor |
| Navigation | Top-level **Home** 뿯↽ Report Dashboard (startup item) |
| Queries | `IReportDashboardQueryService` |


## Localization (Layer A)

Officer UI strings (chrome, category/sub-report/person-type/table headers, fixed validity/bucket labels, Home / Report Dashboard nav) use **`VisaUiMessages`** via **`ReportDashboardLocalization`**.

| Source | Role |
|--------|------|
| `tools/GenerateModelLocalization/UiStrings.messages.json` (`ReportDashboard.*`) | Runtime message catalog |
| `Visa2026.Module/Localization/UiStrings.json` (`navigation.Home`) | Model nav captions |
| `ReportDashboardLocalization.cs` | Resolve labels; `Status(english)` for display-only bucket text |

**Invariant:** SQL / loaders keep English status keys; Razor localizes at render so Open ListView criteria still match. Lookup/`NameTm` segments are not translated (Layer B).

`ReportDashboardLocalization.Status` localizes exact fixed labels and each recognized ` · ` segment (placeholders such as `(No period)` / `(No type)`). Incomplete missing-area CSV lists are localized the same way. Do **not** run `Status` on arbitrary Project/ColumnA cells outside Incomplete persons — English keys like `Education` can collide with project names.

Open ListView applies the localized catalog sub-report Label as the view caption and maps English column captions through `Header()`.

After editing message JSON, regenerate: `dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj`.

## Relationship to other features

| Feature | Relationship |
|---------|----------------|
| **Bo State Notifications** (header bell) | Separate; kept |
| **Person incomplete data** | Manual officer flag; category **Incomplete persons** — see [`PERSON_INCOMPLETE_DATA.md`](PERSON_INCOMPLETE_DATA.md) |
| **State specifications / evaluators** | Still define BO state criteria for colors and notifications; **not** the home UI |
| **Resminamalar / Document copies** | Unchanged; Excel action may reuse `UserReportTemplate` seeds |

## Removed

The old State Dashboard UI (tiles per state code 뿯↽ filtered list) is **removed** and must not be reintroduced. See superseded notes in `STATE_SPECIFICATIONS.md` and related docs.
