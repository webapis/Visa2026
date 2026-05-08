---
name: visa2026-lifecycle-docker
description: >-
  Visa2026 dev-to-Docker lifecycle: local image build, compose deploy, verify Visa2026.Module
  AssemblyVersion (csproj vs DLL in running app container), container log triage,
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

## Deterministic contract (what “deterministic” means here)

This skill must be executable as a **fixed runbook**:

- Prefer **repo scripts** over ad-hoc `docker compose` commands.
- Use a **single canonical command sequence** for local rebuild/recreate, log capture, DB update, and verification.
- Branch only on **observable log substrings** (e.g., `Invalid column name`) and do not introduce “choose your own adventure”.
- If the user requests “ask before each command”, switch to Approval mode (§1). Otherwise, run the deterministic sequence (§2–§10).

### Chat openers (copy-paste)

Use with **`@.cursor/skills/visa2026-lifecycle-docker/`** (or rely on the skill `description` alone):

- **Local build + recreate, step-by-step with confirmation:**  
  `Coding is done—walk me through local Docker image build and container recreate; propose each step and wait for my OK.`

- **Local build + recreate, step-by-step (scripted):**  
  `Run the local Docker lifecycle scripts step-by-step and ask for my OK before each command (build → list → recreate local → logs).`

Shorter variants:

- `Local Docker: build images and recreate visa2026-dev stack.`
- `@visa2026-lifecycle-docker docker logs show Invalid column name on People`

---

## 1. Approval mode (ask before each command)

When the user requests “step-by-step” or “ask for approval”, do **not** run a batch of commands.
Propose each command and wait for an explicit **OK** before running it.

Deterministic local workstation sequence (scripts-only):

```powershell
.\scripts\local\lifecycle-docker\Build-DockerImages.ps1
.\scripts\local\lifecycle-docker\Docker-ListContainers.ps1
.\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1
.\scripts\local\lifecycle-docker\Docker-AppLogs.ps1 -Tail 200
.\scripts\local\lifecycle-docker\Verify-AppModuleVersion.ps1
```

If logs show schema drift (`Invalid column name`), insert DB update + recreate:

```powershell
.\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1 -Silent
.\scripts\local\lifecycle-docker\Recreate-App.ps1
```

---

## 2. Deterministic phase map (identify where we start)

| Phase | Typical actions | Repo pointers |
|--------|-----------------|---------------|
| **A. IDE** | Edit Module/Blazor, F5, debug | `AGENTS.md`, `.cursor/rules/` |
| **B. Verify compile** | `dotnet build Visa2026.slnx -c Debug` | Workspace rules |
| **C. Local images** | `scripts/local/lifecycle-docker/Build-DockerImages.ps1` | [scripts/README.md](../../../scripts/README.md) |
| **D. Compose run** | `docker compose -p … --env-file … -f … up -d` | [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md) |
| **E. Runtime issues** | `docker logs`, playbooks below | This skill |

**Rule:** `docker compose` runs on the **host** (PowerShell / SSH), not inside the `app` container.
For determinism, prefer the repo scripts in `scripts/local/lifecycle-docker/` and avoid inventing compose flags in-chat.

### Version verification (csproj vs running container)

**Goal:** Confirm **`AssemblyVersion`** in [`Visa2026.Module/Visa2026.Module.csproj`](../../../Visa2026.Module/Visa2026.Module.csproj) matches **`Visa2026.Module.dll`** inside the **running** app container—i.e. the image you built/recreate actually contains that module build (no stale `:local` image, wrong `APP_IMAGE_TAG`, or forgot recreate).

- **Script:** [`scripts/local/lifecycle-docker/Verify-AppModuleVersion.ps1`](../../../scripts/local/lifecycle-docker/Verify-AppModuleVersion.ps1) (optional `-ComposeProject`).
- **Not the same as SQL `ModuleInfo`:** the DB row reflects what XAF recorded after startup/updaters; it can lag or differ during migration edge cases. The DLL check answers “did this container get the bits for this `AssemblyVersion`?”

---

## 3. Deterministic local lifecycle (default path)

Unless the user explicitly says they are on a server/droplet, use this exact sequence.
Do not reorder steps.

1. Build local images:

   ```powershell
   .\scripts\local\lifecycle-docker\Build-DockerImages.ps1
   ```

2. Recreate the app container for the local stack (default script behavior):

   ```powershell
   .\scripts\local\lifecycle-docker\Compose-PullAndRecreateApp.ps1
   ```

3. Capture app logs (initial diagnostic bundle):

   ```powershell
   .\scripts\local\lifecycle-docker\Docker-AppLogs.ps1 -Tail 200
   ```

4. Verify **`Visa2026.Module`** version in repo matches the DLL in the container (exit code **1** on mismatch):

   ```powershell
   .\scripts\local\lifecycle-docker\Verify-AppModuleVersion.ps1
   ```

**Stop condition:** if logs contain a repeating exception loop, do not keep “recreating”; classify and apply exactly one playbook (§6–§8), then re-run step 2 and step 3.

---

## 4. Deterministic triage tree (runtime issue)

