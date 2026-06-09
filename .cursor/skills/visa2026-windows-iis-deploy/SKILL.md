---
name: visa2026-windows-iis-deploy
description: >-
  Deploy and update Visa2026 on company Windows Server with IIS + native SQL Server Express (no Docker/WSL).
  Three slots on one host — Production :80, Staging :8080, Demo :8081 — each with its own DB, publish folder,
  app pool, and env file. Publish, SSH copy, -Profile deploy, DB update, .bak restore, FORCE_XAF_DB_UPDATE per pool.
  Use for on-prem IIS, localhost\SQLEXPRESS, scripts/windows-iis, visa2026-onprem, Login failed for user sa.
disable-model-invocation: false
---

# Visa2026: Windows Server IIS deploy and updates

## Goal

Deploy or update Visa2026 on **Windows Server** using **IIS** and **SQL Server Express** on the same host — **no Docker**, **no WSL**.

One server runs **three independent slots** (separate site, app pool, publish folder, database, data-protection keys):

| Slot | Port | Site / pool | Publish path | Env file | Database |
|------|------|-------------|--------------|----------|----------|
| **Production** | **80** | `Visa2026-Prod` | `C:\inetpub\visa2026-prod` | `C:\visa2026\env\prod.env` | `Visa2026DbProd` |
| **Staging** | **8080** | `Visa2026-Staging` | `C:\inetpub\visa2026-staging` | `C:\visa2026\env\staging.env` | `Visa2026DbStaging` |
| **Demo** | **8081** | `Visa2026-Demo` | `C:\inetpub\visa2026-demo` | `C:\visa2026\env\demo.env` | `Visa2026DbDemo` |

Manifest: [Visa2026-IisSlots.ps1](../../../scripts/windows-iis/Visa2026-IisSlots.ps1). Pass **`-Profile Production|Staging|Demo`** on slot-aware scripts.

**Canonical runbook:** [docs/ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md)

**Scripts:** [scripts/windows-iis/README.md](../../../scripts/windows-iis/README.md)

**Host experience (read first):** [learnings.md](./learnings.md)

**Copy-paste commands:** [reference.md](./reference.md)

**Incident log / funnel:** [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md) §8 (IIS on-prem)

**Not this skill:** Ubuntu + Docker ([setup-docker-engine](../setup-docker-engine/SKILL.md)), droplet ([visa2026-droplet-prod-deploy](../visa2026-droplet-prod-deploy/SKILL.md)), legacy WSL ([legacy-on-prem-windows-setup](../legacy-on-prem-windows-setup/SKILL.md)), local dev Docker ([visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md)).

**Runtime app errors:** [visa2026-runtime-error-tracking](../visa2026-runtime-error-tracking/SKILL.md) — not IIS deploy/502/sa login (§6 below).

### Chat openers

- `@.cursor/skills/visa2026-windows-iis-deploy/` — deploy or update IIS slot (prod / staging / demo).
- **IIS Login failed for user sa** / **port 80 in use** / **restore .bak to SQLEXPRESS**.

---

## 1. Before you start

