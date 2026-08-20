# application-profile-approval-leg-versions*.json — generated artifact

**Do not invent ministry chains by hand.** Regenerate from VISA2015 via the matrix exporter, then review Defaults.

## Pipeline

```text
VISA2015 (read-only)
  └─ Visa2014ApplicationApprovalLegProfileInference + ApplicationType translations
       → frequency matrix (ApplicationType × ApprovalLegProfileCode)
       → application-profile-approval-leg-versions.calik-energi.json
  └─ App deploy / host-start ApplicationProfileSeedSync
       → ApplicationProfileApprovalLegVersionTenantCatalogSync
       → sets ApplicationProfile.DefaultApprovalLegProfile only
       → deletes leftover nested ApplicationProfileApprovalLegVersion copies
```

Shared chains themselves come from `approval-leg-profile.json` (Configuration). Do **not** copy legs onto each Application Profile.

## Regenerate

```powershell
.\scripts\visa2014-migration\catalogs\generate\ApplicationProfileApprovalLegVersions-CalikEnergi.ps1
# or:
dotnet run --project Visa2026.DataImporter -- --export-visa2014-application-profile-approval-leg-version-matrix --legacy-source calik-energi
```

Requires `VISA2014_SQL_PASSWORD` (ReadOnlyUser on VISA2015).

## Sign-off

Rows with `"SignOff": "approved"` are applied on deploy. Change Defaults in Configure profile after review if needed.

## Phase B

Imported via-ministry instances:

1. Keep `ApplicationProfileInstance.ApprovalLegProfile` when already set (VISA2015 inference / import PATCH).
2. Else use the template `DefaultApprovalLegProfile`.
3. Fill `ApprovalLegVersionName` and missing `ApplicationProfileInstanceApprovalLegSnapshot` rows.

Runs on F5 / deploy (`ApplicationProfileInstanceApprovalLegBackfill` after Default sync) and via:

```powershell
dotnet run --project Visa2026.DataImporter -- --backfill-application-approval-leg-snapshots --target-connection "<pg>" --dry-run
```