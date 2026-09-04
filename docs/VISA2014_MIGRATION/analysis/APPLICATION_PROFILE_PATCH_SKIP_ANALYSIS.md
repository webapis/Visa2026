# Application Profile patch — skip analysis (Wave 0b)

**Date:** 2026-08-11  
**Target:** local `visa2026` · legacy source `calik-energi-local-pg`  
**Tool:** `--patch-visa2014-application-profile` with `--skip-report`

## Summary

| Metric | Before fix | After fix |
|--------|------------|-----------|
| Applications in scope | 12,282 | 12,282 |
| Skipped (no profile) | **5,103** | **0** |
| Already correct | 2,417 | 7,520 |
| Patched (would write) | 4,754 | 4,754 |
| Skipped (no transform) | 8 | 8 |
| Failed | 0 | 0 |

## Root cause

All **5,103** skips were bucket `TYPE_ONLY_GAP` for **registration** application types (direct migration, no contract):

| Count | Type | Profile code |
|------:|------|--------------|
| 2,130 | `App_Reg_Check_Out` | `check_out` |
| 1,752 | `App_Reg_Check_In` | `check_in` |
| 633 | `App_Reg_Info_Change_Address` | `check_in_info_change` |
| 487 | `App_Reg_Check_In_Internal` | `check_in` |
| 101 | `App_Reg_Info_Change_Passport` | `check_in_info_change` |

Profiles **existed** in the tenant catalog and DB (`DefaultProjectContract` null), but `ApplicationProfileCatalogGroupKey.NameLooksLikeContractVariant` treated Turkmen titles with descriptive parentheses — e.g. `Hasaba Almak (Daşary ýurtdan gelmegi sebäpli)` — as contract-variant suffixes `(Şatlyk-1)`.

Type-only matching requires a profile with `DefaultProjectContractId == null` **and** a name that does not look like a contract variant. Registration titles failed the name heuristic.

## Fix

**`ApplicationProfileCatalogGrouping.NameLooksLikeContractVariant`** — only treat trailing `(…)` as a contract variant when the inner text is a **short code without spaces** (Wave 0b contract suffix), not a Turkmen phrase.

File: `Visa2026.Module/DatabaseUpdate/ApplicationProfileCatalogGrouping.cs`

## Patch tooling

- Skip histogram buckets: `Visa2026.DataImporter/legacy/visa2014/Visa2014ApplicationProfilePatch.cs`
- Report path: `docs/VISA2014_MIGRATION/analysis/application-profile-patch-skips.md` (auto-written by `Application-Profile.ps1`)

Re-run:

```powershell
.\scripts\visa2014-migration\patch\Application-Profile.ps1 -LegacySource calik-energi-local-pg -DryRun
.\scripts\visa2014-migration\patch\Application-Profile.ps1 -LegacySource calik-energi-local-pg
```

## Follow-up (not in scope now)

- **Demo/staging `.25` promote** — excluded from current plan per developer request.
- **Wave 3** — nested templates per type/contract pattern.
- **Shared profile `Code`** — several `ApplicationType` rows still share semantic codes (`check_in`, `get_invitation`, …). Contract-variant matching covers via-ministry types; registration types rely on one type-only row per code. Consider unique `Code` per `ProfileCatalogKey` if distinct configs per type are required long term.
