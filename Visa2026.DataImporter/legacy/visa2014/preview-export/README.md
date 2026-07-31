# VISA2014 legacy — Excel preview exports

**Purpose:** Local **`.xlsx`** files showing consolidated, import-ready data from **`VISA2015`** (after dedupe + field-map transforms).

**Spec:** [docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md](../../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md)

**Planned CLI** (Person, Passport, **Visa** shipped):

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --export-visa2014-preview `
  --entity Visa `
  --legacy-source calik-energi
```

**Git:** `*.xlsx` in this folder is **gitignored** (production PII). Only this README is tracked.

**Import-gap preview** (target-aware unresolved rows for manual review — not approved exclusions):

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --export-visa2014-import-gaps `
  --entity AddressOfResidence `
  --legacy-source calik-energi-onprem-demo `
  --target-connection "Server=10.100.128.25\SQLEXPRESS;Database=Visa2026DbDemo;..." `
  --person-id-map ...\Person.json `
  --address-id-map ...\AddressOfResidence.json `
  --output legacy\visa2014\preview-export\AddressOfResidence-ImportGaps-demo.xlsx
```

**Workflow:** generate after discovery `complete` → review in Excel → then set `importConfirmed: true` on the dossier.
