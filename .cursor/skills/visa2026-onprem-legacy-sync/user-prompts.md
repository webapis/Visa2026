# On-prem legacy sync — chat openers

Use `@visa2026-onprem-legacy-sync` when your Cursor UI supports skill mentions.

**Hosts:** legacy `10.100.128.15` (VISA2015) → **prod only** `10.100.128.25` (`Visa2026DbProd`)  
**Staging:** prod `.bak` restore — **not** legacy sync  
**Legacy MCP:** `visa2014-sql-remote` (`.cursor/mcp.json`)

---

## Production (legacy sync)

| Intent | Prompt |
|--------|--------|
| Preflight MCP | `@visa2026-onprem-legacy-sync Preflight: visa2014-sql-remote to VISA2015 on 10.100.128.15 — SELECT DB_NAME() and Application count.` |
| Id-map bootstrap | `@visa2026-onprem-legacy-sync Prod on .25 restored from calik-energi LocalDB — copy id-maps to calik-energi-onprem-prod and verify 19 files.` |
| Manual catch-up | `@visa2026-onprem-legacy-sync Run manual prod catch-up on .25 (application-domain entities only). Ask before running.` |
| Sync state report | `@visa2026-onprem-legacy-sync Run Compare-OnPremSyncState.ps1 for calik-energi-onprem-prod — scalar + FileData tables, id-map counts, sync watermark. Summarize gaps and next wave.` |
| Reconcile | `@visa2026-onprem-legacy-sync Compare legacy .15 vs Visa2026DbProd with Compare-OnPremSyncState.ps1 (-ShowNotes). List BOs with NotCompleted > 0 and whether sync vs import is needed.` |
| File waves | `@visa2026-onprem-legacy-sync Weekly prod file wave: OnPrem-Sync.ps1 -Profile Production -IncludeFileWaves.` |
| Close FileData Partials | `@visa2026-onprem-legacy-sync Re-run remediable FileData waves on prod (Photo/Passport/Education/FamilyProof). Expect hard residual per S11; Person.Photo uses --id-map.` |
| Export dashboard | `@visa2026-onprem-legacy-sync Export-OnPremSyncDashboard from workstation to Temp, scp JSON+HTML to C:\visa2026-sync on .25.` |

## Staging (from prod backup)

| Intent | Prompt |
|--------|--------|
| Refresh staging | `@visa2026-onprem-legacy-sync Refresh Visa2026DbStaging from prod backup on .25 — not legacy sync.` |
| UAT smoke | `@visa2026-onprem-legacy-sync Staging read-only UAT on http://10.100.128.25:8080 after prod restore.` |

## Cross-skill

| Intent | Prompt |
|--------|--------|
| IIS not up | `@visa2026-windows-iis-deploy Prod slot on 10.100.128.25 not responding — smoke LoginPage before sync.` |
| Mapping fix | `@visa2014-to-visa2026-import ApplicationItem transform issue — fix mapping in dev LocalDB before on-prem re-sync.` |
| Delta sync impl | `@visa2026-onprem-legacy-sync Plan --sync-visa2014 v1 for prod nightly (PATCH + GCRecord) before 1–2 month cutover.` |
