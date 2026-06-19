# Excel in-place editor — reference

Canonical narrative: [`docs/EXCEL_TEMPLATE_INPLACE_EDITOR.md`](../../../docs/EXCEL_TEMPLATE_INPLACE_EDITOR.md)

## Architecture

```text
UserReportTemplate DetailView (XAF Blazor)
  └─ Spreadsheet tab → ExcelSpreadsheetHost (Module, NotMapped)
       └─ UserReportTemplateExcelSpreadsheetPropertyEditor
            └─ Panel.razor: toolbar + <iframe data-src="...?embed=true">
                 └─ GET /user-report-template-spreadsheet/{templateId}
                      └─ DevExpress Spreadsheet (Open from FileData bytes)
                      └─ POST Save → FileData via UserReportTemplateSpreadsheetFileService
```

Generation at report time is unchanged: **ClosedXML** (`ExcelReportGenerator`). The editor only maintains the **`.xlsx` blob** in `UserReportTemplate.TemplateFile`.

## File map

| Area | Path |
|------|------|
| BO host property | `Visa2026.Module/BusinessObjects/UserReportTemplate.cs` (`ExcelSpreadsheetHost`) |
| Editor alias | `Visa2026.Module/Editors/UserReportTemplateExcelEditorAliases.cs` |
| Razor host page | `Visa2026.Blazor.Server/Pages/UserReportTemplateSpreadsheet.cshtml` (+ `.cshtml.cs`) |
| Iframe property editor | `Visa2026.Blazor.Server/Editors/UserReportTemplateExcelSpreadsheetPropertyEditor.cs` |
| Panel + toolbar | `Visa2026.Blazor.Server/Editors/UserReportTemplateExcelSpreadsheetPanel.razor` |
| HTTP auth (iframe) | `Visa2026.Blazor.Server/Services/UserReportTemplateSpreadsheetHttpAccess.cs` |
| Load/save FileData | `Visa2026.Blazor.Server/Services/UserReportTemplateSpreadsheetFileService.cs` |
| Document id / reload generation | `Visa2026.Blazor.Server/Services/UserReportTemplateSpreadsheetSessionService.cs` |
| Unsaved close guard | `Visa2026.Blazor.Server/Controllers/UserReportTemplateSpreadsheetCloseGuardController.cs` |
| Host JS | `wwwroot/js/user-report-template-spreadsheet-host.js` (parent) |
| Iframe JS | `wwwroot/js/user-report-template-spreadsheet.js` |
| CSS | `wwwroot/css/user-report-template-spreadsheet.css` |
| DetailView layout | `Visa2026.Blazor.Server/Model.xafml` — `UserReportTemplateSpreadsheetTab` |
| Round-trip test | `Visa2026.Module.Tests/ExcelReports/ExcelSpreadsheetRoundTripTests.cs` |
| UI strings | `tools/GenerateModelLocalization/UiStrings.messages.json` → `UserReport.ExcelSpreadsheet.*` |

## Startup / npm

**`Visa2026.Blazor.Server/Startup.cs`:**

- `AddDevExpressControls()` + `AddSpreadsheet()` with hibernation under `App_Data/SpreadsheetHibernation`
- Static files: `/node_modules` → `Visa2026.Blazor.Server/node_modules`
- Middleware order: **`UseXaf()` then `UseDevExpressControls()`** (reversed order causes `ValueManagerType` conflict)

**Packages (`package.json`):** `devextreme-dist`, `devexpress-aspnetcore-spreadsheet` (match DevExpress **25.2.6**). Run `npm ci` in `Visa2026.Blazor.Server` after clone.

**NuGet:** `DevExpress.AspNetCore.Spreadsheet` on Blazor.Server csproj.

## Embed mode

Iframe URL includes **`?embed=true`**:

- Hides duplicate toolbar inside iframe (`HideToolbar` on page model)
- Parent Panel shows **Save to template** / **Reload from database**
- CSS uses `height: 100%` chain (not `100vh` inside iframe)

## Reload flow

1. Panel `RequestReloadAsync` → `reloadSpreadsheetIframe` JS
2. Navigates to `...?embed=true&reload=true&_=timestamp`
3. `OnGet` bumps session **generation** → new `DocumentId` → fresh `Open()` from DB bytes

## Save flow

1. Parent postMessage `urt-spreadsheet-save-request` (or iframe toolbar if not embedded)
2. POST `spreadsheetState` + anti-forgery token
3. `SpreadsheetRequestProcessor.GetSpreadsheetFromState` → `SaveCopy(Xlsx)` → `TrySave` → `FileData`
4. Toast on parent via postMessage; officer should **Extract** on General if placeholders changed

## Localization keys

`UserReport.ExcelSpreadsheet.SaveToTemplate`, `.ReloadFromDatabase`, `.StatusSaved`, `.StatusUnsaved`, `.SaveSuccess`, `.SaveFailed`, `.ReloadConfirm`, `.ReadOnly`, `.NoFile`, `.UnsavedCloseWarning`

## Permissions

Same as template admin: read/write on `UserReportTemplate` and `FileData` via `UserReportTemplateEditAccess` / `UserReportTemplateSpreadsheetHttpAccess`.
