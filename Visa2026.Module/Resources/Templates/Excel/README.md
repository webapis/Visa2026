# Excel user report seeds

- Embed `.xlsx` files in `Visa2026.Module.csproj` as `EmbeddedResource`.
- Register in `DatabaseUpdate/UserReportTemplateUpdater.cs` with `TemplateOutputFormat.Excel`.
- Regenerate `Personnel_List_Seed.xlsx`: `dotnet run --project tools/ExcelTemplateSpike/ExcelTemplateSpike.csproj`
- Placeholder rules: `docs/EXCEL_PLACEHOLDER_REFERENCE.md`
