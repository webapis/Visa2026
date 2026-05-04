---
name: commit-after-verify
description: >-
  When the user asks to commit (or save and commit, git commit, create a commit),
  verifies the solution by building and running tests first, and only creates a
  git commit if those checks succeed. Use for commit requests where the user
  wants pre-commit verification to reduce broken commits.
disable-model-invocation: false
---

# Commit only after verify

## When this applies

The user wants a **git commit** but expects: **verify first** → **commit only if verification passes**.

## Procedure (run in order)

1. **Build the full solution** from repo root (failures here include many “runtime” issues that appear as load failures):

   ```powershell
   dotnet build Visa2026.slnx -c Release
   ```

   If `Release` is inappropriate for the task, use `-c Debug` instead and say so in the reply.

2. **Run automated tests** after a successful build (adjust if the user scoped tests):

   ```powershell
   dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c Release --no-build
   ```

   If E2E is too heavy for a quick commit, ask once, or run `dotnet test Visa2026.slnx -c Release --no-build` when the SDK supports it.

3. **If build or test fails**  
   - Summarize the **first actionable errors**.  
   - **Do not** run `git commit`.  
   - Offer fixes or ask for direction.

4. **If build and tests succeed**  
   - Show **`git status`** (short).  
   - Stage only paths the user asked to include, or **ask** if unclear. Avoid `git add -A` unless the user explicitly asked for all changes.  
   - Create the commit with a **clear message** (and do not embed secrets).

## Limits (say this if relevant)

- **`dotnet build` + `dotnet test`** catch compile and **automated** test failures; they do **not** guarantee every **manual UI / Docker-only** runtime path. For those, note that the user should still smoke-test if needed.

## Phrasing for the user

Suggest they say explicitly: **“commit after verify”** or **“commit if build passes”** so this skill lines up with **Agent** mode and tool use.
