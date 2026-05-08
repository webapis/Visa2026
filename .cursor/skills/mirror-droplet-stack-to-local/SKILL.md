---
name: mirror-droplet-stack-to-local
description: >-
  Mirror droplet stack configuration to local: sync compose file selection, project names, image tags, and non-secret env variables so local Docker closely matches droplet runtime. Does NOT copy production secrets; DB mirroring is handled by mirror-droplet-db-to-local.
disable-model-invocation: true
---

# Mirror droplet stack → local (non-DB parity)

## Goal

Make your **local** Docker stack behave like the **droplet** stack by aligning:

- Compose file (`docker-compose.prod.yml` vs `docker-compose.dev.yml`)
- Compose project name (droplet typically `-p visa2026-prod`; local dev should remain `-p visa2026-dev`)
- Image tags (`APP_IMAGE_TAG`, `IMPORTER_IMAGE_TAG`, `SQLSERVER_IMAGE`)
- Non-secret env settings (ports, DB name, feature flags)

**Out of scope (separate skills / workflows):**

- **Database content**: use `mirror-droplet-db-to-local`
- **Secrets**: never copy prod secrets into git; use `.env.*.example` and set secrets locally
- **Destructive compose**: do not run `down -v` on prod stacks without explicit intent

## Quick checklist (local vs droplet)

- **Droplet**: use `.env.prod` + `docker-compose.prod.yml` + `-p visa2026-prod`
- **Local dev**: use `.env.dev` + `docker-compose.dev.yml` + `-p visa2026-dev`
  
**Rule for this repo:** keep **one** local stack: `visa2026-dev`. Do not introduce `visa2026-local` / `visa2026-prodlike` project names on developer PCs.

## What to ask the user for (minimal)

- Which droplet stack: `prod` or `dev`
- Any local port conflicts (80 / 8081 / 1433)

## Deterministic steps (non-destructive)

1) Identify active stacks on the host:

```powershell
docker compose ls
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

2) Align local compose invocation (keep local project name `visa2026-dev`; change only env/compose/tag values as needed):

```powershell
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d
```

3) Verify local is pulling the expected tag (do not change tags blindly):

```powershell
docker compose -p <project> --env-file <env> -f <compose> config | findstr /i "image:"
```

4) Restart only `app` when changing env/tag:

```powershell
docker compose -p <project> --env-file <env> -f <compose> up -d --no-deps --force-recreate app
```

## When to escalate to DB mirror

If logs show schema drift (e.g. `Invalid column name`) and the local DB volume is behind, recommend:

- Run `--updateDatabase --forceUpdate` (local stack), OR
- If updater is slow/unreliable locally, use **`mirror-droplet-db-to-local`** to snapshot the droplet DB into local.

