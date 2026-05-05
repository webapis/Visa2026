# AGENTS.md — Visa2026

Guidance for AI coding assistants (Cursor, Claude Code, Copilot, etc.) working in this repository.

## Stack

- **.NET** 8 (`net8.0`)
- **DevExpress / XAF** 25.2.6 (ExpressApp **EF Core**, **Blazor** UI, Security, Reports V2, Validation, Office, Audit Trail, Web API + OData, EasyTest adapter for E2E)
- **Database:** SQL Server (EF Core + `Microsoft.Data.SqlClient`)
- **PDF:** Spire.PDF (form filling / document workflows)
- **Tests:** xUnit, Selenium WebDriver; Blazor EasyTest adapter in E2E project

## Solution layout (`Visa2026.slnx`)

| Project | Role |
|---------|------|
| **Visa2026.Module** | **Primary place for domain logic:** EF `DbContext`, business objects (`BusinessObjects/`), XAF controllers (`Controllers/`), module wiring (`Module.cs`), database update / seeding (`DatabaseUpdate/`), PDF form mapping (`PdfFormMapping*`, `Services/PdfMappingHelper.cs`). Prefer extending patterns here instead of duplicating logic in the web host. |
| **Visa2026.Blazor.Server** | Blazor **application host**: XAF Blazor startup, `Model.xafml` (copy to output), Web API / Swagger as configured. Keep thin; heavy rules belong in the Module. |
| **Visa2026.DataImporter** | Separate tool for data import / maintenance (used with Docker **tools** profile where applicable). |
| **Visa2026.E2E.Tests** | End-to-end tests (Selenium + EasyTest Blazor adapter), references the Module. |

## Conventions

- **Match existing code** in `Visa2026.Module` (naming, `ObjectSpace` usage, controllers, validation, EF mappings). When unsure, search for a similar feature and mirror it.
- **XAF model:** `Visa2026.Module` uses embedded `Model.DesignedDiffs.xafml`; the Blazor Server project ships `Model.xafml` to output. Respect how model differences are already organized.
- **PDF / forms:** Business object `PdfFormMapping`, updater `DatabaseUpdate/PdfFormMappingUpdater.cs`, helpers and controllers under `Visa2026.Module` (`PdfFormMappingController`, cache controller, `PdfMappingHelper`). Form templates and assets live under **`Visa2026.Module/Resources/`** (including `FormTemplates/`). Do not invent new mapping storage without following this pipeline.
- **Database updates:** Logic under `Visa2026.Module/DatabaseUpdate/`. Be careful with `ModuleUpdater` / schema compatibility; see `docs/ENVIRONMENTS.md` for **`FORCE_XAF_DB_UPDATE`** when updaters must run once on an “already current” database.
- **Configurations:** Solution uses **Debug**, **Release**, and **EasyTest** (E2E / importer mapping as in `.slnx`).

## Local build and test (from repo root)

```powershell
dotnet build Visa2026.slnx -c Debug
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c Debug
```

If your SDK supports testing the whole solution file, you can use `dotnet test Visa2026.slnx -c Debug` instead of the single-project line.

## Git: “commit” with verification first

If you want the assistant to **build and test before creating a commit** (and **not** commit when checks fail), say so explicitly, for example: **“commit after verify”** or **“commit if the build passes”**.

That workflow is encoded as the Agent **Skill** **`.cursor/skills/commit-after-verify/SKILL.md`**. In **Agent** mode, the model can follow it when your message matches that intent (the skill description includes *commit*, *git commit*, etc.).

For a **standing** reminder in every chat, you can add a one-line **Cursor user rule** (global) or a short **always-on project rule** in `.cursor/rules/`—but the **Skill** stays the right place for the **step-by-step** procedure so `AGENTS.md` does not grow into a script.

## Docker and environments

Authoritative split (prod vs dev on one host, env files, compose project names, SQL volumes): **`docs/ENVIRONMENTS.md`**.

