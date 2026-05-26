---
name: setup-docker-engine
description: >-
  Install Docker Engine on Ubuntu and deploy Visa2026 prod from Docker Hub on a company
  Linux server (LAN VM). Uses scripts/linux/ and docker-compose.prod.yml. Accumulates
  experience in learnings.md. Not Windows Server/WSL (legacy scripts/legacy/on-prem-windows/). For
  DigitalOcean use visa2026-droplet-prod-deploy. Never scripts/local for prod deploy.
disable-model-invocation: false
---

# setup-docker-engine — Docker Engine on Ubuntu + Visa2026 compose

**Scope:** **Docker Engine** + **Compose plugin** on **Ubuntu 22.04/24.04 LTS**, then **`visa2026-prod`** from **Docker Hub**.

**Canonical runbook:** [docs/ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md)

**Official Docker install:** [Install Docker Engine on Ubuntu](https://docs.docker.com/engine/install/ubuntu/)

## Not this skill

| Target | Use instead |
|--------|-------------|
| **Windows Server + WSL** | Legacy — [scripts/legacy/on-prem-windows/](../../../scripts/legacy/on-prem-windows/) + [legacy-on-prem-windows-setup](../legacy-on-prem-windows-setup/SKILL.md) (archived path) |
| **DigitalOcean droplet** | [visa2026-droplet-prod-deploy](../visa2026-droplet-prod-deploy/SKILL.md) + `droplet-scripts/` |
| **Developer PC** | Docker Desktop + `scripts/local/` |
| **Remote admin (SSH)** | [setup-openssh-server](../setup-openssh-server/SKILL.md) — optional before deploy |

## Hard rules

- **Do not** install **Docker Desktop** on the production Linux server (dev PCs only).
- **Do not** use `scripts/legacy/on-prem-windows/*.ps1` or WSL commands for new on-prem deploys.
- **Do not** run `fresh-install.sh` / `fresh-install.ps1` on production (wipes DB volume).
- Use **`remote-compose-sql-up.sh`** for deploy/recovery (SQL before app — avoids **4060**).

## Requirements doc

[ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md) — § **Linux host (on-prem)**

### Host ready when

- [ ] Ubuntu **22.04** or **24.04** LTS (x64), **8+ GB RAM**, **100+ GB** free disk
- [ ] SSH or console access with **sudo**
- [ ] Outbound HTTPS to `download.docker.com`, `registry-1.docker.io`, `mcr.microsoft.com`
- [ ] Inbound **TCP 80** (or chosen `APP_PORT`) from LAN

## Scenarios (install + compose)

| # | Scenario | Signal | Fix |
|---|----------|--------|-----|
| A1 | **Outbound blocked** | `apt`/`docker pull` timeout | IT allow list — [ON_PREM_PREREQUISITES §3](../../../docs/ON_PREM_PREREQUISITES.md#3-network-requirements) |
| A2 | **Docker daemon down** | `Cannot connect to docker daemon` | `sudo systemctl enable --now docker` |
| A3 | **Disk full** | `no space left on device` | Free disk under `/var/lib/docker` |
| B1 | **Image pull slow** | First pull 15+ min | Wait; verify A1 |
| C1 | **Missing `.env.prod`** | SQL/app exit; auth errors | Copy from `.env.prod.example`; set `SA_PASSWORD`, `DEVEXPRESS_LICENSEKEY` |
| C2 | **SQL 4060** | Cannot open database | `sudo bash remote-compose-sql-up.sh` |
| C3 | **Firewall** | `curl` OK locally, LAN refused | `ufw allow 80/tcp` or corporate firewall |
| C4 | **Port 80 in use** | Bind error on compose | Change `APP_PORT` in `.env.prod` |
| C5 | **`FORCE_XAF` left on** | Slow every restart | Remove from `.env.prod` after first healthy login |
| D1 | **Docker Desktop on server** | Wrong tooling / licensing | Engine only via `apt` — [Engine install Ubuntu](https://docs.docker.com/engine/install/ubuntu/) |

**Step 1 success:** `docker --version`, `docker compose version`, **Hello from Docker!**

**Step 2 success:** `compose ps` → **app** + **sqlserver** **Up**; `http://<server-ip>/LoginPage` → 200/302

---

## Script allowlist (strict)

Only these under [`scripts/linux/`](../../../scripts/linux/):

| File | Step |
|------|------|
| [remote-compose-sql-up.sh](../../../scripts/linux/remote-compose-sql-up.sh) | 2 — SQL-first deploy |
| [docker-compose.restart.override.yml](../../../scripts/linux/docker-compose.restart.override.yml) | 2 — optional restart policy |

Repo root (copy to server, not under `scripts/linux/`):

| File | Step |
|------|------|
| [docker-compose.prod.yml](../../../docker-compose.prod.yml) | 2 |
| [.env.prod.example](../../../.env.prod.example) → `/opt/visa2026/.env.prod` | 2 |

Manifest: [scripts/linux/README.md](../../../scripts/linux/README.md)

### Forbidden for this skill

- `scripts/legacy/on-prem-windows/**` (Windows/WSL — legacy)
- `droplet-scripts/**` unless target **is** the droplet (use **visa2026-droplet-prod-deploy**)
- `scripts/local/**` for production deploy
- `wsl`, `Install-WslDockerEngine.ps1`, `Start-Visa2026Compose.ps1`

### Allowed without being repo scripts

- Official `apt` Docker install per [Docker docs](https://docs.docker.com/engine/install/ubuntu/)
- `docker compose` / `docker logs` on the server for verify/triage

## Goal (two steps)

| Step | What you get |
|------|----------------|
| **1** | **Docker Engine** + Compose; `hello-world` OK; `docker` enabled on boot |
| **2** | **visa2026-prod** **Up** at `http://<server-ip>:<APP_PORT>` |

**Commands:** [reference.md](./reference.md) · **Experience:** [learnings.md](./learnings.md) · **Maturity:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md) (shared loop; name is historical)

### Chat openers

- `@.cursor/skills/setup-docker-engine/` — install Docker on Ubuntu, deploy Visa2026 prod on LAN Linux server.
- Linux VM prod deploy, `docker compose`, `4060`, firewall port 80.
- Migrate from Windows Server WSL to Ubuntu.

---

## Step 1 — Install Docker Engine (Ubuntu)

**Where:** Ubuntu server (SSH), user with **sudo**.

```bash
# Official path — see https://docs.docker.com/engine/install/ubuntu/
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

sudo tee /etc/apt/sources.list.d/docker.sources <<EOF
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}")
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF

sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo systemctl enable --now docker
```

**Post-install (optional):** run `docker` without sudo — [Linux postinstall](https://docs.docker.com/engine/install/linux-postinstall/).

**Verify:**

```bash
docker --version
docker compose version
docker run --rm hello-world
```

### Step 1 checklist

- [ ] **Hello from Docker!**
- [ ] `systemctl is-active docker` → **active**

---

## Step 2 — Deploy Visa2026 (Docker Hub)

**Layout:** `/opt/visa2026/` — see [scripts/linux/README.md](../../../scripts/linux/README.md).

```bash
sudo mkdir -p /opt/visa2026
# Copy: docker-compose.prod.yml, .env.prod, scripts/linux/* → /opt/visa2026/
cd /opt/visa2026
sudo bash remote-compose-sql-up.sh
```

**Verify:**

```bash
docker compose -p visa2026-prod --env-file .env.prod ps
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:${APP_PORT:-80}/LoginPage
```

From LAN: `http://<server-ip>/LoginPage` — **Admin** / empty password.

**First deploy schema:** set `FORCE_XAF_DB_UPDATE=true` in `.env.prod`, `docker compose ... up -d --force-recreate app`, confirm login, remove flag, recreate app. See [ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md).

### Step 2 checklist

- [ ] Both containers **Up**
- [ ] HTTP login page from LAN
- [ ] `FORCE_XAF_DB_UPDATE` removed after success

---

## Agent workflow

1. **Read** [learnings.md](./learnings.md).
2. Confirm **Linux** target (not WSL) — if user says Windows Server, point to legacy [ON_PREM_WINDOWS_SERVER.md](../../../docs/legacy/ON_PREM_WINDOWS_SERVER.md) or recommend Ubuntu per [ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md).
3. **Step 1** — Docker Engine via official Ubuntu install.
4. **Step 2** — copy deploy files; `remote-compose-sql-up.sh`; verify `ps` + HTTP.
5. One command per message if user wants approval mode.
6. **Append** [learnings.md](./learnings.md) after verified success.

---

## Investigation map (quick)

| Signal | Likely |
|--------|--------|
| `docker: command not found` | Step 1 not done |
| `Cannot connect to docker daemon` | **A2** |
| Pull timeout | **A1** |
| SQL **4060** | **C2** |
| Local `curl` OK, LAN fails | **C3** |
| Missing license / SQL auth in logs | **C1** |

---

## Continuous improvement

Append-only [learnings.md](./learnings.md); promotion rules in [MATURITY.md](../on-prem-deploy/MATURITY.md). Prefer citing scenario IDs (**A1**, **C2**, …) in entries.
