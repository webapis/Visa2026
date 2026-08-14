# Application Profile catalog — Wave 0 / 0b (legacy proposal)

**Status:** Wave 0b signed off 2026-08-11 (176 profiles) · tenant JSON promoted  
**Skills:** [visa2026-application-profile](../../.cursor/skills/visa2026-application-profile/SKILL.md) · [visa2014-to-visa2026-import](../../.cursor/skills/visa2014-to-visa2026-import/SKILL.md)

## Goal

Legacy `VISA2015` has no `ApplicationProfile` — only `dbo.Application` with type subtables. Wave 0/0b produces a **developer sign-off workbook** of proposed tenant profiles from full history (~12k applications).

### Wave 0b granularity (2026-08-11 — supersedes Wave 0)

| Route | Profile granularity |
|--------|---------------------|
| **Via ministries** (`ProgressRoute = ViaMinistries`) | **ApplicationType + ProjectContract** when legacy row has contract; else **type-only** |
| **Direct migration** | **ApplicationType only** |

| Topic | Decision |
|-------|----------|
| Profile `Code` | Keep existing type code (e.g. `get_invitation`) |
| Contract | Set `ApplicationProfile.DefaultProjectContract` on via-ministry variants |
| Match key | `Code` + `DefaultProjectContract` (not code alone) |
| Tenant JSON | Regenerate after 0b Excel sign-off (`SignOff` empty until approved) |

Wave 0 (one profile per type, 21 rows) remains in git history; do not use its Excel/JSON for import after 0b is approved.

## Run (Çalik — live legacy on `.15`)

```powershell
$env:VISA2014_SQL_PASSWORD = '<readonly-password>'
.\scripts\visa2014-migration\catalogs\generate\ApplicationProfileCatalog-CalikEnergi.ps1
```

Or via DataImporter directly:

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --export-visa2014-preview `
  --entity ApplicationProfileCatalog `
  --legacy-source calik-energi-local-pg `
  --output Visa2026.DataImporter\legacy\visa2014\preview-export\ApplicationProfileCatalog-proposal-0b.calik-energi.xlsx
```

**Source SQL:** `10.100.128.15` / `VISA2015` only (see import skill hard rule).  
**Output:** gitignored `*.xlsx` under `Visa2026.DataImporter/legacy/visa2014/preview-export/` (PII).

## Workbook sheets

| Sheet | Purpose |
|-------|---------|
| **ApplicationProfiles** | One row per profile group + `ProfileCatalogKey`, `ProfileGranularity`, `DefaultProjectContractCode`, usage counts, empty **Decision** / **SignOff** |
| **_ByComposite** | Legacy composite key → `ApplicationType` → counts (audit trail) |
| **_DuplicateNumbers** | Same `FullApplicationNumber`, multiple legacy Oids — confirms profile follows **each Oid’s type + contract** |
| **_MissingTypeCatalog** | Types in data but missing from `ApplicationTypeConfigurationCatalog.json` |
| **_SkippedComposites** | Import skip rows (`E:44`, `E:55`, …) |
| **_UnmappedLookups** | Lookup translation gaps |
| **_Meta** | Run metadata (`wave=0b-proposal`) |

Profile field mapping uses `ApplicationProfileFromApplicationTypeMapper` + `ApplicationTypeConfigurationCatalog.json`.

## Sign-off

**Wave 0b signed off 2026-08-11** — developer approved Excel (`ApplicationProfileCatalog-proposal-0b.calik-energi.xlsx`, **176** profiles). Tenant JSON promoted to `application-profile.calik-energi.json`.

1. ~~Open **ApplicationProfiles** sheet from latest 0b export.~~
2. ~~Verify **ApplicationCount** / `ProfileCatalogKey` / `DefaultProjectContractCode` / route columns.~~
3. ~~Fill **Decision** (`Keep` / `Exclude`) and **SignOff** (`approved`).~~
4. Fix any **_MissingTypeCatalog** / **_UnmappedLookups** before deploy sync.
5. Deploy / run `ApplicationProfileTenantCatalogSeedUpdater` on target DB, then re-run Application Profile patch dry-run.

## Wave 1 (tenant JSON)

```powershell
$env:VISA2014_SQL_PASSWORD = '<readonly-password>'
.\scripts\visa2014-migration\catalogs\generate\ApplicationProfileTenant-CalikEnergi.ps1
```

Synced on deploy via `ApplicationProfileTenantCatalogSeedUpdater` (matches `Code` + `DefaultProjectContract`).

## Wave 2 (Application import FK)

Greenfield `--import-visa2014 --entity Application` POSTs `ApplicationProfile` resolved by **ApplicationType + ProjectContract** (via-ministry) or type-only (direct / no contract).

**Product naming:** imported `Application` rows are **Application Profile instances** (running cases). Shared templates come from Wave 0b/1 tenant seed (+ Wave 3 nested templates) — not from cloning profile config per legacy app.

Backfill on DBs imported before Wave 2 / 0b:

```powershell
.\scripts\visa2014-migration\patch\Application-Profile.ps1 -LegacySource calik-energi-local-pg -DryRun
.\scripts\visa2014-migration\patch\Application-Profile.ps1 -LegacySource calik-energi-local-pg
```

Requires `ApplicationProfileTenantCatalogSeedUpdater` to have run once on the target DB with **0b** tenant JSON.

## Wave 2b (locked 2026-08-12) — People via ApplicationPerson, not ApplicationItem

| Topic | Decision |
|-------|----------|
| Import entity | Still OData **`Application`** (profile instance persistence) |
| People | Build **`ApplicationPerson` M2M** from legacy PersonInApplication (or equivalent) — **do not** import `ApplicationItem` |
| Auto-link person-related BOs | **Immediate resolve** when creating ApplicationPerson links (Passport, Visa, Education, … per profile `RequirePerson*` + §10.2 valid rules) |
| Child permits / invitations / visa issuing | Attach to **Application + Person** (and/or `ApplicationPerson`) only — **no** hidden ApplicationItem bridge (**option A**) |
| Wave order implication | Person + person-related scalar BOs (Passport, Visa, Education, …) **before** Application / ApplicationPerson so immediate resolve can find targets |

Implementation: **ApplicationPerson importer shipped 2026-08-12** (`--entity ApplicationPerson`, chains updated). ApplicationItem retained for dual-read / IssuingApplicationItem until child FK remap (WorkPermitItem / InvitationItem / Visa issuing → Application+Person). Track remap in import skill learnings.

## Wave 3 (nested templates)

See [APPLICATION_PROFILE_CATALOG_WAVE3.md](APPLICATION_PROFILE_CATALOG_WAVE3.md) — `ApplicationProfileTemplate` rows per profile catalog key from `UserReportTemplate` visibility on target DB.

## Historical / completed waves

| Wave | Deliverable | Status |
|------|-------------|--------|
| 0b | Profile catalog Excel + 176 profiles | Signed off 2026-08-11 |
| 1 | `application-profile.calik-energi.json` | Promoted |
| 2 | `Application.ApplicationProfile` FK patch | Done (local) |
| 3 | Nested templates per profile key | In progress |

Skip analysis (local PG): [`analysis/APPLICATION_PROFILE_PATCH_SKIP_ANALYSIS.md`](analysis/APPLICATION_PROFILE_PATCH_SKIP_ANALYSIS.md)
