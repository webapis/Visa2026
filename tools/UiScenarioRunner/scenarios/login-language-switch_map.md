# login-language-switch — scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `login-language-switch` |
| **Status** | Green local run |
| **Map version** | 0.2 |
| **Date** | 2026-06-08 |
| **YAML file** | [login-language-switch.yaml](./login-language-switch.yaml) |

---

## 1. Journey

Open the **logon page** (unauthenticated), use the DevExpress **runtime language switcher** in the header toolbar to change UI culture from the default (**en-US**) to **tk-TM**, and confirm the page reloads with logon fields still available.

**Outcome (v1):**

1. Language switcher button is visible on `/LoginPage`.
2. User picks a target culture from the dropdown (`.dxbl-listbox-item`) via `select-listbox-item`.
3. App performs a full reload (`?culture=` / `?ui-culture=`).
4. `login-user-name` and `login-submit` remain visible after reload.

**Out of scope (v1):** logon caption `assert-text`, settings-menu switcher, round-trip to en-US.

**Supported cultures:** `en-US`, `tr-TR`, `tk-TM`, `ru-RU`.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5052` |
| **Auth** | none (`requiresAuth: false`) |
| **Paths** | `/LoginPage` |

---

## 3. Hook inventory

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `login-language-switcher` | Action `LanguageSwitcher` | click | **verified** | [UI_TEST_HOOKS.md](../../../docs/UI_TEST_HOOKS.md) |
| `login-user-name` | Logon `UserName` | wait-for | **verified** | Post-reload sanity |
| `login-submit` | Action `Logon` | wait-for | **verified** | |
| Culture list items | `.dxbl-listbox-item` by label | select-listbox-item | **waived** | `env.targetCultureLabel` — no per-culture hook ids |

**Ready for YAML:** ☑

---

## 4. Proposed YAML

Authoritative: [login-language-switch.yaml](./login-language-switch.yaml).

```yaml
id: login-language-switch
description: Change UI language from login page header switcher (en-US to tk-TM)
requiresAuth: false

env:
  targetCultureLabel: Türkmen Dili (Türkmenistan)

steps:
  - goto: /LoginPage
  - wait-for: login-language-switcher
  - click: login-language-switcher
  - select-listbox-item: ${targetCultureLabel}
  - wait-for: login-user-name
  - wait-for: login-submit
```

---

## 5. Blockers

- **`assert-text`** on localized Logon caption — optional v2; use screenshots for now.
- Pin **`targetCultureLabel`** if OS culture display name differs (runner tries exact then partial match).

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-06-08 | Initial map (examples/) |
| 2026-06-08 | `select-listbox-item` runner step; promoted to `scenarios/` |
| 2026-06-08 | Green run on `:5052` — menuitem selector + `Türkmen Dili (Türkmenistan)` label |