1. **Confirm stack identity:** `docker compose ls` or `docker ps` — note **project** (`visa2026-dev`, `visa2026-prod`, …) and **`app` container name** (often `<project>-app-1`).
2. **Browser shows generic “Application Error”** → treat as **server exception** until proven otherwise.
3. Use the scripted log capture (preferred):

   ```powershell
   .\scripts\local\lifecycle-docker\Docker-AppLogs.ps1 -Tail 200
   ```

   Find the **first** `fail:` / `SqlException` / `Error`.
4. **Classify:**
   - **`Invalid column name` / EF SQL errors** → playbook **Schema drift** (§6).
   - **`not found` on image pull** → playbook **Image tag** (§7).
   - **License / DevExpress / connection string** → env and secrets; compare `.env.*` with `.env.*.example`; see [DEBUGGING_DOCKER_DEPLOYMENTS.md](../../../docs/DEBUGGING_DOCKER_DEPLOYMENTS.md).
   - **“Works in Visual Studio, fails in Docker”** → same doc (data, env, networking, paths).
5. **Verify after fix:** `docker logs` clean on startup, hit the failing URL again; optional `ModuleInfo` check (§5 MCP / script).

---

## 5. MCP usage (when and prerequisites)

Use MCP **after** logs point in that direction—not on every incident. **Read the MCP tool schema** before calling (project `mcps/` descriptors). **Never paste** passwords, tokens, or full connection strings into chat.

| Symptom / need | Server (see `.cursor/mcp.json`) | Prerequisites |
|----------------|----------------------------------|---------------|
| XAF `DatabaseUpdateMode`, `ModuleUpdater`, compatibility, Blazor adapter behavior | **dxdocs** | Question is DX/XAF-specific |
| “Did CI publish this tag?” workflow logs, PR, Actions | **github** (Docker must be running if server uses Docker) | User PAT / access |
| `ModuleInfo` rows, column exists?, simple read-only sanity | **visa2026-sql-local** | **`DATABASE_NAME` / port in MCP env must match the SQL instance the failing app uses** (local compose often exposes 1433; dev DB name may differ from `Visa2026DbProd`). |

If SQL MCP points at the **wrong** database, results will mislead—confirm **`DB_NAME`** in the active `.env` file and **`MSSQL_HOST_PORT`** if non-default.

---

## 6. Playbook: schema drift / DatabaseUpdate (deterministic)

**Signs:** `SqlException` **Invalid column name**; app version ahead of physical schema.

1. Run the one-off DB updater for the same stack as the app:

   ```powershell
   .\scripts\local\lifecycle-docker\Compose-UpdateDatabase.ps1 -Silent
   ```

2. Recreate the app container:

   ```powershell
   .\scripts\local\lifecycle-docker\Recreate-App.ps1
   ```

3. Re-check logs:

   ```powershell
   .\scripts\local\lifecycle-docker\Docker-AppLogs.ps1 -Tail 200
   ```

4. If **`ModuleInfo` says current** but the schema is still missing columns, use **`FORCE_XAF_DB_UPDATE=true`** exactly once per [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md), then remove it immediately after a successful update.

Full narrative: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md).

---

## 7. Playbook: image tag / `pull` failure (deterministic)

**Signs:** `docker.io/... not found` for `webapia/visa2026:<tag>`.

1. **`APP_IMAGE_TAG`** in `.env.*` must exist on the registry (CI tags from `Visa2026.Module` **AssemblyVersion**—see `.github/workflows/publish-to-docker-hub.yml`).
2. Use **`latest`** or a published tag until CI pushes the new semver.
3. Private registry: `docker login` on the host pulling the image.

Details: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md §5](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md).

---

## 8. Playbook: droplet / SSH (deterministic)

- **`droplet-scripts/update-app.ps1`:** pull + restart **app**; `-IdentityFile` for SSH key. Repo root from script path (no hardcoded `LOCAL_REPO`).
- **`droplet-scripts/Set-ForceXafDbUpdate.ps1`:** env change + force-recreate on server (if present).

[docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md](../../../docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md), [scripts/README.md](../../../scripts/README.md).

---

## 9. Verification checklist (deterministic)

- [ ] `docker logs <app-container> --tail 100` — no repeating exception on the original path.
- [ ] Browser: reproduce steps that failed.
- [ ] **`AssemblyVersion` vs deployed bits:** `.\scripts\local\lifecycle-docker\Verify-AppModuleVersion.ps1` — csproj must match `Visa2026.Module.dll` in the running app container.
- [ ] Optional (DB / XAF updater state): `scripts/local/Get-ModuleInfoFromSql.ps1 -EnvFile .env.dev` or SQL MCP — `ModuleInfo` vs **AssemblyVersion** in `Visa2026.Module.csproj`.

---

## 10. Escalation (stop and ask the human)

- **Production** data at risk, unclear backup, or destructive compose (`down -v`, prune volumes).
- Changing **secrets** or **production** `.env` without explicit approval.
- Incident not matching playbooks after logs + one playbook attempt—summarize **first error block** and propose next hypothesis.
