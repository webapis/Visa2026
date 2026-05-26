# scripts/linux — company Ubuntu server (on-prem LAN)

**Agent skill:** [setup-docker-engine](../../.cursor/skills/setup-docker-engine/SKILL.md)

**Runbook:** [docs/ON_PREM_LINUX_SERVER.md](../../docs/ON_PREM_LINUX_SERVER.md)

**Not this folder:** `scripts/on-prem/` (legacy **Windows Server + WSL**), `droplet-scripts/` (DigitalOcean), `scripts/local/` (dev PC).

## Deploy layout on the server

```text
/opt/visa2026/
  docker-compose.prod.yml      # from repo root
  docker-compose.restart.override.yml   # optional; from this folder
  .env.prod                    # from .env.prod.example (secrets — never commit)
  remote-compose-sql-up.sh     # from this folder
```

## Allowlist (setup-docker-engine)

| File | Purpose |
|------|---------|
| `remote-compose-sql-up.sh` | SQL-first `compose up` (avoids DB 4060) |
| `docker-compose.restart.override.yml` | `restart: unless-stopped` for app + sqlserver |

Repo root (not in this folder): `docker-compose.prod.yml`, `.env.prod.example`

## Quick start

```bash
sudo mkdir -p /opt/visa2026
# copy compose, .env.prod, scripts from repo
cd /opt/visa2026
sudo bash remote-compose-sql-up.sh
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1/LoginPage
```
