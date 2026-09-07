# VISA2014 to Visa2026 import - chat openers

Copy-paste into Cursor chat. Reference:

**@visa2014-to-visa2026-import** or **@.cursor/skills/visa2014-to-visa2026-import**

**Sole skill for data migration** (Demo/Prod **targets** on .25 included). Import-only (`--import-visa2014`); no delta Sync.

**Calik Energi source (locked):** legacy **`10.100.128.15` / `VISA2015`** for Import **and** lookup mapping — never Demo/Prod Visa2026 DBs on `.25`.

**In scope:** discovery, Excel preview, lookup resolution, lookup preflight, OData / in-process import, OnPrem-Sync.ps1 Demo/Prod (optional **-IncludeFileWaves** / DocumentCopies), reimport history compare, partial reimport (dev), Calik Energi.

**Out of scope:** on-prem IIS publish / ForceUpdate / staging .bak -> @visa2026-windows-iis-deploy ; lookup seed JSON authoring alone -> @visa2026-lookup-data

**Agent should:** read learnings.md first; append after every import attempt (MATURITY.md).

**Locked full-Import order:** lookup resolution -> lookup preflight -> full Import (SKILL.md Full Import order).

---

## Quick start

| You want | Copy this |
|----------|-----------|
| Orient me | @visa2014-to-visa2026-import Summarize migration status and currentFocus from migration-status.yaml + recent learnings. |
| Full Import order | @visa2014-to-visa2026-import Remind me: lookup resolution -> lookup preflight -> full Import. What is still open for Calik Energi? |
| After verified work | @visa2014-to-visa2026-import Append learnings.md for [title] - verified: [one line]. |

---

## Lookup resolution (before Import)

| Intent | Prompt |
|--------|--------|
| Explain process | @visa2014-to-visa2026-import Explain lookup resolution for Calik Energi - LookupCatalogs vs lookup-translations.yaml vs tenant JSON. |
| Audit one catalog | @visa2014-to-visa2026-import Lookup resolution: audit DISTINCT live values for [Catalog] on VISA2015, compare to Visa2026, update lookup-translations (calik overlay if needed). |
| Fix preflight gaps | @visa2014-to-visa2026-import Lookup resolution from Demo preflight report - gap list from report OK, but seed values from VISA2015 on 10.100.128.15 (not Demo DB), then re-run preflight. |
| City / FullAddress | @visa2014-to-visa2026-import Lookup resolution for AddressOfResidence: FullAddress -> Region/City - CityByName gaps from preflight. |
| Calik overlay | @visa2014-to-visa2026-import Review lookup-translations.calik-energi.yaml for [ProjectContract|EducationInstitution|Position|Department] - identityPassThrough vs values[]. |
| Comparison doc | @visa2014-to-visa2026-import Open lookup-comparisons/[Catalog] and sync decisions into lookup-translations.yaml. |

---

## Mapping verify (post-import)

| Need | Say |
|------|-----|
| Design / status | @visa2014-to-visa2026-import Summarize mapping verification tiers and CLI status ([MAPPING_VERIFICATION.md](../../../docs/VISA2014_MIGRATION/MAPPING_VERIFICATION.md)). |
| After Application import | @visa2014-to-visa2026-import Run or stub mapping verify for Application (ApplicationType histograms + sample parity). |
| Implement CLI | @visa2014-to-visa2026-import Implement `--verify-visa2014-mapping` per MAPPING_VERIFICATION.md; pilot Application. |
| After ApplicationProgress import | @visa2014-to-visa2026-import Run mapping verify for ApplicationProgress (State histograms + sample parity). |
| Silent outcomes | @visa2014-to-visa2026-import Run Application mapping verify and summarize Silent / implicit outcomes (buckets + unexpected). |

## Lookup preflight

