# UI scenarios — reference

Canonical workflow: [SKILL.md](./SKILL.md). **Start with map:** [reference-map-contract.md](./reference-map-contract.md).

---

## Three-stage pipeline

```text
<id>_map.md     →  ui-test-hooks (gaps)  →  <id>.yaml  →  runner
     ↑                      ↑
  this skill            hook prep skill
```

Do not skip the map. Do not author YAML until map §3 hooks are **verified** or **waived**.

---

## Architecture

```text
examples/<id>_map.md + .yaml     draft (Hooks pending)
        ↓ promote when Ready for YAML
tools/UiScenarioRunner/scenarios/   ready only — runner reads here
docs/UI_TEST_HOOKS.md               verified hook catalog
hooks-manifest.json                 hook id → selectors
```

**Single source for selectors:** `tools/VerifyUiTestHooks/hooks-manifest.json`. Scenarios never hardcode `#login-user-name` except in comments.

---

## YAML scenario shape

```yaml
id: my-scenario-id
description: Short human label
baseUrl: https://localhost:5001   # optional; runner default
requiresAuth: true                  # default true except login-only
env:
  personDetailPath: /Person_DetailView_Employee/{guid}   # optional

steps:
  - login:
      user: Admin
      password: ""
  - goto: /Person_ListView_Employees
  - fill:
      person-first-name: Ada
      person-last-name: Lovelace
  - click: login-submit             # shorthand: hook id string
  - select-tab: person-employee-tab-passports
```

---

## Step vocabulary (planned runner)

| Step | YAML | Resolves hooks | Behavior |
|------|------|----------------|----------|
| **login** | `login: { user, password }` | `login-user-name`, `login-password`, `login-submit` | fill + click submit |
| **goto** | `goto: /path` or full URL | — | `page.GotoAsync` + Blazor wait |
| **fill** | `fill: { hook-id: value, … }` | each key = hook id | focus, clear, type |
| **click** | `click: hook-id` or `click: { hook: id }` | hook id | click visible target |
| **select-tab** | `select-tab: hook-id` | layout tab hook | click tab header |
| **wait-for** | `wait-for: hook-id` | hook id | wait until selector visible |
| **assert-visible** | `assert-visible: hook-id` | hook id | fail if not found |
| **select-listbox-item** | `select-listbox-item: <text>` or `${envKey}` | — | click DevExpress dropdown row (`.dxbl-listbox-item`, `[role=menuitem]`, or `.dxbl-dropdown-body button`); exact then partial match; waits for load after click |

**Runner timing (mandatory):** before/after `click`, `goto`, and `login`, the runner calls `WaitForBusyOverlayAsync` (and `WaitForAppShellAsync` after navigation). Do not remove these waits — XAF Blazor shows hooks while still loading. See [reference-run-lifecycle.md](./reference-run-lifecycle.md) § Blazor wait discipline.

Add new step types only after runner support + one example scenario.

---

## Hook resolution

For hook id `person-first-name`:

1. Load `hooks-manifest.json` → find check/checks or dedicated `hooks` map with matching `id`.
2. Try `selectors` **in order** (same as DevTools priority: `#id`, `[data-testid]`, `.e2e-*`).
3. Fail fast with: *hook `person-first-name` not in manifest or not verified in UI_TEST_HOOKS.md*.

**Rule:** scenario author checks [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) before adding a hook id to YAML.

---

## Navigation (XAF Blazor)

| Goal | Typical path / note |
|------|---------------------|
| Logon | `/LoginPage` |
| Person employees list | `/Person_ListView_Employees` |
| Person employee detail | `/Person_DetailView_Employee/{oid}` — copy `{oid}` from browser |
| Application list | `/Application_ListView` *(confirm in running app)* |

Until **ListView row hooks** exist, prefer **`goto`** with a **known detail URL** (fixture employee) via `env.personDetailPath` or runner `--start-url`.

**Scenario runs:** dedicated host **`http://localhost:5052`** only — see [reference-run-lifecycle.md](./reference-run-lifecycle.md).

---

## Variables and secrets

| Source | Use |
|--------|-----|
| YAML `env:` | Non-secret paths, fixture guids |
| Runner CLI | `--base-url`, `--user`, `--password` |
| OS env | `VISA2026_SCENARIO_BASE_URL`, `VISA2026_SCENARIO_USER`, `VISA2026_HOOK_VERIFY_PERSON_URL` |

Never commit real passwords. Stage credentials via env files (not tracked).

---

## Relation to other tools

| Tool | Difference |
|------|------------|
| **VerifyUiTestHooks** | Single-view hook checks; no multi-step story |
| **EasyTest / E2E.Tests** | Caption/Selenium; Windows CI matrix |
| **UiScenarioRunner** | Multi-step YAML + hook ids; stage smoke |

---

## UiScenarioRunner (planned layout)

```text
tools/UiScenarioRunner/
  UiScenarioRunner.csproj
  Program.cs
  ScenarioLoader.cs
  HookResolver.cs          # reads hooks-manifest.json
  StepExecutors/             # Login, Fill, Click, …
  scenarios/                 # authoritative YAML (after implement)
  README.md
```

Implement runner in a follow-up task; it loads YAML from **`tools/UiScenarioRunner/scenarios/`** only. Drafts stay in [examples/](./examples/).

---

## Authoring checklist

- [ ] **`<id>_map.md`** exists first ([reference-map-contract.md](./reference-map-contract.md))
- [ ] Map §3 hook table complete; gaps handed to **ui-test-hooks**
- [ ] Map status **Ready for YAML** before creating `.yaml`
- [ ] Every hook id in YAML appears in **`docs/UI_TEST_HOOKS.md`**
- [ ] Matching entry in **`hooks-manifest.json`**
- [ ] `login` step when `requiresAuth: true`
- [ ] No localized captions as locators
- [ ] Map §4 and `.yaml` stay in sync
