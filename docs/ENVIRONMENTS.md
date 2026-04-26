# Visa2026 Environment Split (Prod + Dev on One Host)

This guide shows how to run production and development stacks safely on the same droplet without mixing data.

---

## Files

- `docker-compose.prod.yml`
- `docker-compose.dev.yml`
- `.env.prod.example`
- `.env.dev.example`

Create real env files from examples:

```bash
cp .env.prod.example .env.prod
cp .env.dev.example .env.dev
```

Set strong values for `SA_PASSWORD` and `DEVEXPRESS_LICENSEKEY` in both files.

---

## Start Production Stack

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d
```

---

## Start Development Stack

```bash
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d
```

Default ports:
- Prod app: `80`
- Dev app: `8081`

Each stack has its own SQL data volume:
- `visa2026-prod_sqlserver_data_prod`
- `visa2026-dev_sqlserver_data_dev`

---

## Run Importer Safely

Production lookup seed:

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml --profile tools run --rm db-updater --seed-lookups-only
```

Production optional YAML import:

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml --profile tools run --rm db-updater --import-yaml-only
```

Development lookup seed:

```bash
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml --profile tools run --rm db-updater --seed-lookups-only
```

Development optional YAML import:

```bash
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml --profile tools run --rm db-updater --import-yaml-only
```

From Windows with interactive yes/no prompt for YAML import:

```powershell
.\droplet-scripts\seed-data.ps1 -Environment dev
.\droplet-scripts\seed-data.ps1 -Environment prod
```

---

## Update One Stack Without Touching the Other

Update production app only:

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull app
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d --no-deps app
```

Or from Windows:

```powershell
.\droplet-scripts\update-app.ps1 -Environment prod
```

Update development app only:

```bash
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml pull app
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d --no-deps app
```

Or from Windows:

```powershell
.\droplet-scripts\update-app.ps1 -Environment dev
```

---

## Stop/Remove

Stop one stack:

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml down
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml down
```

Do not use `down -v` on production unless you explicitly intend to delete database data.

---

## Operational Rules

- Never run dev seeding commands against prod project name/env file.
- Keep prod and dev secrets in separate env files.
- Backup production DB before releases and before production data imports.
- Do not use `fresh-install.sh` / `fresh-install.ps1` on production.
