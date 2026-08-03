# visa2026-postgresql — learnings (append-only)

**Read before** install/config work. **Append after** verified fixes only. Template: [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md).

## Entries

### 2026-07-14 — Demo PostgreSQL 16 on IIS host (`10.100.128.25`)

- **Symptom**: EDB GUI/exe installer failed under SSH (`temp_check_comspec.bat` / COM temp issues).
- **Try**: Unattended EDB exe via `Install-PostgreSqlForVisa2026.ps1`.
- **Test**: Installer exit non-zero / incomplete under remote session.
- **Fix**: **Binaries zip** → extract to `C:\PostgreSQL\16` → `initdb` (let initdb create `data`) → `pg_ctl register` service `postgresql-x64-16` → `createdb visa2026_demo`. Wire `demo.env` with `EFCORE_PROVIDER=Postgres` + `PG_*`; `Configure-Visa2026Production.ps1 -Profile Demo`.
- **Prevent**: Prefer binaries path for SSH installs; keep EDB script for interactive/RDP when it works. Document both in skill reference.
- **Skill**: visa2026-postgresql

### 2026-07-15 — Local PC PostgreSQL 16 (dev launch profile)
- **Symptom**: Need Postgres on developer Windows for F5 (not IIS Demo).
- **Try**: EDB binaries zip `postgresql-16.9-1-windows-x64-binaries.zip` → `C:\PostgreSQL\16` → `initdb -U postgres -A scram-sha-256` (let initdb create `data`) → `pg_ctl start` (service register needed elevation; `postgresql-x64-16` registered Stopped until reboot/Start-Service) → `createdb visa2026`.
- **Test**: `psql -h localhost -U postgres -d visa2026 -c "SELECT version();"` OK (16.9).
- **Fix / wire**: Launch profile `Visa2026 - PostgreSQL` in `Visa2026.Blazor.Server/Properties/launchSettings.json` with `EFCoreProvider=Postgres`; local password `Visa2026Local`; helper `scripts/local/Start-LocalPostgreSql.ps1`.
- **Prevent**: Prefer binaries zip on local too; without admin use `pg_ctl start` until service is elevated once. Do not point LocalDB and Postgres at the same DB name differently — LocalDB stays `Visa2026`, Postgres `visa2026`.
- **Skill**: visa2026-postgresql.

### 2026-07-31 — Prod PG cluster on E:; fresh visa2026_prod; remove C: data

- **Host**: `10.100.128.25`
- **Binaries** (unchanged): `C:\PostgreSQL\16`
- **Data directory**: `E:\visa2026\postgresql\16\data` (`SHOW data_directory`; service `postgresql-x64-16` `-D` on E:)
- **Action**: Dropped/recreated empty UTF8 DB `visa2026_prod` (0 public tables). Deleted leftover `C:\PostgreSQL\16\data` (~1 GB) after confirming live cluster on E:.
- **Disk**: C: free ~32 GB after delete; E: used for PG data (~185 GB free before).
- **Next**: `Run-Visa2026DbUpdateOnServer.ps1 -Profile Production -ForceUpdate` (or deploy ForceUpdate) before Import; start `Visa2026-Prod` app pool.
- **Skill**: visa2026-postgresql
