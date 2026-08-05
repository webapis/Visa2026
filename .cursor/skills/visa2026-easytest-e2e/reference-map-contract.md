# E2E `*_map.md` contract (Playwright)

**Driver:** **`playwright` only** for new scenarios. DevExpress EasyTest is **deprecated**.

**Blocking:** Do **not** author **`<scenario-id>.yaml`** until the map shows **all required locators verified** (or explicitly waived).

**Execution (Option A):** YAML is **spec metadata**; matching C# under `Visa2026.E2E.Tests/Playwright/` runs the steps. Keep map, yaml, and `[Fact]` in sync.

**DetailView fill:** §3 and yaml `fill:` must list fields in **top → bottom** layout order (officer simulation).

Copy **`Visa2026.E2E.Tests/scenarios/examples/_map_TEMPLATE.md`** when starting a new scenario (set `driver: playwright`).

---

## Co-located files

| File | When | Role |
|------|------|------|
| **`<scenario-id>_map.md`** | **First** | Planned YAML + locator inventory (fill order) |
| **`<scenario-id>.yaml`** | **After** locators verified | Step metadata |
| **`Playwright/*Tests.cs` method** | With yaml | Executable Playwright API |

### Folder rules

| Map status | Location |
|------------|----------|
| **Draft**, locators pending | `Visa2026.E2E.Tests/scenarios/examples/` |
| **Ready for YAML**, stable in CI | `Visa2026.E2E.Tests/scenarios/ready/` |

**Basename rule:** same stem — e.g. `login-smoke_map.md` + `login-smoke.yaml`.

---

## Workflow (mandatory order)

```text
1. MAP   — <id>_map.md with driver: playwright; §3 top→bottom
2. YAML  — when §3 all verified/waived → <id>.yaml
3. C#    — Playwright [Fact] + Trait Driver=Playwright
4. RUN   — dotnet test -c EasyTest --filter "Driver=Playwright"
5. PROMOTE — examples/ → ready/ when CI-stable
```

---

## Required map sections

| § | Title | Content |
|---|--------|---------|
| **0** | Header | Scenario id, **E2E id**, **`driver: playwright`**, status, date, yaml file, C# test method |
| **1** | Journey | Officer goal (BO, views, outcome) |
| **2** | Navigation | User, `:5050` paths, seed constants |
| **3** | Locator inventory | **Ordered top→bottom** for DetailView fills: label / `data-testid` / role, UI target, step, status |
| **4** | Proposed YAML | Sketch of final `.yaml` (`fill:` keys in same order) |
| **5** | Blockers | TabbedMDI, lookup settle, nested New, … |
| **6** | Changelog | Date + note |

Do **not** set `driver: easytest` or `hybrid` on new maps.

---

## Locator status values (§3)

| Status | Meaning | Next action |
|--------|---------|-------------|
| **verified** | Works in headed Playwright run | Ready for YAML |
| **flaky** | Needs wait/retry | Document in §5; harden locator |
| **missing** | No stable locator | Add `data-testid` / accessible name in Blazor, or waive with URL-only step |
| **waived** | Step works without field fill (e.g. `goto` URL) | Document why in §5 |

**Ready for YAML:** every §3 row is **verified** or **waived**. Order of fill rows = layout top→bottom.

---

## YAML step vocabulary (Playwright)

| Step | Meaning | C# |
|------|---------|-----|
| `login:` | User Name / Password, Log In | Label fill + button |
| `goto:` | View path segment | `page.GotoAsync` |
| `fill:` | **Ordered** label → value (top→bottom) | `FillDetailViewTopToBottomAsync` |
| `action:` | Toolbar / button caption | `GetByRole(Button, …)` |
| `assert-shell:` | Post-login shell | Employees URL / shell locator |
| `assert-url-contains:` | URL check | `page.Url` |
| `assert-property:` / visible text | Field or grid value | Locator assertions |
| `open-grid-row:` | Grid row by column value | Row locator + click |

Use **`user` / `password`** aligned with `E2ETestLoginValues`.

---

## Agent rules

| Situation | Action |
|-----------|--------|
| User asks for E2E / EasyTest scenario | Playwright map + `Playwright/` Fact; EasyTest is deprecated |
| Locator missing | Add `data-testid` / accessible name in Module/Blazor |
| DetailView multi-field | Fill **top → bottom** only |
| Yaml without C# | Incomplete — add matching Playwright `[Fact]` |
| Legacy EasyTest Fact touched | Migrate to Playwright; do not extend EasyTest |
| Promote to ready/ | Only after filtered Playwright `dotnet test` passes |