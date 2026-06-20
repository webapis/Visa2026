# Template staging (UNC only)

Resminamalar **Edit template** writes to a **Windows SMB share** configured in `TemplateEditStaging:StagingRootUnc`.

**Development:** `\\127.0.0.1\Visa2026TemplateEdit` (see `appsettings.Development.json`).

Do not use local drive paths (`D:\`, `C:\`) or folders under this project — UNC only.

Verify share access:

```powershell
.\scripts\local\Ensure-TemplateEditDevShare.ps1
```

See [`docs/TEMPLATE_STAGING_EDIT.md`](../../docs/TEMPLATE_STAGING_EDIT.md).
