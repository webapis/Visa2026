# Deployment lifecycle experience (working notes)

**Purpose:** Capture repeatable steps from real deploy / Docker / DB troubleshooting. The executable Agent workflow lives in **`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`**; keep long examples and new incidents **here**, then distill into that skill when a pattern repeats (see **How we maintain this** at the end).

**Related:** [ENVIRONMENTS.md](./ENVIRONMENTS.md), [PRODUCTION_DEPLOYMENT_RUNBOOK.md](./PRODUCTION_DEPLOYMENT_RUNBOOK.md), [DEBUGGING_DOCKER_DEPLOYMENTS.md](./DEBUGGING_DOCKER_DEPLOYMENTS.md), [scripts/README.md](../scripts/README.md), Agent skill [`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`](../.cursor/skills/visa2026-lifecycle-docker/SKILL.md).

---

## 1. Where commands run

- **`docker compose`** runs on the **host** (Windows PowerShell with Docker Desktop, or SSH session on the droplet)—**not** inside the `app` container.
- Use a **real** compose invocation: `-p`, `--env-file`, `-f docker-compose.*.yml`. Do **not** paste placeholder text like `docker compose ...` (Docker treats `...` as invalid).

---

## 2. Symptom: generic “Application Error” in the browser

- XAF Blazor shows **“An error occurred while processing your request.”**
- Often the **real** exception is only in **container logs**, not in the modal.

### 2.1 Read app logs (local example)

```powershell
docker logs visa2026-local-app-1 --tail 200
```

Adjust the container name (`docker ps` / Docker Desktop). Common pattern: `<project>-app-1`.

### 2.2 Typical root cause we hit

- **`SqlException`: `Invalid column name '…'`** (example: new `Person` columns such as `DeclareFamilyMembersOnVisa`, `VisaApplicationFamilyMembersText`).
- Meaning: **application model / image is ahead of the database schema** on the SQL volume you attached. The UI loads entities; EF generates SQL that the database cannot satisfy.

---

## 3. Fix: one-off XAF database update (same image, same env as `app`)

When the web process cannot reliably complete startup UI, run the built-in updater as a **one-off** container so it uses the **same** connection string and compose env as the stack.

From **repository root**:

```powershell
cd C:\Users\webap\Documents\GitHub\Visa2026

docker compose -p visa2026-local --env-file .env.local -f docker-compose.prod.yml run --rm --no-deps app --updateDatabase --forceUpdate
```

- **`--forceUpdate`:** ask XAF to update even when version checks might otherwise skip work (helps when schema drift and `ModuleInfo` disagree).
- The tool may prompt **“press Enter”** twice (backup reminder + disconnect users). For automation, prefer **`--silent`** if your build supports it (see `Visa2026.Blazor.Server` `--help` / `Program.cs`).

**After a successful update**, recreate the long-running app container:

```powershell
docker compose -p visa2026-local --env-file .env.local -f docker-compose.prod.yml up -d --force-recreate --no-deps app
```

**Droplet (Linux):** same idea from `~/visa2026` with `visa2026-prod` / `.env.prod` / `docker-compose.prod.yml` (or dev equivalents).

---

## 4. “Normal” startup update vs one-off CLI

- **Release** defaults to **`UpdateOldDatabase`**: on startup, XAF may update the DB when it decides the database is **behind** the app.
- If the DB looks **current** but **schema is still wrong**, startup may not run enough work—use **`--updateDatabase --forceUpdate`** as above, or the one-shot **`FORCE_XAF_DB_UPDATE=true`** flow described in [ENVIRONMENTS.md](./ENVIRONMENTS.md) (then **remove** the flag).

---

## 5. Image tag vs database (deploy ordering)

- **`APP_IMAGE_TAG`** in `.env.*` must match a tag that **exists** on Docker Hub (or your registry) **before** you expect `docker compose pull` to succeed.
- CI tags images from **`Visa2026.Module` `AssemblyVersion`** (see `.github/workflows/publish-to-docker-hub.yml`). Do not set `APP_IMAGE_TAG` to a new version until that tag is published (or use `latest` only with full understanding of mutability).

