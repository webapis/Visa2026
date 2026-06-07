# VerifyUiTestHooks

Headless **Playwright** runner for [visa2026-ui-test-hooks](../../.cursor/skills/visa2026-ui-test-hooks/SKILL.md) — automates the same **access + behavior** checks as the DevTools console.

**Does not** write to [`docs/UI_TEST_HOOKS.md`](../../docs/UI_TEST_HOOKS.md). After a green run, promote hooks in that doc and [registry.md](../../.cursor/skills/visa2026-ui-test-hooks/registry.md) manually (verified-only policy).

## Prerequisites

1. **Visa2026.Blazor.Server** running locally (Debug profile).
2. **Chromium** for Playwright (one-time install).

```powershell
dotnet build tools/VerifyUiTestHooks/VerifyUiTestHooks.csproj -c Debug
pwsh tools/VerifyUiTestHooks/bin/Debug/net8.0/playwright.ps1 install chromium
```

## Usage

### Login hooks (no extra URL)

App must be up at `https://localhost:5001` (or pass `--base-url`).

```powershell
dotnet run --project tools/VerifyUiTestHooks -- --scenario login
```

### Person detail hooks

Requires an **open employee detail** URL (copy from browser after opening an employee):

```powershell
$env:VISA2026_HOOK_VERIFY_PERSON_URL = "/Person_DetailView_Employee/your-guid-here"
dotnet run --project tools/VerifyUiTestHooks -- --scenario person-employee-tabs
dotnet run --project tools/VerifyUiTestHooks -- --scenario person-name-fields
```

Or:

```powershell
dotnet run --project tools/VerifyUiTestHooks -- `
  --scenario person-employee-tabs `
  --start-url "/Person_DetailView_Employee/your-guid-here"
```

### Options

| Flag | Default | Purpose |
|------|---------|---------|
| `--base-url` | `https://localhost:5001` | App root |
| `--user` | `Admin` | Logon when scenario requires auth |
| `--password` | *(empty)* | Logon password |
| `--start-url` | env `VISA2026_HOOK_VERIFY_PERSON_URL` | Person detail path or full URL |
| `--scenario` | all | Repeatable scenario id from `hooks-manifest.json` |
| `--headed` | headless | Show browser |
| `--timeout` | `30000` | Playwright timeout (ms) |

## Manifest

Edit **`hooks-manifest.json`** when adding hooks to verify (keep in sync with [registry.md](../../.cursor/skills/visa2026-ui-test-hooks/registry.md)).

Check types:

| `type` | Behavior |
|--------|----------|
| `exists` | First matching selector is non-null |
| `text-input` / `password-input` | focus + fill probe + read `value` |
| `layout-tab` | visible + `click()` |
| `button` | visible; clicks only if `clickOnBehavior` is true |

Selectors are tried **in order** (primary first, same as `docs/UI_TEST_HOOKS.md`).

## Workflow with the skill

```text
Implement hook → dotnet run VerifyUiTestHooks → all PASS
→ add row to docs/UI_TEST_HOOKS.md → registry status verified
```

Manual DevTools verify remains valid; Playwright is optional Phase 2.
