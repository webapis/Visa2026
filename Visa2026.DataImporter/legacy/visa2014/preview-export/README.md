# VISA2014 legacy — Excel preview exports

**Purpose:** Local **`.xlsx`** files showing consolidated, import-ready data from **`VISA2015`** (after dedupe + field-map transforms).

**Spec:** [docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md](../../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md)

**Planned CLI:**

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --export-visa2014-preview `
  --entity Person `
  --output Visa2026.DataImporter/legacy/visa2014/preview-export/Person-preview.xlsx
```

**Git:** `*.xlsx` in this folder is **gitignored** (production PII). Only this README is tracked.

**Workflow:** generate after discovery `complete` → review in Excel → then set `importConfirmed: true` on the dossier.
