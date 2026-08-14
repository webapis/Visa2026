# Application Profile catalog — Wave 3 (nested templates)

**Status:** Signed off + patched local `visa2026` 2026-08-11 (691 JSON rows → **637** `ApplicationProfileTemplate` in DB; 54 rows skipped — profile key not resolved)  
**Skills:** [visa2026-application-profile](../../.cursor/skills/visa2026-application-profile/SKILL.md) · [visa2014-to-visa2026-import](../../.cursor/skills/visa2014-to-visa2026-import/SKILL.md)

## Goal

Populate `ApplicationProfile.NestedTemplates` (`ApplicationProfileTemplate` rows) per **Wave 0b profile catalog key** (`ProfileCatalogKey` = type + optional contract). Resminamalar uses this catalog when non-empty (`ApplicationProfileNestedTemplateCatalogHelper`).

Proposals come from **seeded `UserReportTemplate` visibility** on the **target Visa2026 DB** (not legacy SQL): synthetic `Application` probe per tenant profile row (type + default contract).

| Input | Source |
|-------|--------|
| Profile list | `application-profile.calik-energi.json` (Wave 1, signed off) |
| Template visibility | `UserReportTemplate` + `UserReportVisibilityService` on target DB |
| Output | Excel sign-off → `application-profile-nested-templates.calik-energi.json` |

Sync applies only rows with `"SignOff": "approved"`.

## Prerequisites

1. Wave 0b + Wave 1 complete (`application-profile.calik-energi.json` promoted).
2. Wave 2 patch applied (`Application.ApplicationProfile` FK).
3. Target DB has `UserReportTemplate` seeds (normal deploy / updater).

## Run (local `visa2026`)

### 1. Excel proposal

```powershell
.\scripts\visa2014-migration\catalogs\generate\ApplicationProfileNestedTemplates-CalikEnergi.ps1
```

Or:

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --export-visa2014-application-profile-nested-template-preview `
  --target-connection "Host=localhost;Port=5432;Database=visa2026;Username=postgres;Password=***;EFCoreProvider=Postgres" `
  --output Visa2026.DataImporter\legacy\visa2014\preview-export\ApplicationProfileNestedTemplates-proposal.calik-energi.xlsx
```

**Workbook sheets**

| Sheet | Purpose |
|-------|---------|
| **ProfileNestedTemplates** | One row per (profile key, template) + empty **Decision** / **SignOff** |
| **_ProfilesWithoutTemplates** | Profile keys with no visible templates (review type/group/contract filters) |
| **_Meta** | Row counts, target DB mask |

### 2. Sign-off

Review **ProfileNestedTemplates**. Confirm template sets per type/contract variant match officer expectations for Resminamalar.

### 3. Tenant JSON

```powershell
.\scripts\visa2014-migration\catalogs\generate\ApplicationProfileNestedTemplates-CalikEnergi.ps1 -ExportTenantJson
```

Set `"SignOff": "approved"` on each row in:

`Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/application-profile-nested-templates.calik-energi.json`

Rebuild **Visa2026.Module** so embedded JSON is current.

### 4. Patch target DB

```powershell
.\scripts\visa2014-migration\patch\Application-Profile-NestedTemplates.ps1 -DryRun
.\scripts\visa2014-migration\patch\Application-Profile-NestedTemplates.ps1
```

Deploy sync: `ApplicationProfileNestedTemplateTenantCatalogSeedUpdater` (after `ApplicationProfileTenantCatalogSeedUpdater`).

## Out of scope (this wave)

- Demo/staging promote on `.25` (per migration plan).
- Excel → JSON auto-promotion (manual `SignOff` on JSON for now).

## Related

- [APPLICATION_PROFILE_CATALOG_WAVE0.md](APPLICATION_PROFILE_CATALOG_WAVE0.md) — Waves 0b–2
- [APPLICATION_PROFILE_PLAN.md](../APPLICATION_PROFILE_PLAN.md) — live FK model
