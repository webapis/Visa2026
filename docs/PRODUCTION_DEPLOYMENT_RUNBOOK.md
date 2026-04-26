# Visa2026 Production Deployment Runbook

This runbook defines a safe deployment process for the Droplet-based Docker setup in this repository.

It is designed to prevent:
- production outage from risky rollout steps
- accidental production data loss/corruption
- untested schema/data changes reaching production

---

## 1) Deployment Principles

- Deploy to **testing/staging first**, then production.
- Promote the **same image tag** from testing to production (no rebuild at production time).
- Separate **application deploy** from **data seeding/import**.
- Treat production database as critical state: backup first, verify after.

---

## 2) Script Safety Matrix

Use the scripts in `droplet-scripts/` as follows:

- **Safe for normal production app updates**
  - `update-app.ps1`
  - `update-app.sh`

- **Use with caution (data operations)**
  - `seed-data.ps1` (lookup seed is safe when intentional; optional YAML import is business-data mutation)

- **Never use on production**
  - `fresh-install.ps1`
  - `fresh-install.sh`
  - Reason: runs `docker compose down -v` and `docker system prune -af --volumes`, which can destroy persistent data.

---

## 3) Pre-Deployment Checklist (Production)

Before each production release:

1. Confirm release passed testing/staging.
2. Confirm deployment window and rollback owner.
3. Take a full SQL backup and verify file exists and is non-trivial size.
4. Record currently running app image tag (`before` tag).
5. Confirm Droplet health:
   - free disk space
   - Docker daemon healthy
   - SQL container healthy

If any item fails, do not deploy.

---

## 4) Standard Production Deployment Flow (No Data Import)

This is the default flow for regular code releases.

### From Windows operator machine

```powershell
# 1) Sync compose/env/scripts to Droplet and run safe app update
.\droplet-scripts\update-app.ps1
```

`update-app.ps1` uploads `docker-compose.yml` + `.env`, then runs `update-app.sh`, which:
- pulls latest app image
- restarts only `app` (`--no-deps`)
- leaves SQL Server/data volume untouched

### Post-deploy smoke checks

1. Login to app UI/API.
2. Check one read path and one write path.
3. Confirm no 5xx spike in logs.
4. Confirm app container stable (no restart loop).

---

## 5) Database Backup and Recovery Basics

At minimum, do this before production deployment:

- create SQL backup (`.bak`) and store with timestamp
- copy backup off-droplet (DO Spaces/S3 or secure remote store)
- keep retention policy (daily + weekly snapshots)

Recommended operational rule:
- if migration is risky or irreversible, verify restore in staging before production rollout.

---

## 6) Schema Migration Strategy

Use **expand/contract** to reduce rollback risk:

- **Expand release**
  - additive DB changes only (new nullable columns/tables/indexes)
  - app remains compatible with old and new schema

- **Contract release (later)**
  - remove old columns/constraints only after traffic/data has moved safely

Avoid destructive schema operations in the same release as major app logic changes.

---

## 7) Data Seeding/Import Policy for Production

Production-safe default:
- run lookup seed only when required
- run YAML business-data import only with explicit approval/change ticket

Use:

```powershell
# Interactive script:
# - always runs lookup seed
# - asks yes/no for optional data.yaml import
.\droplet-scripts\seed-data.ps1
```

Important:
- Do not run seeding as part of every code deploy.
- Treat YAML import as data migration, not application rollout.

---

## 8) Rollback Runbook

If deployment fails:

1. Roll back app image to previous known-good tag.
2. Restart app container only.
3. Validate smoke checks.

If bad migration/data change already applied:

1. Decide between forward-fix vs restore.
2. If restore required, restore latest verified backup.
3. Re-point/restart app and validate critical paths.

Document incident timeline and root cause after service is stable.

---

## 9) Suggested Release Cadence

- Development: frequent, flexible seeding allowed.
- Testing/Staging: mirror production compose/env and rehearse deploy + rollback.
- Production: controlled windows, backup-first, app deploy separate from data import.

---

## 10) Quick Do/Don't

Do:
- use `update-app.ps1` for normal production updates
- backup database before each production release
- run smoke checks after each deployment

Don't:
- run `fresh-install.*` in production
- auto-import `data.yaml` during every deployment
- combine untested schema + data + app changes in one big release

