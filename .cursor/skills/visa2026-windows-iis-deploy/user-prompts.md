# Windows IIS deploy — user prompts

**Developer / ops only.** Copy-paste for **you** or **Cursor Agent** when deploying or fixing Visa2026 on **Windows Server + IIS + SQL Express** (no Docker).

Invoke [visa2026-windows-iis-deploy](./SKILL.md) with **`@visa2026-windows-iis-deploy`** (or `@.cursor/skills/visa2026-windows-iis-deploy`).

**Not this skill:** runtime app errors / inbox → [visa2026-runtime-error-tracking](../visa2026-runtime-error-tracking/user-prompts.md); Ubuntu Docker → [setup-docker-engine](../setup-docker-engine/SKILL.md); droplet → [visa2026-droplet-prod-deploy](../visa2026-droplet-prod-deploy/SKILL.md); CI → [ci-failed-triage](../ci-failed-triage/SKILL.md).

**Host:** SSH **`visa2026-onprem`** → `10.100.128.25` (example). Read [learnings.md](./learnings.md) before mutating prod.

| Slot | HTTPS URL | `-Profile` | Database |
|------|-----------|------------|----------|
| Production | `https://10.100.128.25/LoginPage` | `Production` | `Visa2026DbProd` |
| Staging | `https://10.100.128.25:8080/LoginPage` | `Staging` | `Visa2026DbStaging` |
| Demo | `https://10.100.128.25:8081/LoginPage` | `Demo` | `Visa2026DbDemo` |

Set `HTTPS_ENABLED=true` and `HTTPS_PORT` (443 / 8080 / 8081) in each slot’s `C:\visa2026\env\*.env` before deploy.

**Approval:** Agent may run **read-only** checks without asking. **Deploy, DB restore, recycle, and prod mutations** need your explicit OK unless you said “go ahead” in the same thread. See [SKILL.md § Approval policy](./SKILL.md).

---

## Quick start

| You want… | Copy this |
|-----------|-----------|
| **First-time orientation** | `@visa2026-windows-iis-deploy Walk me through the three-slot HTTPS layout on visa2026-onprem (prod :443, staging :8080, demo :8081). What scripts run on dev PC vs server?` |
| **Routine release to staging** | `@visa2026-windows-iis-deploy Deploy current branch to **Staging** on visa2026-onprem: publish, copy, HTTPS binding, DB update, smoke https://10.100.128.25:8080/LoginPage. Ask before running deploy.` |
| **Routine release to production** | `@visa2026-windows-iis-deploy Plan production HTTPS deploy — backup Visa2026DbProd first, confirm HTTPS_ENABLED in prod.env, then Deploy-Visa2026IisRemote.ps1 -Profile Production. Confirm each step with me before executing.` |
| **All three slots (one build)** | `@visa2026-windows-iis-deploy Deploy-Visa2026AllIisSlotsRemote.ps1 — publish once, deploy Production then Staging then Demo. Report version and HTTPS status per slot.` |
| **Health check only** | `@visa2026-windows-iis-deploy Read-only: curl HTTPS LoginPage on :443, :8080, :8081; list IIS https bindings on visa2026-onprem. No deploy.` |
| **After deploy — runtime errors** | `@visa2026-runtime-error-tracking We just deployed staging. Pull staging errors (last 2h), smoke https://10.100.128.25:8080/LoginPage, triage any open inbox rows.` |

---

## Deploy and update (dev PC → SSH)

Run from repo root unless noted.

```powershell
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Production
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Staging -ForceUpdate
.\scripts\windows-iis\Deploy-Visa2026AllIisSlotsRemote.ps1 -ForceUpdate
```

| Goal | User prompt |
|------|-------------|
| **Staging only** | `@visa2026-windows-iis-deploy Run Deploy-Visa2026IisRemote.ps1 -Profile Staging -ForceUpdate. After deploy, verify publish-version.txt and HTTPS 200 on https://<server>:8080/LoginPage.` |
| **Demo only** | `@visa2026-windows-iis-deploy Deploy demo slot (HTTPS :8081). Use -EnableForceXafDbUpdate only if schema drift; remove FORCE_XAF after successful update.` |
| **Production only** | `@visa2026-windows-iis-deploy Deploy production (HTTPS :443). Backup SQL first. Confirm HTTPS_ENABLED=true in prod.env. Stop if any step fails — do not leave prod in half-updated state.` |
| **Skip republish** (reuse dist) | `@visa2026-windows-iis-deploy Deploy-Visa2026IisRemote.ps1 -Profile Staging -SkipPublish -ForceUpdate` |
| **Files only, no DB update** | `@visa2026-windows-iis-deploy Deploy-Visa2026IisRemote.ps1 -Profile Staging -SkipDbUpdate — app bits only, then smoke test.` |
| **Known publish folder** | `@visa2026-windows-iis-deploy Deploy from dist\visa2026-iis-<version>\ to Staging using -PublishPath` |
| **Copy scripts to server** | `@visa2026-windows-iis-deploy scp scripts/windows-iis/*.ps1 to visa2026-onprem:C:/visa2026-deploy/iis/ — no site changes.` |
| **Post-deploy version audit** | `@visa2026-windows-iis-deploy Compare AssemblyVersion in repo vs publish-version.txt on each inetpub folder (prod, staging, demo).` |

