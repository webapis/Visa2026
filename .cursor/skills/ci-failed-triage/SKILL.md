---
name: ci-failed-triage
description: >-
  Triages failed GitHub Actions for Visa2026 (Docker Hub publish workflow, DevExpress
  license steps, docker build). Use when the user reports a red workflow, failed CI,
  Actions error, publish failure, or pastes a GitHub Actions / workflow log.
disable-model-invocation: false
---

# CI failed → triage (Visa2026)

## Goal

Turn a failed GitHub Actions run into a **short diagnosis**: which **job/step** broke, **likely cause**, and **next checks** (local or repo), without leaking secrets.

## 1. Gather facts (pick what is available)

- If the user pasted a log: extract **workflow name**, **job id**, **step name**, and the **first error block** (compiler line, `docker` error, `secrets not found`, `NU1301`, etc.).
- If they only gave a PR or branch: ask for the **run URL** or permission to use **GitHub MCP** / terminal to fetch logs.
- Repo workflows live under **`.github/workflows/`** (e.g. `publish-to-docker-hub.yml`).

Prefer **failed step output** over summaries.

## 2. Classify the failure (common buckets for this repo)

| Signal | Likely area |
|--------|-------------|
| `secrets.*` not set, login denied | **GitHub Actions secrets** (Docker Hub, DevExpress keys) — fix in repo/org settings, not in code. |
| DevExpress / license / `DevExpress.Key` | **Write DevExpress license** steps; missing or wrong `DEVEXPRESS_KEY` / `DEVEXPRESS_LICENSE` secrets. |
| `docker build` / `denied` / manifest | **Dockerfile**, build args, registry auth, or **context path** in workflow. |
| `NU1301`, NuGet, restore | **Package restore** (network, private feeds, `DevExpress.Key` in CI context). |
| `grep`, `AssemblyVersion`, bash in workflow | **Version extraction** step — `Visa2026.Module.csproj` `AssemblyVersion` format. |
| Wrong branch / no run | **Workflow `on:` filters** — e.g. push branches must match what you actually push. |

Do **not** ask the user to paste secret values. Redact tokens in replies.

## 3. Local reproduction (when the failure is “build”)

From repo root (see **`AGENTS.md`**):

```powershell
dotnet build Visa2026.slnx -c Release
```

If CI fails in **Docker build**, reproduce with the same `docker build` / Dockerfile and build-args as the workflow (read `.github/workflows/*.yml`).

## 4. Output format (what to tell the user)

1. **What failed** — workflow + job + step (as precise as possible).  
2. **Why (hypothesis)** — one primary cause tied to log lines.  
3. **What to do next** — concrete checks (settings UI path, file to edit, command to run).  
4. **If unclear** — list the **smallest** extra artifact needed (e.g. “failed step log only”, “commit SHA”).

## 5. Optional: GitHub CLI

If `gh` is authenticated:

```powershell
gh run list --limit 10
gh run view RUN_ID --log-failed
```

Use this when the user has not pasted enough log.

## 6. Boundaries

- Do not change workflow **secrets** from the agent; describe **which** secret names are missing.  
- Do not “fix” **Docker Hub** or **registry** credentials in code.  
- Prefer **minimal** code changes; CI-only issues often need **config/secrets/branch** fixes.
