# visa2026-postgresql — chat openers

Copy into chat (or `@` the skill folder):

## Install / configure

- `@.cursor/skills/visa2026-postgresql/` Install PostgreSQL 16 on the IIS host for Demo (`visa2026_demo`). Prefer binaries zip if SSH.
- Download and install Postgres for Visa2026 Demo; create empty DB; set `demo.env` `EFCORE_PROVIDER=Postgres`.
- EDB installer failed over SSH — use zip + initdb path from the PostgreSQL skill.

## After install

- Wire Demo to Postgres and run Configure + ForceUpdate (hand off to `visa2026-windows-iis-deploy`).
- Verify `psql` at `C:\PostgreSQL\16\bin` and service `postgresql-x64-16`.

## Not for these openers

- IIS publish / recycle → `visa2026-windows-iis-deploy`
- VISA2014 file/scalar import → `visa2014-to-visa2026-import`