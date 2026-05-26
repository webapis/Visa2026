# setup-docker-engine — command reference (Ubuntu on-prem)

Runbook: [docs/ON_PREM_LINUX_SERVER.md](../../../docs/ON_PREM_LINUX_SERVER.md)

Skill: [SKILL.md](./SKILL.md) · **Maturity:** [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md)

Deploy root: **`/opt/visa2026`**

Official install: [Docker Engine on Ubuntu](https://docs.docker.com/engine/install/ubuntu/)

---

## Step 1 — Docker Engine

See [SKILL.md Step 1](./SKILL.md#step-1--install-docker-engine-ubuntu) or Docker docs.

**Verify:**

```bash
docker run --rm hello-world
sudo systemctl status docker --no-pager
```

---

## Step 2 — Deploy files

On admin workstation (copy to server):

```bash
scp docker-compose.prod.yml user@<server>:/opt/visa2026/
scp .env.prod user@<server>:/opt/visa2026/
scp scripts/linux/remote-compose-sql-up.sh scripts/linux/docker-compose.restart.override.yml user@<server>:/opt/visa2026/
```

On server:

```bash
sudo mkdir -p /opt/visa2026
cd /opt/visa2026
sudo bash remote-compose-sql-up.sh
```

**Manual compose (not recommended for first start):**

```bash
cd /opt/visa2026
docker compose -p visa2026-prod --env-file .env.prod \
  -f docker-compose.prod.yml -f docker-compose.restart.override.yml up -d
```

**Status / logs:**

```bash
docker compose -p visa2026-prod --env-file .env.prod ps -a
docker compose -p visa2026-prod --env-file .env.prod logs app --tail 100
docker compose -p visa2026-prod --env-file .env.prod logs sqlserver --tail 50
```

**HTTP check:**

```bash
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1/LoginPage
```

---

## App-only update

```bash
cd /opt/visa2026
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull app
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d --no-deps app
```

---

## FORCE_XAF_DB_UPDATE

```bash
cd /opt/visa2026
# Enable: add FORCE_XAF_DB_UPDATE=true to .env.prod, then:
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d --force-recreate app
# Disable: remove line from .env.prod, then recreate app again
```

See [ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md).

---

## Firewall (ufw example)

```bash
sudo ufw allow 80/tcp
sudo ufw status
```

---

## SQL backup (migration / maintenance)

```bash
cd /opt/visa2026
SA_PASSWORD="$(grep -E '^SA_PASSWORD=' .env.prod | cut -d= -f2-)"
DB_NAME="$(grep -E '^DB_NAME=' .env.prod | cut -d= -f2-)"
docker exec visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C \
  -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'/var/opt/mssql/data/${DB_NAME}.bak' WITH INIT, COMPRESSION"
docker cp visa2026-prod-sqlserver-1:/var/opt/mssql/data/${DB_NAME}.bak ./${DB_NAME}.bak
```