1. Read **[learnings.md](./learnings.md)** (append-only fixes from real hosts, e.g. `10.100.128.25`).
2. Confirm path: **IIS + native SQL**, not Docker.
3. **Ask which slot** if the user did not say (default **Production** for production releases; **Demo** for greenfield/playground).
4. Secrets live in **`C:\visa2026\env\*.env`** (`SA_PASSWORD`, `DEVEXPRESS_LICENSEKEY`, `DB_NAME` per slot). Legacy **`C:\visa2026\.env.prod`** can seed new env files — **never commit** or paste into chat.
5. SSH host **`visa2026-onprem`**; scripts on server under **`C:\visa2026-deploy\iis\`**.

**Legacy single site** (`Visa2026` on `C:\inetpub\visa2026`) — use **`-Profile Legacy`** or migrate via [reference.md § Migration](./reference.md).

---

## 2. Approval policy

| Class | Examples | OK required? |
|-------|----------|--------------|
| **Read-only** | `appcmd list site`, `sc query MSSQL$SQLEXPRESS`, curl LoginPage per port, `Diagnose-Port80.ps1` | No |
| **Deploy / mutate** | Copy publish, `Configure-Visa2026Production.ps1 -Profile …`, `Run-Visa2026DbUpdateOnServer.ps1 -Profile …`, restore `.bak`, recycle app pool | **Yes** (unless user said “go ahead”) |
| **Destructive** | `RESTORE … WITH REPLACE` on **prod** DB, removing portproxy, SQL single-user `-m` bootstrap | **Yes** |

Take a **SQL `.bak`** before restore or risky schema update on **Production**.

---

## 3. Greenfield (all slots on server)

Full steps: [reference.md](./reference.md). Phases: [ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md).

| Step | Script / action |
|------|-----------------|
| Publish (dev PC) | `Publish-Visa2026ForIis.ps1` |
| SQL Express | `Install-SqlServerExpress.ps1` or `Configure-SqlExpressSaLogin.ps1` |
| IIS + Hosting Bundle | `Install-Visa2026ServerPrerequisites.ps1` |
| **All three slots** | `Install-Visa2026IisSlots.ps1 -SourceEnvFile C:\visa2026\.env.prod` (creates sites, env templates, DBs) |
| Copy publish | Same build into each `C:\inetpub\visa2026-{prod,staging,demo}\` (or deploy one slot at a time) |
| Per-slot DB update | `Run-Visa2026DbUpdateOnServer.ps1 -Profile Demo -ForceUpdate` (etc.) |
| Auto-start | `Set-Visa2026IisSlotsAutoStart.ps1` (prod :80 + staging :8080 + demo :8081) |
| Port 80 | `Diagnose-Port80.ps1`; remove WSL **portproxy** if present |

**Remote (dev PC, one slot):**

```powershell
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Production
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Staging -ForceUpdate
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Demo -EnableForceXafDbUpdate -ForceUpdate
```

---

## 4. App update (each release)

1. **Backup** that slot’s database (`C:\visa2026\backups\<prod|staging|demo>\`).
2. **Publish** on dev PC.
3. **Stop** that slot’s app pool (`Visa2026-Prod` / `-Staging` / `-Demo`).
4. Copy publish to that slot’s **`C:\inetpub\visa2026-*`** — keep slot’s `appsettings.Production.json` and **`DataProtection-Keys-*`**.
5. **`Run-Visa2026DbUpdateOnServer.ps1 -Profile <slot>`** (`-ForceUpdate` if drift).
6. Start app pool + site; smoke that slot’s LoginPage URL (see table in § Goal).

Track version via `publish-version.txt` in the publish folder.

---

## 5. Restore prod data from `.bak`

```powershell
C:\visa2026-deploy\iis\Restore-Visa2026SqlBackup.ps1 -Profile Production -BackupPath C:\visa2026\backups\prod\<file>.bak
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Production
```

Use **`-Profile Staging`** only when intentionally cloning data to staging.

---

## 6. Investigation map

| Signal | Likely cause | Action |
|--------|----------------|--------|
| **`Login failed for user 'sa'`** | `sa` / mixed mode / password mismatch | [Configure-SqlExpressSaLogin.ps1](../../../scripts/windows-iis/Configure-SqlExpressSaLogin.ps1) |
| **Port 80 in use** | portproxy or Default Web Site | [Diagnose-Port80.ps1](../../../scripts/windows-iis/Diagnose-Port80.ps1) |
| **Staging won’t bind :8080** | Rare conflict with Default Web Site on `127.0.0.1:8080` | `Set-Visa2026IisSlotsAutoStart.ps1` moves Default to **8090** |
| **`Invalid column name`** | App newer than that slot’s DB | `Run-Visa2026DbUpdateOnServer.ps1 -Profile … -ForceUpdate` |
| **502.5 / 500.30** | Hosting Bundle / runtime | [ON_PREM_WINDOWS_IIS.md § Troubleshooting](../../../docs/ON_PREM_WINDOWS_IIS.md) |
| **Logged out after recycle** | Wrong data-protection path for slot | Per-slot `ASPNETCORE_DATA_PROTECTION_KEYS` via `Configure-Visa2026Production.ps1 -Profile …` |
| **`FORCE_XAF_DB_UPDATE` left on** | Slow startup / 500.30 | `Remove-Visa2026ForceXafDbUpdate.ps1 -Profile …` |
| **Wrong data shown** | Deployed to wrong slot or old single-site swap | Confirm `-Profile` and smoke URL port |

**Avoid** swapping `DB_NAME` on one site (`Set-Visa2026EnvDbName.ps1`) — prefer **slot deploy**.

---

## 7. Record experience

After a **verified** fix: **append** [learnings.md](./learnings.md). Do **not** put secrets in learnings or docs.
