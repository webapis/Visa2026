# ApprovalLegProfile — Çalik Energi (VISA2015 → tenant JSON)

**Target BO:** `ApprovalLegProfile` + nested `ApprovalLegProfileMinistryLeg`  
**Tenant seed:** `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/approval-leg-profile.json`  
**DB updater:** `ApprovalLegProfileSeedUpdater` (not `LookupCatalogSyncUpdater` / not in `tenant/manifest.json`)

## Design

| Layer | Source | Notes |
|-------|--------|-------|
| **Shared profiles (~10 codes)** | Generated JSON | `TE-EN`, `TE-EN-GU`, `NG`, `NG-GU`, `TG`, `TG-GU`, `TH`, `TH-GU`, `TN`, `AH` |
| **Per-application FK** | VISA2014 import / PATCH | `Application.ApprovalLegProfile` from legacy ministry routing inference |
| **Not imported** | — | Profile rows from VISA2015 lookup tables; contract-level `ProjectContractMinistryLegs` on target |

Runtime DB update seeds **only** from committed JSON. It does **not** scan `dbo.Applications`.

## Generation (migration scripts)

### Stage 1 — Legacy contract ministry chains

`scripts/visa2014-migration/catalogs/generate/ProjectContract-CalikEnergi.ps1` reads **VISA2015** (`dbo.Application`, `dbo.Contract`, `dbo.AppliedMinistery`) and writes `tenant/project-contract.calik-energi.json` with optional nested `MinistryLegs` per contract code.

Inference rules (same family as `Visa2014ApplicationApprovalLegProfileInference`):

- Leg 1: `Application.AppliedMinistery` (majority per contract when ministry-forward date set)
- Türkmenenergo flow: `TitleOfMinisteryL` contains `energo` → `Türkmenenergo` → `Energetika`
- Leg 2+: `Gurluşyk` when construction forward (`DateForwardedToMinConstruction` / doc number)
- Fallback: `Contract.AppliedMinistery` or default `Energetika`

### Stage 2 — Deduplicate to shared profiles

`scripts/visa2014-migration/catalogs/generate/ApprovalLegProfile.ps1` (tool: `tools/GenerateApprovalLegProfileCatalog/`) collapses distinct leg chains from the contract catalog into `approval-leg-profile.json`.

Use `-StripContractLegs` after review to remove nested `MinistryLegs` from contract JSON (contracts become identity-only on target).

### Stage 3 — Deploy seed

`Update-LocalDatabase.ps1` or app startup with `FORCE_XAF_DB_UPDATE` / `--forceUpdate` runs `ApprovalLegProfileSeedUpdater` → `dbo.ApprovalLegProfiles` + `dbo.ApprovalLegProfileMinistryLegs`.

### Stage 4 — Application FK (import wave)

`scripts/visa2014-migration/patch/Application-ApprovalLegProfile.ps1` (or Application reimport with `ApprovalLegProfile` column) sets `Application.ApprovalLegProfile` from per-app legacy inference.

## Regenerate checklist

1. `VISA2014_SQL_PASSWORD` set; VISA2015 reachable
2. Run Stage 1 + Stage 2 scripts; commit `approval-leg-profile.json`
3. Deploy / `Update-LocalDatabase.ps1 -ForceUpdate`
4. Run Approval PATCH or reimport for application FKs

See also: [`tenant/approval-leg-profile.GENERATION.md`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/approval-leg-profile.GENERATION.md)
