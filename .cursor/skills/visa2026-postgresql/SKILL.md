---
name: visa2026-postgresql
description: >-
  Download, install, and configure PostgreSQL on Windows Server for Visa2026
  (Demo pilot DB visa2026_demo, EFCORE_PROVIDER=Postgres, Npgsql). Covers EDB
  unattended installer vs binaries zip+initdb (preferred under SSH), service
  postgresql-x64-16, psql at C:\PostgreSQL\16\bin, demo.env PG_* keys, and
  empty-DB create. Use for PostgreSQL install, PG download, initdb, psql,
  Demo Postgres, dual EF provider. Not IIS publish (visa2026-windows-iis-deploy)
  or VISA2014 import (visa2014-to-visa2026-import).
disable-model-invocation: false
---

# Visa2026: PostgreSQL on Windows Server

**Scope:** **download → install → configure → create empty DB** for Visa2026 on a **Windows Server** IIS host. Primary target today: **Demo** slot (`visa2026_demo`).

**Canonical dual-provider notes:** [docs/ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md#dual-ef-providers-sql-server--postgresql)

**App deploy after PG is ready:** [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md)

**Commands:** [reference.md](./reference.md) · **Experience:** [learnings.md](./learnings.md) · **Prompts:** [prompts.md](./prompts.md)

**Maturity loop:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md) — read learnings before work; append after verified install/config fixes.

## Not this skill

| Target | Use instead |
|--------|-------------|
| IIS publish / slot recycle / ForceUpdate | [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) |
| VISA2014 → Visa2026 import on Demo PG | [visa2014-to-visa2026-import](../visa2014-to-visa2026-import/SKILL.md) |
| Ubuntu Docker SQL | [setup-docker-engine](../setup-docker-engine/SKILL.md) |
| Developer Docker Desktop SQL | [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) |

## Hard rules

- **Never commit** `PG_PASSWORD` / real connection strings.
- **Prod / Staging** stay on **SQL Express** until an explicit Postgres cutover.
- **One provider per database** — never point the same DB at both SQL Server and Postgres.
- Prefer **binaries zip + `initdb`** when installing over **SSH** (EDB GUI/exe often fails — see scenarios).
- After install, wire Demo via **`EFCORE_PROVIDER=Postgres`** + **`PG_*`** in `C:\visa2026\env\demo.env`, then `Configure-Visa2026Production.ps1 -Profile Demo`.

## Verified layout (Demo on `10.100.128.25`)

| Item | Value |
|------|--------|
| Version | PostgreSQL **16** |
| Binaries | `C:\PostgreSQL\16` (`bin\psql.exe`, `bin\initdb.exe`, …) |
| Data | `C:\PostgreSQL\16\data` (created by `initdb`, do not pre-create nonempty) |
| Service | `postgresql-x64-16` |
| Port | `5432` |
| Superuser | `postgres` |
| App DB | `visa2026_demo` (UTF8) |

## Goal (four steps)