Typical local dev database stack:

```powershell
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d
```

Optional hot reload inside Docker: **`docker-compose.watch.yml`** and **`scripts/local/Start-ComposeWatch.ps1`** (see `docs/ENVIRONMENTS.md` for constraints).

## Secrets and tooling

- **Never commit** real passwords, license keys, or production connection strings. Use `.env.dev` / `.env.prod` (from `*.example` files) for Docker; keep Cursor MCP credentials in **OS user environment** or Cursor settings, not in tracked JSON when avoidable.
- **`.cursorignore`** excludes `bin/`, `obj/`, build artifacts, and local `.env` files from indexing; see that file for details.
- **Cursor MCP (optional):** `.cursor/mcp.json` may define **`dxdocs`** (DevExpress documentation), **`visa2026-sql-local`** (read-only SQL via `npx`), and **`github`** (official [`ghcr.io/github/github-mcp-server`](https://github.com/github/github-mcp-server) via Docker — **Docker Desktop / Engine must be running** when you use it). Set a Windows **user** environment variable **`GITHUB_PERSONAL_ACCESS_TOKEN`** to a [fine-grained or classic PAT](https://github.com/settings/personal-access-tokens) with the scopes you are comfortable granting (e.g. **Contents** / **Metadata** for private repos, **Actions** read for workflow runs, **Issues** / **Pull requests** as needed). Do not commit the token. `GITHUB_TOOLSETS` in config enables **repos, issues, pull_requests, actions**; trim in `.cursor/mcp.json` if you want fewer surfaces. The SQL MCP **`DATABASE_NAME`** must match the database that actually exists on the SQL instance you point at (dev vs prod compose uses different `DB_NAME` defaults). Adjust MCP env when switching stacks. Use the terminal (or Cursor-approved shell) and **`docs/ENVIRONMENTS.md`** for **`docker compose`** workflows.

## What not to do

- Do not move core business rules only into `Visa2026.Blazor.Server` if the rest of the app keeps them in the Module.
- Do not widen scope of changes beyond the task (avoid drive-by refactors unrelated to the request).
- Do not strip or ignore existing comments and established patterns without a clear reason.

## Agent skills (project plan)

- **One `SKILL.md` ≈ one recurring task** (single workflow family). New recurring work → new `.cursor/skills/<name>/` folder, not a mega-skill.
- **Capture first in docs** (e.g. **`docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md`**) for narrative; **promote** to a skill after repeat or when high-risk (funnel spelled out in that doc).
- **AI may draft** skill changes; **developer reviews** before commit—especially prod, secrets, and destructive `docker compose` / volume operations.
- **Structure:** keep `SKILL.md` short (triggers, steps, links); use **`reference.md`** in the same folder for long command blocks. Prefer **linking** between skills (e.g. lifecycle → `ci-failed-triage`) over duplicating procedures.

## Further docs

- **`docs/ENVIRONMENTS.md`** — compose files, ports, volumes, `FORCE_XAF_DB_UPDATE`, watch stack.
- **`docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md`** — deploy/DB incident log + **plan** (doc → skill funnel, one-skill-one-task, AI-assisted updates).
- **`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`** — optional Agent **Skill**: IDE → Docker → logs/DB triage, MCP hooks (SQL reader, dxdocs, GitHub).
- **`scripts/README.md`** — which scripts are for local workstation vs server/droplet.
- **`.cursor/rules/*.mdc`** — Cursor-only rules (always-on core + file-scoped Module / Blazor host). Same intent as this file, kept short for the agent.
- **`.cursor/skills/ci-failed-triage/SKILL.md`** — optional Agent **Skill** for triaging failed **GitHub Actions** (invoke when CI fails or `@` the skill if your Cursor UI supports it).
- **`.cursor/skills/commit-after-verify/SKILL.md`** — optional Agent **Skill**: run **build + tests** before **`git commit`**; skip commit on failure (see **Git: “commit” with verification first** above).