---

## Greenfield (new server or all slots)

| Goal | User prompt |
|------|-------------|
| **Full greenfield checklist** | `@visa2026-windows-iis-deploy Greenfield on visa2026-onprem: SQL Express, IIS prerequisites, Install-Visa2026IisSlots.ps1, HTTPS per slot (Enable-Visa2026IisHttps.ps1), DB update per slot, Set-Visa2026IisSlotsAutoStart.ps1, firewall 443/8080/8081, Diagnose-Port80. Step-by-step with approval gates.` |
| **HTTPS all slots** | `@visa2026-windows-iis-deploy Enable-Visa2026IisHttps.ps1 for Production (-HttpsPort 443), Staging (8080), Demo (8081) with -IpAddress 10.100.128.25 -RedirectHttpToHttps. Confirm HTTPS_ENABLED in each env file.` |
| **Office trust (officers)** | `@visa2026-windows-iis-deploy Plan Set-Visa2026TemplateEditOfficeTrust.ps1 on officer PCs for https://10.100.128.25 — Resminamalar Edit template.` |
| **Install SQL + sa** | `@visa2026-windows-iis-deploy SQL Express on server: Install-SqlServerExpress.ps1 or Configure-SqlExpressSaLogin.ps1 using C:\visa2026\env\prod.env — fix Login failed for user sa before DB update.` |
| **Create three slots** | `@visa2026-windows-iis-deploy Run Install-Visa2026IisSlots.ps1 -SourceEnvFile C:\visa2026\.env.prod on server. Confirm sites Visa2026-Prod, -Staging, -Demo and three databases exist.` |
| **Firewall staging/demo** | `@visa2026-windows-iis-deploy Enable-Visa2026IisSlotFirewall.ps1 on server for TCP 8080 and 8081; confirm TCP 443 for production HTTPS.` |
| **Boot / auto-start** | `@visa2026-windows-iis-deploy Set-Visa2026IisSlotsAutoStart.ps1 + Register-Visa2026IisBootTask.ps1 — SQL before W3SVC, Default Web Site on 127.0.0.1:8090.` |

---

## Database (backup, restore, schema)

| Goal | User prompt |
|------|-------------|
| **Backup before prod deploy** | `@visa2026-windows-iis-deploy Before production deploy: backup Visa2026DbProd to C:\visa2026\backups\prod\ with timestamp. Confirm .bak size before proceeding.` |
| **Restore prod from .bak** | `@visa2026-windows-iis-deploy Restore-Visa2026SqlBackup.ps1 -Profile Production -BackupPath C:\visa2026\backups\prod\<file>.bak then Run-Visa2026DbUpdateOnServer.ps1 -Profile Production. Stop app pool during restore.` |
| **Clone prod → staging** (intentional) | `@visa2026-windows-iis-deploy Restore prod .bak to **Staging** (-Profile Staging) only — confirm I want staging overwritten.` |
| **Schema drift / Invalid column** | `@visa2026-windows-iis-deploy Staging shows Invalid column name — Run-Visa2026DbUpdateOnServer.ps1 -Profile Staging -ForceUpdate. If still failing, check FORCE_XAF_DB_UPDATE and stdout logs.` |
| **Remove stuck FORCE_XAF** | `@visa2026-windows-iis-deploy Remove-Visa2026ForceXafDbUpdate.ps1 -Profile Demo — site returns 500.30 after greenfield update.` |
| **Greenfield DB only** | `@visa2026-windows-iis-deploy Run-Visa2026DbUpdateOnServer.ps1 -Profile Demo -ForceUpdate on server — no app copy.` |

---

## Incidents and triage

Paste the symptom or error text after the skill tag.

