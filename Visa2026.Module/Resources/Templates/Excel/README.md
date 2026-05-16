# Excel user report seeds

- Embed **`.xlsx`** only (merge uses ClosedXML). Legacy **`.xls`** must be saved as `.xlsx` in Excel before embed.
- Add to `Visa2026.Module.csproj` as `EmbeddedResource`.
- Register in `DatabaseUpdate/UserReportTemplateUpdater.cs` via `EnsureExcelTemplateExists`.
- Placeholder rules: `docs/EXCEL_PLACEHOLDER_REFERENCE.md`
- **Do not** rebuild `.xlsx` by zipping a folder with `ZipFile.CreateFromDirectory` / `Compress-Archive` — that often yields a broken OOXML package and ClosedXML throws (`NullReferenceException` in `LoadSpreadsheetDocument`). Save from Excel or use **`XLWorkbook` → SaveAs** (`tools/ExcelTemplateSpike`, e.g. `patch-gurlusyk`).

| Seed file | Template name (DB) | Applicability |
|-----------|-------------------|---------------|
| `gurlusyk_uzt.xlsx` | Gurlusyk (seed) | `App_Visa_and_WP_Ext` |
| `433-ek.xlsx` | 433-ek sanawy (seed) | `App_Visa_and_WP_Ext` |

Spike commands: `-- patch-gurlusyk`, `-- patch-gurlusyk-mohlet`; build **`433-ek.xlsx`** from source **`433-ek.xls`**: `-- build-433-ek` (then `-- test-433-ek`).