| Step | Outcome |
|------|---------|
| **1 Download** | Installer or binaries zip under `C:\visa2026\downloads\` |
| **2 Install** | `psql` works; Windows service **Running** |
| **3 Configure** | `demo.env` has `EFCORE_PROVIDER=Postgres` + `PG_*`; listen/auth OK for local app |
| **4 Create DB** | Empty `visa2026_demo` exists; `SELECT version();` succeeds |

**Success:** `psql -h localhost -U postgres -d visa2026_demo -c "SELECT version();"` returns a row with no auth error.

---

## Before you start

1. Read **[learnings.md](./learnings.md)**.
2. Confirm target is **Windows IIS host** Demo PG (not Docker).
3. Ensure `C:\visa2026\env\demo.env` exists (or will) with `PG_PASSWORD` / `DB_NAME` — see [demo.env.example](../../../scripts/windows-iis/env/demo.env.example).
4. Admin / elevated PowerShell on the server (or SSH as admin capable of service install).

### Chat openers

- `@visa2026-postgresql` — install/configure PostgreSQL for Demo.
- EDB installer failed over SSH / need binaries zip + initdb.
- Create `visa2026_demo`, set `EFCORE_PROVIDER=Postgres`.

---

## Script allowlist

| Script | Role |
|--------|------|
| [Install-PostgreSqlForVisa2026.ps1](../../../scripts/windows-iis/Install-PostgreSqlForVisa2026.ps1) | Download EDB exe if missing, unattended install, ensure DB |
| [Configure-Visa2026Production.ps1](../../../scripts/windows-iis/Configure-Visa2026Production.ps1) | Writes Npgsql CS (`Persist Security Info=True;EFCoreProvider=Postgres`) |

Fallback (no script): official **binaries zip** procedure in [reference.md](./reference.md) § Binaries zip (SSH-safe).

### Forbidden

- Dropping/recreating **Prod/Staging SQL** DBs while “trying Postgres”
- Writing Visa2026 schema via raw SQL — use XAF `--updateDatabase` / IIS deploy ForceUpdate
- Putting Postgres passwords into git or chat logs

---

## Scenarios

| # | Signal | Fix |
|---|--------|-----|
| A1 | EDB exe fails under SSH (`temp_check_comspec.bat`, COM/temp) | Use **binaries zip + initdb** — [reference.md](./reference.md) |
| A2 | `psql` not found after install | Check `C:\PostgreSQL\16\bin` and `C:\Program Files\PostgreSQL\*\bin` |
| A3 | `initdb` fails on existing data dir | Use empty path; let `initdb` create the directory |
| A4 | Service missing / stopped | `pg_ctl register` / `Start-Service postgresql-x64-16` |
| A5 | Auth failed for `postgres` | `PGPASSWORD` / `pg_hba.conf` (scram/md5) for localhost |
| A6 | App still on SQL Express | `demo.env` → `EFCORE_PROVIDER=Postgres` + re-run Configure |
| A7 | Keyword not supported: `host` in importer | Target CS must be Npgsql; do not rewrite `Password=`→`PWD=` on PG CS |
| B1 | Feature missing on PG (raw T-SQL updater) | Expected for Demo slim allowlist — expand updater safety before Prod cutover |

---

## Step 1 — Download

**Preferred under SSH (verified):** binaries zip → extract to `C:\PostgreSQL\16` ([reference.md](./reference.md)).

**Automated EDB path (console / RDP often OK):**

```powershell
# On Windows Server (elevated), after demo.env has PG_PASSWORD:
cd C:\visa2026-deploy\iis   # or repo scripts\windows-iis
.\Install-PostgreSqlForVisa2026.ps1 -EnvFile C:\visa2026\env\demo.env
```

Downloads default: `C:\visa2026\downloads\postgresql-16-windows-x64.exe` (EDB 16.x URL in script params).

---

## Step 2 — Install / service

- **Binaries path:** `initdb` → `pg_ctl register` → start service `postgresql-x64-16` (details in [reference.md](./reference.md)).
- **EDB path:** unattended flags in `Install-PostgreSqlForVisa2026.ps1` (`--mode unattended`, `--servicename postgresql-x64-16`, port from `PG_PORT`).

Verify:

```powershell
Get-Service postgresql*
& "C:\PostgreSQL\16\bin\psql.exe" -h localhost -p 5432 -U postgres -d postgres -c "SELECT version();"
```

---

## Step 3 — Configure for Visa2026 Demo

In `C:\visa2026\env\demo.env` (never commit):

```ini
EFCORE_PROVIDER=Postgres
PG_HOST=localhost
PG_PORT=5432
PG_USER=postgres
PG_PASSWORD=<strong>
DB_NAME=visa2026_demo
```

Then:

```powershell
.\Configure-Visa2026Production.ps1 -Profile Demo
```

Expected connection fragment: `Host=…;Port=…;Database=visa2026_demo;…;Persist Security Info=True;EFCoreProvider=Postgres`.

Optional: allow LAN clients to `5432` only if required (Demo app uses localhost — usually **no** firewall open).

---

## Step 4 — Create empty database

`Install-PostgreSqlForVisa2026.ps1` creates `DB_NAME` if missing. Manual:

```powershell
$env:PGPASSWORD = "<from demo.env>"
& "C:\PostgreSQL\16\bin\createdb.exe" -h localhost -p 5432 -U postgres -E UTF8 visa2026_demo
```

Greenfield schema: deploy Demo with **`-ForceUpdate -EnableForceXafDbUpdate`**, then remove ForceUpdate — IIS skill, not this one.

---

## After PG is ready (handoff)

1. [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) — Demo publish + DB update.
2. Smoke: `http://<server>:8081/LoginPage` (or HTTPS if enabled).
3. Import work: [visa2014-to-visa2026-import](../visa2014-to-visa2026-import/SKILL.md) (in-process DataImporter against Demo PG CS).

Append **[learnings.md](./learnings.md)** after any verified install/config change.
