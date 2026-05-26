# Visa2026 on company Linux server (Ubuntu)

Runbook for deploying Visa2026 on a **company LAN Ubuntu VM** using **Docker Engine**, **Compose**, and **`docker-compose.prod.yml`**.

**Prerequisites:** [ON_PREM_PREREQUISITES.md](./ON_PREM_PREREQUISITES.md) (§ Linux host)

**Agent skills:** [setup-docker-engine](../.cursor/skills/setup-docker-engine/SKILL.md) · [setup-openssh-server](../.cursor/skills/setup-openssh-server/SKILL.md) (optional SSH)

**Scripts:** [scripts/linux/README.md](../scripts/linux/README.md)

**Not this path:** [ON_PREM_WINDOWS_SERVER.md](./ON_PREM_WINDOWS_SERVER.md) (legacy Windows Server + WSL — deprecated for new deploys).

**Cloud alternative:** DigitalOcean droplet — `droplet-scripts/` and [visa2026-droplet-prod-deploy](../.cursor/skills/visa2026-droplet-prod-deploy/SKILL.md).

---

## Architecture

```text
LAN clients  -->  http://<server-ip>:80
                        |
                 Ubuntu 22.04/24.04 LTS
                        |
              Docker Engine (systemd)
                        |
         docker compose (visa2026-prod)
              |                |
           app:8080      sqlserver (volume)
```

- **Linux containers only** (`webapia/visa2026`, MCR SQL).
- **No** WSL, **no** Windows portproxy, **no** Docker Desktop on the server.
- Official install: [Docker Engine on Ubuntu](https://docs.docker.com/engine/install/ubuntu/).

---

## Files on the server

| Path | Required |
|------|----------|
| `/opt/visa2026/docker-compose.prod.yml` | Yes |
| `/opt/visa2026/.env.prod` | Yes |
| `/opt/visa2026/remote-compose-sql-up.sh` | Recommended |
| `/opt/visa2026/docker-compose.restart.override.yml` | Optional |

Copy from repo: `scripts/linux/*`, root `docker-compose.prod.yml`, `.env.prod.example` → `.env.prod`.

---

## Deployment phases

### Phase 0 — Host ready

- [ ] Ubuntu **22.04** or **24.04** LTS, **8+ GB RAM**, **100+ GB** free disk
- [ ] Outbound: Docker Hub + `mcr.microsoft.com`
- [ ] Inbound firewall: **TCP 80** (or `APP_PORT`); optional **TCP 22** for SSH admin ([setup-openssh-server](../.cursor/skills/setup-openssh-server/SKILL.md))

### Phase 0b (optional) — OpenSSH for remote admin

```bash
sudo bash /opt/visa2026/ensure-openssh-server.sh
```

From admin PC: `ssh user@<server-ip>`

### Phase 1 — Docker Engine

Follow [setup-docker-engine Step 1](../.cursor/skills/setup-docker-engine/SKILL.md#step-1--install-docker-engine-ubuntu).

Verify: `docker run --rm hello-world`

### Phase 2 — Deploy Visa2026

```bash
cd /opt/visa2026
sudo bash remote-compose-sql-up.sh
```

Verify:

```bash
docker compose -p visa2026-prod --env-file .env.prod ps
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1/LoginPage
```

Browser: `http://<server-ip>/LoginPage` — login **Admin** / empty password (change after).

### Phase 3 — One-shot schema (first deploy only)

In `.env.prod`: `FORCE_XAF_DB_UPDATE=true`, recreate app once, then remove the line and recreate again. See [ENVIRONMENTS.md](./ENVIRONMENTS.md).

---

## App updates

```bash
cd /opt/visa2026
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull app
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d --no-deps app
```

Or from Windows: `droplet-scripts/update-app.ps1` if the Linux host is the droplet; for LAN VM use SSH + commands above.

---

## Migrate from Windows Server + WSL

See [ON_PREM_STABILITY_AND_CUTOVER.md](./ON_PREM_STABILITY_AND_CUTOVER.md) §2 (backup `.bak`, restore on Linux).

---

## Troubleshooting

| Symptom | Action |
|---------|--------|
| SQL **4060** | Run `remote-compose-sql-up.sh` (SQL before app) |
| Containers **Exited** | `docker compose ps`; `docker logs visa2026-prod-app-1 --tail 80` |
| Browser refused | `ufw`/firewall **TCP 80**; `curl` on host first |
| Schema drift | `FORCE_XAF_DB_UPDATE=true` once — [ENVIRONMENTS.md](./ENVIRONMENTS.md) |

Skill scenarios: [setup-docker-engine](../.cursor/skills/setup-docker-engine/SKILL.md)