| Intent | Prompt |
|--------|--------|
| Run Demo preflight | @visa2014-to-visa2026-import Run Preflight-LookupAudit.ps1 for calik-energi-onprem-demo on .25 (C:\visa2026-sync-demo). Summarize blocking gaps. |
| Local preflight | @visa2014-to-visa2026-import Run --preflight-visa2014-lookups --legacy-source calik-energi with local target connection. Exit 0 required. |
| Catalog-only (fast) | @visa2014-to-visa2026-import Lookup preflight --catalog-only for Calik - Phase A sampleQuery only. |
| After fix, re-gate | @visa2014-to-visa2026-import Re-run lookup preflight after catalog/translation fixes - must pass before full Import. |

---

## Demo / Prod Import on .25

| Intent | Prompt |
|--------|--------|
| Demo full Import (scalar) | @visa2014-to-visa2026-import Demo full Import on .25 after lookup preflight passed. No ContinueOnError. Ask before wipe/restart. |
| Demo Import + document copies | @visa2014-to-visa2026-import Demo full Import on .25 with **-IncludeFileWaves** (DocumentCopies.ps1 after scalar: Photo + Passport/Visa/Education/WorkPermit/Invitation/FamilyProof docs). Ask before wipe/restart. Archive must run after file waves. |
| File waves only (resume) | @visa2014-to-visa2026-import On Demo sync host, run DocumentCopies.ps1 only (or OnPrem-Sync with -IncludeFileWaves after scalars already imported). Use -StartAt [Person-Photo\|PassportDocument\|…] if resuming mid file chain. |
| Watch Demo Import | @visa2014-to-visa2026-import Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh -ClearScreen |
| Compare reimports (CLI) | @visa2014-to-visa2026-import Archive/compare Demo reimports — history\index.html + Compare-OnPremImportRuns.ps1 (DbCount + file-presence anomalies). |
| Compare in XAF | @visa2014-to-visa2026-import On Demo (Admin): Operations → Import reimport history — compare Left/Right RunIds for DbCounts, file waves, and file presence. Expect “file waves not run” unless archive had -IncludeFileWaves. |
| Resume after fail | @visa2014-to-visa2026-import Import stopped at [Entity] FailedCount>0 - fix mapping/catalogs, then -StartAt [Entity] (-SkipLookupPreflight only if approved). |
| Prod catch-up Import | @visa2014-to-visa2026-import OnPrem-Sync.ps1 -Profile Production -Mode Import for application-domain entities. Ask before running. Add -IncludeFileWaves only when intentional (disk/time). |
| Single BO by ApplicationType | @visa2014-to-visa2026-import Import --entity [BO] for ApplicationType [App_Inv] only. Inner sequence: header → roster → progress → issued → visa. Do not import Visa before the instance. |
| Single BO mapping check (dev) | @visa2014-to-visa2026-import Development mapping check: --entity [BO] --max-rows [N default 1] --legacy-source calik-energi-local-pg --inprocess. Show one Field / Legacy / Imported / Result table per row; halt until I accept or suggest a mapping change. Then remainder. Not for Demo/Prod full Import. |
| Compare more sample rows (dev) | @visa2014-to-visa2026-import Raise sample to N=[2+] for the current BO and show comparison again. Do not import remainder until I accept. |
| Legacy MCP check | @visa2014-to-visa2026-import Preflight: visa2014-sql-remote to VISA2015 on 10.100.128.15 - SELECT DB_NAME(). |

**Note:** `-IncludeFileWaves` is opt-in. Without it, history shows Files=No / empty document-copy steps; file-presence (Photo, *Document counts) can still be archived from the live DB.

---

## Discovery / mapping

| Intent | Prompt |
|--------|--------|
| Next BO | @visa2014-to-visa2026-import Next discovery BO from order.yaml - dossier + field-map + lookup catalogs. |
| Excel preview | @visa2014-to-visa2026-import Export Excel preview for [Entity] --legacy-source calik-energi. |
| importConfirmed | @visa2014-to-visa2026-import Review [Entity] for importConfirmed - preview + layer 3 complete? |

---

## Cross-skill

| Intent | Prompt |
|--------|--------|
| IIS / ForceUpdate / staging bak | @visa2026-windows-iis-deploy ... |
| Seed catalog JSON only | @visa2026-lookup-data Add/fix [catalog] seed for Calik gap from lookup resolution. |