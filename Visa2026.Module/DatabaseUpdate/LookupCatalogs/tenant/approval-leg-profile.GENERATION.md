# approval-leg-profile.json — generated artifact

**Do not edit `approval-leg-profile.json` by hand** for Çalik Energi / VISA2015 migrations. Regenerate from legacy SQL via the scripts below, then commit the JSON. `ApprovalLegProfileSeedUpdater` embeds this file on deploy; it does **not** read `dbo.Applications` or VISA2015 at runtime.

## Pipeline (VISA2015 → tenant JSON → SQL seed)

```text
VISA2015 (read-only SQL)
  └─ scripts/visa2014-migration/catalogs/generate/ProjectContract-CalikEnergi.ps1
       → project-contract.calik-energi.json  (per-contract ministry chains; input only)
  └─ scripts/visa2014-migration/catalogs/generate/ApprovalLegProfile.ps1
       → approval-leg-profile.json           (~10 deduped shared profiles)
  └─ App deploy / Update-LocalDatabase.ps1
       → ApprovalLegProfileSeedUpdater
       → dbo.ApprovalLegProfiles + dbo.ApprovalLegProfileMinistryLegs

VISA2015 (per application, at import)
  └─ Visa2014ApplicationApprovalLegProfileInference + OData PATCH
       → Application.ApprovalLegProfile (+ snapshots on save)
```

## Regenerate (local)

Automatic (preferred — `order.yaml` **tenantCatalogGeneration**):

```powershell
# Before application-domain import (also runs from Import-Visa2014OnPremStaging.ps1 / --import-visa2014 --entity Application)
dotnet run --project Visa2026.DataImporter -- --generate-visa2014-tenant-catalogs --legacy-source calik-energi

# Or with DB update in one pass:
.\scripts\local\Update-LocalDatabase.ps1 -GenerateTenantCatalogs -ForceUpdate
```

Manual equivalent:

```powershell
# Requires VISA2014_SQL_PASSWORD (ReadOnlyUser on VISA2015)
.\scripts\local\Generate-ProjectContractCalikEnergiCatalog.ps1

.\scripts\local\Generate-ApprovalLegProfileCatalog.ps1 `
  -ContractCatalogPath Visa2026.Module\DatabaseUpdate/LookupCatalogs/tenant/project-contract.calik-energi.json

# Optional: strip MinistryLegs from contract JSON after profiles are generated
.\scripts\local\Generate-ApprovalLegProfileCatalog.ps1 -StripContractLegs
```

## Prerequisites

- `tenant/approving-ministry.json` must be deployed first (`ApprovingMinistry.ShortNameTm` keys match leg `ApprovingMinistryShortNameTm` in profile JSON).
- SLA defaults (`MaxDaysInReview: 10`, `WarningDaysBeforeMax: 8`) are set by the generator, not from legacy.

## Related docs

- [`docs/VISA2014_MIGRATION/lookup-comparisons/ApprovalLegProfile.calik-energi.md`](../../../../docs/VISA2014_MIGRATION/lookup-comparisons/ApprovalLegProfile.calik-energi.md)
- [`docs/VISA2014_MIGRATION/lookup-comparisons/ProjectContract.calik-energi.md`](../../../../docs/VISA2014_MIGRATION/lookup-comparisons/ProjectContract.calik-energi.md)
