---
name: visa2026-lifecycle-docker
description: >-
  Visa2026 dev-to-Docker lifecycle: local image build, compose deploy, container log triage,
  schema drift (Invalid column name), ModuleInfo / DatabaseUpdate, FORCE_XAF_DB_UPDATE,
  image tag mismatch (Docker Hub), droplet update, Visual Studio vs Docker behavior.
  Use when the user hits deploy/runtime issues after building Docker, or asks how to debug
  compose stacks. Orchestrates repo docs; use MCP only when it reduces round-trips.
disable-model-invocation: false
---

# Visa2026: development → Docker → deploy lifecycle

## Goal

Guide **ordered** diagnosis and fixes: **host commands** → **container logs** → **classify** (SQL / env / image / XAF) → **playbook** → **verify**. Do **not** duplicate long prose here; link **[docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)**, **[docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md)**, **[docs/DEBUGGING_DOCKER_DEPLOYMENTS.md](../../../docs/DEBUGGING_DOCKER_DEPLOYMENTS.md)**.

**Related skills (keep separate):** [.cursor/skills/ci-failed-triage/SKILL.md](../ci-failed-triage/SKILL.md) (CI red), [.cursor/skills/commit-after-verify/SKILL.md](../commit-after-verify/SKILL.md) (commit after build/test).

**Copy-paste commands:** [reference.md](./reference.md).

**How we evolve skills (one task per skill, doc-first funnel, human review):** [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md § Plan](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md).

### Chat openers (copy-paste)

Use with **`@.cursor/skills/visa2026-lifecycle-docker/`** (or rely on the skill `description` alone):

- **Local build + recreate, step-by-step with confirmation:**  
  `Coding is done—walk me through local Docker image build and container recreate; propose each step and wait for my OK.`

- **Local build + recreate, step-by-step (scripted):**  
  `Run the local Docker lifecycle scripts step-by-step and ask for my OK before each command (build → list → recreate local → logs).`

Shorter variants:

- `Local Docker: build images and recreate visa2026-local stack.`
- `@visa2026-lifecycle-docker docker logs show Invalid column name on People`

---

## 0. Approval mode (ask before each command)

When the user requests “step-by-step” or “ask for approval”, do **not** run a batch of commands.
Propose each command and wait for an explicit **OK** before running it.

Default local workstation sequence (prod-like local stack):

```powershell
.\scripts\local\lifecycle-docker\Build-DockerImages.ps1
.\scripts\local\lifecycle-docker\Docker-ListContainers.ps1
.\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1
.\scripts\local\lifecycle-docker\Docker-AppLogs.ps1 -Tail 200
```

If logs show schema drift (`Invalid column name`), insert DB update + recreate:

```powershell
.\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1 -Silent
.\scripts\local\lifecycle-docker\Recreate-App.ps1
```

## 1. Phase map (where the user is)

| Phase | Typical actions | Repo pointers |
|--------|-----------------|---------------|
| **A. IDE** | Edit Module/Blazor, F5, debug | `AGENTS.md`, `.cursor/rules/` |
| **B. Verify compile** | `dotnet build Visa2026.slnx -c Debug` | Workspace rules |
| **C. Local images** | `scripts/local/lifecycle-docker/Build-DockerImages.ps1` | [scripts/README.md](../../../scripts/README.md) |
| **D. Compose run** | `docker compose -p … --env-file … -f … up -d` | [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md) |
| **E. Runtime issues** | `docker logs`, playbooks below | This skill |

**Rule:** `docker compose` runs on the **host** (PowerShell / SSH), not inside the `app` container. Never use literal `...` as a compose argument.

---

## 2. Triage tree (do this order)

