# visa2026-windows-docker-desktop — learnings

Append-only. Newest first.

## Entries

### 2026-08-03 — Docker Desktop production on `.25` (`E:\visa2026-prod`, `:80`)

- **Layout:** `E:\visa2026-prod`, project `visa2026-prod`, image `1.0.0.644`, `DB_NAME=visa2026_prod_docker`, `PG_HOST_PORT=5435`, `APP_PORT=80`.
- **Port 80:** stopped IIS `Visa2026-Prod` (and legacy `Visa2026` already stopped) so Docker could bind `:80`. IIS Staging `:8080` left running; Docker staging `:8081` unchanged.
- **Compose CLI:** on this host use `E:\Docker\resources\bin\docker-compose.exe` (`docker compose` plugin unavailable). Pull with stub cred helpers + `DOCKER_CONFIG` under the client folder (same as staging).
- **First boot:** `compose run … --updateDatabase --forceUpdate --silent` then `up -d app`. Allow ~40–60s after recreate before LoginPage smoke (early curl can return `000`).
- **Smoke:** `http://10.100.128.25/LoginPage` → **HTTP 200**; staging still `http://10.100.128.25:8081/LoginPage` → 200. Empty Docker prod DB (lookups seeded only) — business import is a separate skill.
- **FORCE_XAF:** removed from `.env.prod` after ModuleUpdaters ran.

### 2026-08-03 — Skill promoted; local pilot waived

- **Decision:** Create this skill after **one** verified on-prem Desktop pilot (`.25` staging). Local `C:\visa2026-pilot` waived — workstation disk insufficient; staging shipped on `10.100.128.25` instead.
- **Pilot green:** LoginPage **HTTP 200** on `http://10.100.128.25:8081` (moved from 9080 for LAN ACL); image `1.0.0.644`.
- **IIS:** Demo site stopped to free `:8081`; IIS Staging `:8080` / Prod unchanged. IIS skill remains supported.

### 2026-08-03 — On-prem Desktop staging (`.25`) operational notes

- **Docker install path:** `E:\Docker` (not `C:\Program Files\Docker`). Use full path or prepend `E:\Docker\resources\bin`.
- **SSH `docker pull`:** fails with wincred “logon session does not exist”. Fix: stub `docker-credential-desktop`/`wincred` ahead of Docker bin + `DOCKER_CONFIG` without interactive store; or pull under RDP.
- **First boot:** empty PG → `ApplicationTypes` missing until `--updateDatabase --forceUpdate` (or `FORCE_XAF_DB_UPDATE` once).
- **LAN:** host firewall alone insufficient if perimeter ACL blocks port (9080 blocked; 8081 open on this network). Prefer ports already allowed or open ACL.
- **Import:** Demo sync host can target Docker if `VISA2026_DEMO_SQL_CONNECTION` → container PG; **DataImporter Module must match app schema** (old DI still queried `OrganizationTypeID` after column dropped).
