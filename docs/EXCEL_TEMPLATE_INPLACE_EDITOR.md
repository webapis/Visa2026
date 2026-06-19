# Excel template in-place editor (DevExpress Spreadsheet)

In-browser editing for **Excel** user report templates on **Reports → User Report Templates**.

Word templates are unchanged (upload `.docx` as before). Excel templates can still be uploaded on the **General** tab; the **Spreadsheet** tab adds an embedded DevExpress ASP.NET Core Spreadsheet editor.

## Prerequisites

- DevExpress subscription that includes **ASP.NET Core Spreadsheet** (ASP.NET & Blazor / Universal).
- Host must serve Spreadsheet npm assets: from `Visa2026.Blazor.Server` run `npm ci` (or `npm install`) so `node_modules/devextreme-dist` and `node_modules/devexpress-aspnetcore-spreadsheet` exist.

## Officer workflow

1. Open an Excel **User Report Template** (or create one and set **Output Format** to Excel).
2. Upload an initial `.xlsx` on the **General** tab if the template has no file yet.
3. Open the **Spreadsheet** tab.
4. Edit layout and placeholder tokens in the embedded editor.
5. Click **Save to template** (writes bytes back to **Template File** in the database).
6. On the **General** tab, run **Extract Placeholders** then **Validate Placeholders** whenever `{{…}}` tokens changed.
7. Test from **Resminamalar** as usual.

**Reload from database** discards unsaved Spreadsheet edits and reloads the stored `FileData` blob.

## Permissions

Same as today: `UserReportTemplate` + `FileData` read/write via role grants. Users without template write see a read-only Spreadsheet.

## Technical notes

- XAF has no native Blazor `SpreadsheetPropertyEditor`; the UI hosts [`Pages/UserReportTemplateSpreadsheet.cshtml`](../Visa2026.Blazor.Server/Pages/UserReportTemplateSpreadsheet.cshtml) in an iframe via `ExcelSpreadsheetHost` on the detail view.
- Merge at generation time still uses **ClosedXML** (`ExcelReportGenerator`); the editor only maintains the `.xlsx` blob.
- Temporary Spreadsheet documents use server hibernation under `App_Data/SpreadsheetHibernation`.

## Limitations (v1)

- No auto **Extract** after save (toast reminds officers to run Extract manually).
- No version history / check-out.
- DevExpress Spreadsheet [unsupported features](https://docs.devexpress.com/AspNetCore/401838) may apply; ministry list templates depend on loop row layout — validate after major layout edits.
- EasyTest coverage deferred (iframe + Spreadsheet widget).

## Manual QA checklist

- [ ] Open seeded Excel template → Spreadsheet tab loads ministry layout.
- [ ] Edit a placeholder cell → **Save to template** → **Extract** shows updated tokens.
- [ ] **Validate** passes for unchanged token set.
- [ ] **Resminamalar** preview/ZIP still generates expected output.
- [ ] Replace file on General tab → **Reload from database** shows new upload.
- [ ] Read-only user: Spreadsheet visible, save disabled.
- [ ] Word template: no Spreadsheet tab / host hidden.

## Related docs

- [`USER_TEMPLATE_AUTHOR_GUIDE.md`](USER_TEMPLATE_AUTHOR_GUIDE.md)
- [`EXCEL_TEMPLATE_REPORTING_PLAN.md`](EXCEL_TEMPLATE_REPORTING_PLAN.md)
- [`EXCEL_PLACEHOLDER_REFERENCE.md`](EXCEL_PLACEHOLDER_REFERENCE.md)
- Agent skill: [`.cursor/skills/visa2026-excel-inplace-editor/SKILL.md`](../.cursor/skills/visa2026-excel-inplace-editor/SKILL.md)