1. **Confirm stack identity:** `docker compose ls` or `docker ps` — note **project** (`visa2026-local`, `visa2026-prod`, …) and **`app` container name** (often `<project>-app-1`).
2. **Browser shows generic “Application Error”** → treat as **server exception** until proven otherwise.
3. **`docker logs <app-container> --tail 200`** (increase if needed). Find the **first** `fail:` / `SqlException` / `Error`.
4. **Classify:**
   - **`Invalid column name` / EF SQL errors** → playbook **Schema drift** (§4).
   - **`not found` on image pull** → playbook **Image tag** (§5).
   - **License / DevExpress / connection string** → env and secrets; compare `.env.*` with `.env.*.example`; see [DEBUGGING_DOCKER_DEPLOYMENTS.md](../../../docs/DEBUGGING_DOCKER_DEPLOYMENTS.md).
   - **“Works in Visual Studio, fails in Docker”** → same doc (data, env, networking, paths).
5. **Verify after fix:** `docker logs` clean on startup, hit the failing URL again; optional `ModuleInfo` check (§6 MCP / script).

---

## 3. MCP usage (when and prerequisites)

Use MCP **after** logs point in that direction—not on every incident. **Read the MCP tool schema** before calling (project `mcps/` descriptors). **Never paste** passwords, tokens, or full connection strings into chat.

| Symptom / need | Server (see `.cursor/mcp.json`) | Prerequisites |
|----------------|----------------------------------|---------------|
| XAF `DatabaseUpdateMode`, `ModuleUpdater`, compatibility, Blazor adapter behavior | **dxdocs** | Question is DX/XAF-specific |
| “Did CI publish this tag?” workflow logs, PR, Actions | **github** (Docker must be running if server uses Docker) | User PAT / access |
| `ModuleInfo` rows, column exists?, simple read-only sanity | **visa2026-sql-local** | **`DATABASE_NAME` / port in MCP env must match the SQL instance the failing app uses** (local compose often exposes 1433; dev DB name may differ from `Visa2026DbProd`). |

If SQL MCP points at the **wrong** database, results will mislead—confirm **`DB_NAME`** in the active `.env` file and **`MSSQL_HOST_PORT`** if non-default.

---

## 4. Playbook: schema drift / DatabaseUpdate

**Signs:** `SqlException` **Invalid column name**; app version ahead of physical schema.

1. One-off updater (same compose project / env / file as `app`): see [reference.md](./reference.md) **§ DB update one-off**.
2. Recreate **`app`** only after success: [reference.md](./reference.md) **§ Recreate app**.
3. If **`ModuleInfo` says current** but schema still wrong: **`FORCE_XAF_DB_UPDATE=true`** one shot per [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md), then remove.

Full narrative: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md).

---

## 5. Playbook: image tag / `pull` failure

**Signs:** `docker.io/... not found` for `webapia/visa2026:<tag>`.

1. **`APP_IMAGE_TAG`** in `.env.*` must exist on the registry (CI tags from `Visa2026.Module` **AssemblyVersion**—see `.github/workflows/publish-to-docker-hub.yml`).
2. Use **`latest`** or a published tag until CI pushes the new semver.
3. Private registry: `docker login` on the host pulling the image.

Details: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md §5](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md).

---

## 6. Playbook: droplet / SSH

- **`droplet-scripts/update-app.ps1`:** pull + restart **app**; `-IdentityFile` for SSH key. Repo root from script path (no hardcoded `LOCAL_REPO`).
- **`droplet-scripts/Set-ForceXafDbUpdate.ps1`:** env change + force-recreate on server (if present).

[docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md](../../../docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md), [scripts/README.md](../../../scripts/README.md).

---

## 7. Verification checklist

- [ ] `docker logs <app-container> --tail 100` — no repeating exception on the original path.
- [ ] Browser: reproduce steps that failed.
- [ ] Optional: `scripts/local/Get-ModuleInfoFromSql.ps1 -EnvFile .env.local` or SQL MCP — `ModuleInfo` vs **AssemblyVersion** in `Visa2026.Module.csproj`.

---

## 8. Escalation (stop and ask the human)

- **Production** data at risk, unclear backup, or destructive compose (`down -v`, prune volumes).
- Changing **secrets** or **production** `.env` without explicit approval.
- Incident not matching playbooks after logs + one playbook attempt—summarize **first error block** and propose next hypothesis.