---

## 6. Operational scripts (Windows → droplet)

- **`droplet-scripts/update-app.ps1`:** uploads compose/env, pulls image, restarts **app** only. Uses **`$LOCAL_REPO = Split-Path -Parent $PSScriptRoot`** (any clone path). Optional **`-IdentityFile`** for SSH keys.
- **`scripts/local/Set-ForceXafDbUpdate.ps1`:** local env edit + recreate **local** `app`. Remote variant: **`droplet-scripts/Set-ForceXafDbUpdate.ps1`** (if present in your branch).

---

## 7. UI hint: `FORCE_XAF_DB_UPDATE`

When the env var is **`true`**, the Blazor host can show a **bottom banner** (see `Visa2026.Blazor.Server` / `_Host.cshtml`) so operators see that **`UpdateDatabaseAlways`** is active (slow startups until the flag is cleared).

---

## 8. Agent skill (implemented)

The checklist below is encoded in **`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`** (with **MCP** guidance and **reference.md** command blocks):

1. Verify **host** + **repo root** + **project/env/compose file** names.
2. On UI failure: **`docker logs <app-container> --tail N`** first.
3. On **`Invalid column name`**: **`docker compose run --rm --no-deps app --updateDatabase --forceUpdate`** with stack env; then **`up -d --force-recreate --no-deps app`**.
4. Document when to use **`FORCE_XAF_DB_UPDATE`** vs CLI updater vs bump **`AssemblyVersion`** / CI publish.
5. Never document real secrets; reference `.env.*.example` only.

---

## Plan: from experience to skills (team agreement)

During development we **collect** recurring work that is not “just coding”: deploy steps, errors, debugging, and manual checks. We turn that into **durable guidance** in two layers—**docs** (story + detail) and **Agent skills** (short procedures)—with **AI drafting** and a **developer reviewing** merges (especially prod, secrets, destructive Docker).

### Principles

| Principle | Meaning |
|-----------|---------|
| **One skill ≈ one task** | Each new `SKILL.md` under `.cursor/skills/<name>/` targets a **single workflow family** (e.g. lifecycle/Docker, CI triage, commit-after-verify). Do not grow one file into a catch-all; add a **new folder** for a new recurring task. |
| **Docs first, skill second** | **First incident:** add a **dated subsection** here or in [DEBUGGING_DOCKER_DEPLOYMENTS.md](./DEBUGGING_DOCKER_DEPLOYMENTS.md) (observability / droplet vs local). **Skills** stay scannable; long narrative lives in **docs**. |
| **AI + developer** | Assistant proposes edits to `SKILL.md` / `reference.md` from logs and doc notes; **human** approves before commit (safety, tone, triggers). |
| **Cross-link, don’t duplicate** | A skill may **point** to another skill (“then use `ci-failed-triage`”). That is orchestration, not merging unrelated tasks into one file. |
| **YAML `description` = discovery** | List **symptoms and keywords** so the right skill is chosen automatically. |

### Funnel (when to promote into a skill)

1. **First time** you hit a new deploy/Docker/DB issue: add a short **dated subsection** here (or in [DEBUGGING_DOCKER_DEPLOYMENTS.md](./DEBUGGING_DOCKER_DEPLOYMENTS.md) if it is “droplet vs local” observability).
2. **Second occurrence:** ensure the relevant **skill** links to that doc section (still avoid pasting long prose into `SKILL.md`).
3. **Third repeat or high-risk path:** add a **numbered playbook** to the appropriate **single-task** skill (e.g. **`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`**) and a line in **reference.md** if it is command-heavy.

Optional **periodic** pass (e.g. monthly): trim skills that grew too long; move bullets to docs and leave links.

### Scope of this doc

This file is the **deployment / Docker / DB** incident log and funnel entry. Other recurring workflows get their **own** doc and/or skill (e.g. importer seeding, PDF mapping) when they mature—same rules above.

---

*Last consolidated from troubleshooting notes (local `visa2026-local`, schema drift on `People`).*
