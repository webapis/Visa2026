# Excel user report seeds

- Embed **`.xlsx`** only (merge uses ClosedXML). Legacy **`.xls`** must be saved as `.xlsx` in Excel before embed.
- Add to `Visa2026.Module.csproj` as `EmbeddedResource`.
- Register in `DatabaseUpdate/UserReportTemplateUpdater.cs` via `EnsureExcelTemplateExists`.
- Placeholder rules: `docs/EXCEL_PLACEHOLDER_REFERENCE.md`

| Seed file | Template name (DB) | Applicability |
|-----------|-------------------|---------------|
| `Personnel_List_Seed.xlsx` | Personnel list (seed) | `App_Visa_and_WP_Ext` |
| `gurlusyk_uzt.xlsx` | Gurlusyk (seed) | `App_Visa_and_WP_Ext` |

Regenerate minimal `Personnel_List_Seed.xlsx`: `dotnet run --project tools/ExcelTemplateSpike/ExcelTemplateSpike.csproj`
