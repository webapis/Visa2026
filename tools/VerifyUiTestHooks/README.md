# VerifyUiTestHooks

Headless **Playwright** runner for [visa2026-ui-test-hooks](../../.cursor/skills/visa2026-ui-test-hooks/SKILL.md) — automates the same **access + behavior** checks as the DevTools console.

**Does not** write to [`docs/UI_TEST_HOOKS.md`](../../docs/UI_TEST_HOOKS.md). After a green run, promote hooks in that doc and [registry.md](../../.cursor/skills/visa2026-ui-test-hooks/registry.md) manually (verified-only policy).

## Recommended: isolated verify (agents)

Use [`scripts/local/Invoke-UiHookVerify.ps1`](../../scripts/local/Invoke-UiHookVerify.ps1) — builds to `_agent_build_out/`, starts via launch profile **`Visa2026 - Hook Verify (LocalDB)`** on **`http://localhost:5051`**, LocalDB **`Visa2026HookVerify`**, runs this tool, then stops the host.

```powershell
# One-time Playwright Chromium
dotnet build tools/VerifyUiTestHooks/VerifyUiTestHooks.csproj -c Debug
powershell -ExecutionPolicy Bypass -File tools/VerifyUiTestHooks/bin/Debug/net8.0/playwright.ps1 install chromium

# Login + nav hooks
.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario login
.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario nav-people

# All manifest scenarios (Person scenarios need -StartUrl or env — see manifest)
.\scripts\local\Invoke-UiHookVerify.ps1
```

See [SKILL.md § Isolated verify server](../../.cursor/skills/visa2026-ui-test-hooks/SKILL.md) for `-KeepServer`, `-StopOnly`, `-Profile DockerDev`, launch profile **Visa2026 - Hook Verify (LocalDB)**, and Person detail URLs.

## Advanced: host already running

If you intentionally verify against your own host (Visual Studio, Docker, etc.):

```powershell
dotnet run --project tools/VerifyUiTestHooks -- --base-url http://localhost:5000 --scenario login
```

Default `--base-url` is `https://localhost:5001` when invoking the tool directly.

### Person detail hooks

Requires an **open employee detail** URL:

```powershell
.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario person-scalar-fields `
  -StartUrl "/Person_DetailView_Employee/your-guid-here"
```

Or:

```powershell
$env:VISA2026_HOOK_VERIFY_PERSON_URL = "/Person_DetailView_Employee/your-guid-here"
dotnet run --project tools/VerifyUiTestHooks -- --scenario person-employee-tabs --base-url http://localhost:5051
```

## Options (`VerifyUiTestHooks` CLI)

| Flag | Default | Purpose |
|------|---------|---------|
| `--base-url` | `https://localhost:5001` | App root |
| `--user` | `Admin` | Logon when scenario requires auth |
| `--password` | *(empty)* | Logon password |
| `--start-url` | env / scenario `startPath` | Path for authenticated scenarios |
| `--scenario` | all | Repeatable scenario id from `hooks-manifest.json` |
| `--headed` | headless | Show browser |
| `--timeout` | `30000` | Playwright timeout (ms) |

`Invoke-UiHookVerify.ps1` forwards `-Scenario`, `-StartUrl`, `-Password`, `-Headed`, `-TimeoutMs`, and sets `--base-url` to the isolated host.

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
Implement hook → Invoke-UiHookVerify.ps1 → all PASS
→ add row to docs/UI_TEST_HOOKS.md → registry status verified
```

Manual DevTools verify remains valid; Playwright is the default agent path.
