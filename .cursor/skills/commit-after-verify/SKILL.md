---
name: commit-after-verify
description: >-
  When the user asks to commit (or save and commit, git commit, create a commit),
  verifies the solution by building first, and only creates a git commit if the
  build succeeds. Use for commit requests where the user wants pre-commit
  verification to reduce broken commits.
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

2. **If build fails**  
   - Summarize the **first actionable errors**.  
   - **Do not** run `git commit`.  
   - Offer fixes or ask for direction.

3. **If build succeeds**  
   - Show **`git status`** (short).  
   - Stage only paths the user asked to include, or **ask** if unclear. Avoid `git add -A` unless the user explicitly asked for all changes.  
   - Create the commit with a **clear message** (and do not embed secrets).

## Limits (say this if relevant)

- **`dotnet build`** catches compile-time failures; it does **not** guarantee automated test coverage or every **manual UI / Docker-only** runtime path. For those, note that the user should still smoke-test (and/or run tests) if needed.

## Phrasing for the user

Suggest they say explicitly: **“commit after verify”** or **“commit if build passes”** so this skill lines up with **Agent** mode and tool use.
