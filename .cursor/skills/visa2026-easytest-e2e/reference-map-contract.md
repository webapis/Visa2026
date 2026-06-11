# EasyTest E2E `*_map.md` contract

**Blocking:** Do **not** author **`<scenario-id>.yaml`** until the map shows **all required captions verified** (or explicitly waived).

**Execution (Option A):** YAML is **spec metadata**; matching C# in `Visa2026.E2E.Tests` runs the steps. Keep map, yaml, and `[Fact]` in sync on every change.

Copy **`Visa2026.E2E.Tests/scenarios/examples/_map_TEMPLATE.md`** when starting a new scenario.

---

## Co-located files

| File | When | Role |
|------|------|------|
| **`<scenario-id>_map.md`** | **First** | Planned YAML + caption gap analysis |
| **`<scenario-id>.yaml`** | **After** captions verified | Step metadata (not executed by a runner yet) |
| **`*Tests.cs` method** | With yaml | Executable EasyTest API |

### Folder rules

| Map status | Location |
|------------|----------|
| **Draft**, **Captions pending** | `Visa2026.E2E.Tests/scenarios/examples/` |
| **Ready for YAML**, stable in CI | `Visa2026.E2E.Tests/scenarios/ready/` |

**Basename rule:** same stem in the same folder — e.g. `login-smoke_map.md` + `login-smoke.yaml`.

---

## Workflow (mandatory order)

```text
1. MAP   — user describes journey → write <id>_map.md (proposed YAML + caption status table)
2. YAML  — when map caption table all ✅ verified → write <id>.yaml in examples/
3. C#    — implement [Fact] referencing scenario id + e2eId
4. RUN   — dotnet test Visa2026.E2E.Tests -c EasyTest
5. PROMOTE — move <id>_map.md + <id>.yaml → scenarios/ready/ when CI-stable
```

---

## Required map sections

| § | Title | Content |
|---|--------|---------|
| **0** | Header | Scenario id, **E2E id**, status, date, yaml file, C# test method |
| **1** | Journey | Officer goal (BO, views, outcome) |
| **2** | Navigation | User, `:5050` paths, seed constants |
| **3** | Caption inventory | Table: caption/action, UI target, step, status |
| **4** | Proposed YAML | Sketch of final `.yaml` |
| **5** | Blockers | TabbedMDI, combo retry, nested New, … |
| **6** | Changelog | Date + note |

---

## Caption status values (§3)

| Status | Meaning | Next action |
|--------|---------|-------------|
| **verified** | Works in headed EasyTest run | None — ready for YAML |
| **flaky** | Needs retry helper | Use `FillFormWithRetry` / URL nav; document in §5 |
| **missing** | Caption not found by EasyTest | Fix `InputId`/aria in Module/Blazor or waive with URL-only step |
| **waived** | Step works without caption (e.g. `goto` URL) | Document why in §5 |

**Ready for YAML:** every row in §3 is **verified** or **waived**.

---

## YAML step vocabulary (EasyTest)

| Step | Meaning | C# helper |
|------|---------|-----------|
| `login:` | Fill User Name / Password, click Log In | `Login()` |
| `goto:` | Blazor view path segment | `EasyTestBlazorNavigationHelper.GoToRelativeUrl` / `NavigateEmployeesList()` |
| `fill:` | Map of caption → value | `FillForm` / `FillFormWithRetry` |
| `action:` | Toolbar action caption | `ExecuteActionWithRetry` |
| `assert-shell:` | Post-login app shell | `AssertAuthenticatedAppShell()` |
| `assert-action-visible:` | Action exists | `Assert.NotNull(AppContext.GetAction(...))` |
| `assert-url-contains:` | URL check | `EasyTestBlazorNavigationHelper.UrlContains` |
| `assert-property:` | Form field value | `GetForm().GetPropertyValue` + `Assert.Equal` |
| `open-grid-row:` | Grid lookup column → value | `GetGrid().ProcessRow` |

Use **`user` / `password`** top-level or under `login:` — align with `E2ETestLoginValues`.

---

## Agent rules

| Situation | Action |
|-----------|--------|
| User asks for EasyTest scenario | Write `_map.md` first in `scenarios/examples/` |
| Caption missing | Fix Blazor/Module accessibility, `InputId`, or URL navigation |
| Yaml without C# | Incomplete — add matching `[Fact]` |
| Promote to ready/ | Only after filtered `dotnet test` passes |
