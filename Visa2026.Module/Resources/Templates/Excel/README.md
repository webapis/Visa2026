# Excel user report seeds

- Embed **`.xlsx`** only (merge uses ClosedXML). Legacy **`.xls`** must be saved as `.xlsx` in Excel before embed.
- Add to `Visa2026.Module.csproj` as `EmbeddedResource`.
- Register in `DatabaseUpdate/UserReportTemplateUpdater.cs` via `EnsureExcelTemplateExists`.
- Placeholder rules: `docs/EXCEL_PLACEHOLDER_REFERENCE.md`
- **Do not** rebuild `.xlsx` by zipping a folder with `ZipFile.CreateFromDirectory` / `Compress-Archive` — that often yields a broken OOXML package and ClosedXML throws (`NullReferenceException` in `LoadSpreadsheetDocument`). Save from Excel or use **`XLWorkbook` → SaveAs** (`tools/ExcelTemplateSpike`, e.g. `patch-gurlusyk`).

| Seed file | Template name (DB) | Applicability |
|-----------|-------------------|---------------|
| `433_gurlusyk_uzt.xlsx` | Gurlusyk | `App_WP_Ext`, `App_Visa_and_WP_Ext`, `App_Visa_Ext_According_to_WP` — **Möhleti** column: `{{.Visa_DurationFrequencyBlock}}` on the `{{#ds.rows}}` row (not the 433-ek one-line tokens unless you prefer that layout) |
| `433_gurlusyk_ckl.xlsx` | Gurlusyk ckl | **`App_Inv_And_WP`** + GT-15 — **Möhleti**: `Çakylyk {{.Application_VisaPeriod_NameTm}}, {{.Application_VisaCategory_NameTm}}` |
| `433-ek_uzt.xlsx` | 433-ek sanawy | `App_WP_Ext`, `App_Visa_and_WP_Ext`, `App_Visa_Ext_According_to_WP` |
| `Sanaw_ckl.xlsx` | Sanaw_ckl (Excel) | **`App_Inv`**, **`App_Inv_And_WP`** + GT-15 — **`Sanaw_ckl_map.md`** (14-col çakylyk sanawy; row **5** loop) |
| `Sanaw_ckl_ministr_saparov.xlsx` | Sanaw_ckl_ministr_saparov (Excel) | Same visibility as **Sanaw_ckl (Excel)**; static footer **Ministr** / **A.Saparow** — **`Sanaw_ckl_ministr_saparov_map.md`** |

Spike commands: `-- patch-gurlusyk`, `-- patch-gurlusyk-mohlet`; build **`433-ek_uzt.xlsx`** from source **`433-ek.xls`**: `-- build-433-ek` (then `-- test-433-ek`); **`Sanaw_ckl`**: `-- scan-sanaw-ckl`, `-- test-sanaw-ckl`; **`Sanaw_ckl_ministr_saparov`**: `-- scan-sanaw-ckl-ministr`, `-- test-sanaw-ckl-ministr`.
