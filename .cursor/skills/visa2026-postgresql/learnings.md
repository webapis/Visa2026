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