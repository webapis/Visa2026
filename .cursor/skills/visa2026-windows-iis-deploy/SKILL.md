---
name: visa2026-windows-iis-deploy
description: >-
  Deploy and update Visa2026 on company Windows Server with IIS + native SQL Server Express (no Docker/WSL).
  Publish, SSH copy, Configure-Visa2026Production, DB update, .bak restore, SQL sa/mixed-mode fixes, port 80
  portproxy cleanup. Use for on-prem IIS, localhost\SQLEXPRESS, scripts/windows-iis, visa2026-onprem host,
  C:\inetpub\visa2026, Login failed for user sa, site won't start on port 80.
disable-model-invocation: false
---

# Visa2026: Windows Server IIS deploy and updates

## Goal

Deploy or update Visa2026 on **Windows Server** using **IIS** and **SQL Server Express** on the same host — **no Docker**, **no WSL**.

**Canonical runbook:** [docs/ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md)

**Scripts:** [scripts/windows-iis/README.md](../../../scripts/windows-iis/README.md)

**Host experience (read first):** [learnings.md](./learnings.md)

**Copy-paste commands:** [reference.md](./reference.md)

**Incident log / funnel:** [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md) §8 (IIS on-prem)

**Not this skill:** Ubuntu + Docker ([setup-docker-engine](../setup-docker-engine/SKILL.md)), droplet ([visa2026-droplet-prod-deploy](../visa2026-droplet-prod-deploy/SKILL.md)), legacy WSL ([legacy-on-prem-windows-setup](../legacy-on-prem-windows-setup/SKILL.md)), local dev Docker ([visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md)).

### Chat openers

- `@.cursor/skills/visa2026-windows-iis-deploy/` — deploy or update IIS on-prem prod.
- **IIS Login failed for user sa** / **port 80 in use** / **restore .bak to SQLEXPRESS**.

---

## 1. Before you start

1. Read **[learnings.md](./learnings.md)** (append-only fixes from real hosts, e.g. `10.100.128.25`).
2. Confirm path: **IIS + native SQL**, not Docker. If the user wants Linux on-prem, use **setup-docker-engine**.
3. Secrets live in **`C:\visa2026\.env.prod`** on the server (`SA_PASSWORD`, `DEVEXPRESS_LICENSEKEY`, `DB_NAME`) — **never commit** or paste into chat.
4. SSH config example: host **`visa2026-onprem`** → company server; scripts copied to **`C:\visa2026-deploy\iis\`**.

---

## 2. Approval policy

| Class | Examples | OK required? |
|-------|----------|--------------|
| **Read-only** | `appcmd list site`, `sc query MSSQL$SQLEXPRESS`, `curl` LoginPage, `Diagnose-Port80.ps1` | No |
| **Deploy / mutate** | Copy publish folder, `Configure-Visa2026Production.ps1`, `Run-Visa2026DbUpdateOnServer.ps1`, `Restore-Visa2026SqlBackup.ps1`, `Configure-SqlExpressSaLogin.ps1`, `iisreset`, stop/start site | **Yes** (unless user said “go ahead”) |
| **Destructive** | `RESTORE … WITH REPLACE`, removing portproxy, SQL single-user `-m` bootstrap | **Yes** |

Take a **SQL `.bak`** before restore or risky schema update when prod data matters.

---

## 3. Greenfield deploy (summary)

Full steps: [reference.md § Greenfield](./reference.md). Runbook phases: [ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md).

| Step | Script / action |
|------|-----------------|
| Publish (dev PC) | `Publish-Visa2026ForIis.ps1` → `dist/visa2026-iis-<version>/` |
| SQL Express | `Install-SqlServerExpress.ps1` **or** manual install + `Configure-SqlExpressSaLogin.ps1` |
| IIS + Hosting Bundle | `Install-Visa2026ServerPrerequisites.ps1` |
| Site | `Install-Visa2026IisSite.ps1` → `C:\inetpub\visa2026` |
| Config | `Configure-Visa2026Production.ps1 -SqlServer 'localhost\SQLEXPRESS'` |
| App pool env | `Set-Visa2026AppPoolEnvironment.ps1` |
| DB | `Run-Visa2026DbUpdateOnServer.ps1 -ForceUpdate` (exit **0**) |
| Port 80 | `Diagnose-Port80.ps1`; remove WSL **portproxy** if present ([learnings](./learnings.md)) |
| Verify | HTTP **200** on `/LoginPage`; change **Admin** password |

**Remote orchestration (dev PC):** `Deploy-Visa2026IisRemote.ps1` (SSH + scp).

---

## 4. App update (each release)

1. **Backup** database (`.bak` on server under `C:\visa2026\backups\`).
2. **Publish** new version on dev PC.
3. **Stop** app pool `Visa2026` (or site).
4. Copy publish to **`C:\inetpub\visa2026`** — **keep** `appsettings.Production.json` and **`C:\ProgramData\Visa2026\DataProtection-Keys`**.
5. **`Run-Visa2026DbUpdateOnServer.ps1`** when release includes schema/module changes (`-ForceUpdate` if drift).
6. **Start** app pool + site; smoke **`/LoginPage`**.

There is **no** `docker compose pull` — track version via `publish-version.txt` in the publish folder.

---

## 5. Restore prod data from `.bak`

Use when migrating off Docker/WSL SQL or replacing a greenfield DB.

```powershell
# On server (after scp .bak to C:\visa2026\backups\)
C:\visa2026-deploy\iis\Restore-Visa2026SqlBackup.ps1 -BackupPath C:\visa2026\backups\<file>.bak
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1
```

Then verify login with **existing** users (not only greenfield Admin).

---

## 6. Investigation map

| Signal | Likely cause | Action |
|--------|----------------|--------|
| **`Login failed for user 'sa'`** | Manual SQL install: Windows-only auth, disabled `sa`, or `SA_PASSWORD` mismatch | [Configure-SqlExpressSaLogin.ps1](../../../scripts/windows-iis/Configure-SqlExpressSaLogin.ps1); see [learnings](./learnings.md) |
| **Site won't start / port 80 in use** | Leftover **`netsh interface portproxy`** from WSL/Docker | [Diagnose-Port80.ps1](../../../scripts/windows-iis/Diagnose-Port80.ps1); `portproxy delete` — [reference.md](./reference.md) |
| **`Invalid column name`** | App newer than DB schema | `Run-Visa2026DbUpdateOnServer.ps1 -ForceUpdate` |
| **502.5 / 500.30** | Hosting Bundle / runtime | [ON_PREM_WINDOWS_IIS.md § Troubleshooting](../../../docs/ON_PREM_WINDOWS_IIS.md) |
| **HTTP 500.30 after reboot** | **SQL Express** not running yet; app pool started first | Wait 2–3 min; `Set-Visa2026IisAutoStart.ps1`; ensure **Visa2026-IisAfterBoot** task exists |
| **IIS welcome page after reboot** | **Default Web Site** took port **80**; Visa2026 stopped | `Set-Visa2026IisAutoStart.ps1` ([learnings](./learnings.md)) |
| **Everyone logged out after recycle** | Data Protection keys path | `ASPNETCORE_DATA_PROTECTION_KEYS` → `C:\ProgramData\Visa2026\DataProtection-Keys` |
| **DevExpress license** | Missing app pool env | `Set-Visa2026AppPoolEnvironment.ps1` |

---

## 7. Record experience

After a **verified** fix on a real host:

1. **Append** [learnings.md](./learnings.md) (template in [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)).
2. If the pattern repeats, add a row to **§6** above or a short subsection in [DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md) §8.

Do **not** put secrets or full connection strings in learnings or docs.