| Symptom | User prompt |
|---------|-------------|
| **Login failed for user 'sa'** | `@visa2026-windows-iis-deploy SqlException 18456 sa login on visa2026-onprem. Diagnose mixed mode, sa disabled, password vs prod.env — run Configure-SqlExpressSaLogin.ps1 plan.` |
| **Port 80 in use** | `@visa2026-windows-iis-deploy Diagnose-Port80.ps1 on server — portproxy, Default Web Site, legacy Visa2026 site. Fix without breaking staging :8080.` |
| **HTTP 500.30** | `@visa2026-windows-iis-deploy :8080 returns 500.30 after reboot. Check MSSQL$SQLEXPRESS running, Get-Visa2026IisStartupError.ps1 -Profile Staging, recycle app pool.` |
| **502.5 / Hosting Bundle** | `@visa2026-windows-iis-deploy ASP.NET Core 502.5 on production — verify .NET 8 Hosting Bundle and app pool no managed code.` |
| **IIS welcome page on :80** | `@visa2026-windows-iis-deploy After reboot, http://:80 shows IIS default page not Visa2026. Fix site bindings and auto-start for Visa2026-Prod; officers should use https://.` |
| **Edit template / FSA blocked** | `@visa2026-windows-iis-deploy Officers on http:// cannot Edit template — confirm HTTPS URL, HTTPS_ENABLED in env, Enable-Visa2026IisHttps.ps1, Set-Visa2026TemplateEditOfficeTrust.ps1.` |
| **Logged out after recycle** | `@visa2026-windows-iis-deploy Users logged out after app pool recycle on staging — verify ASPNETCORE_DATA_PROTECTION_KEYS path for -Profile Staging.` |
| **Wrong data on prod URL** | `@visa2026-windows-iis-deploy Production URL shows demo data — verify -Profile, DB_NAME in prod.env, and that legacy single-site swap did not happen.` |
| **Site won't start 80070020** | `@visa2026-windows-iis-deploy appcmd start site failed 80070020 — netstat port conflict on :80 or :8080.` |
| **Event log triage** (manual) | `@visa2026-windows-iis-deploy Get-Visa2026RecentIisErrors.ps1 -Profile Production on server — summarize last errors (no auto inbox).` |
| **Stdout logging** | `@visa2026-windows-iis-deploy Enable-Visa2026StdoutLog.ps1 -Profile Staging and tail logs\stdout for startup failure.` |

**Runtime errors in app (SQL inbox):** switch to `@visa2026-runtime-error-tracking` — not IIS deploy.

---

## Migration (legacy single site)

| Goal | User prompt |
|------|-------------|
| **Migrate to three slots** | `@visa2026-windows-iis-deploy Migrate legacy Visa2026 (C:\inetpub\visa2026) to multi-slot on 10.100.128.25 per reference.md § Migration. Enable HTTPS per slot; stop old site after prod smoke https://.` |
| **Legacy profile during cutover** | `@visa2026-windows-iis-deploy Which scripts still accept -Profile Legacy? Plan cutover without downtime on wrong DB.` |
| **Remove WSL/Docker portproxy** | `@visa2026-windows-iis-deploy Server had Docker/WSL on :80 — remove portproxy and stop containers before IIS prod bind.` |

---

## Read-only diagnostics (safe anytime)

| Goal | User prompt |
|------|-------------|
| **Slot inventory** | `@visa2026-windows-iis-deploy List all Visa2026 IIS sites, app pools, physical paths, and bound ports on visa2026-onprem.` |
| **SQL service state** | `@visa2026-windows-iis-deploy sc query MSSQL$SQLEXPRESS and confirm Visa2026DbProd, Visa2026DbStaging, Visa2026DbDemo exist.` |
| **Smoke all URLs** | `@visa2026-windows-iis-deploy curl HTTPS LoginPage on :443, :8080, :8081 from dev PC; report status codes and login-page slot badge if visible.` |
| **Console startup probe** | `@visa2026-windows-iis-deploy Test-Visa2026Startup.ps1 -Profile Staging on server — does exe start outside IIS?` |
| **Compare learnings** | `@visa2026-windows-iis-deploy My symptom: {paste}. Search learnings.md and SKILL.md §6 for matching fix before changing anything.` |

---

## Agent workflow hints

| Situation | What to say |
|-----------|-------------|
| **Explicit go-ahead** | `Go ahead with staging deploy` / `You have approval for production deploy after backup` |
| **Plan only** | `Plan only — do not SSH or deploy until I confirm` |
| **One slot at a time** | `Never deploy all slots unless I say Deploy-Visa2026AllIisSlotsRemote` |
| **Record a fix** | `@visa2026-windows-iis-deploy Append verified fix to learnings.md (no secrets)` |
| **Runbook deep dive** | `@visa2026-windows-iis-deploy Follow docs/ON_PREM_WINDOWS_IIS.md for {topic}` |

---

## Canonical commands

**Dev PC — publish:**

```powershell
.\scripts\windows-iis\Publish-Visa2026ForIis.ps1 -Zip -OpenOutputFolder
```

**Dev PC — deploy one slot:**

```powershell
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Production
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Staging -ForceUpdate
```

**Dev PC — all slots:**

```powershell
.\scripts\windows-iis\Deploy-Visa2026AllIisSlotsRemote.ps1 -ForceUpdate
```

**Server — DB update:**

```powershell
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Staging -ForceUpdate
```

Full tables: [reference.md](./reference.md) · [scripts/windows-iis/README.md](../../../scripts/windows-iis/README.md)
