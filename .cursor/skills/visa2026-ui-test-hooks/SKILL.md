---
name: visa2026-ui-test-hooks
description: >-
  Adds and verifies Visa2026 Blazor UI test hooks (data-testid, InputId, e2e-* classes)
  via Module ModelDefault, Model.xafml, and Blazor controllers; documents selectors in
  registry.md and DevTools discoverability checks. Read/append learnings.md for verified
  hook patterns. Use for test hooks, E2E selectors, Playwright stage smoke, login/field/tab
  accessibility, BlazorLayoutManager ItemCreated, or scraping the live UI.
disable-model-invocation: false
---

# Visa2026: UI test hooks

## Scope

| Layer | Tool | Skill |
|-------|------|-------|
| **UI test hooks** | `data-testid`, `InputId`, `e2e-*` CSS | **This skill** |
| **EasyTest E2E (dev/CI)** | `Visa2026.E2E.Tests` + Selenium | [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) — not this skill |
| **Stage smoke (planned)** | Playwright + hooks against stage URL | This skill + future `tools/` or Playwright project |
| **Unit / integration** | `Visa2026.Module.Tests` | [visa2026-unit-tests](../visa2026-unit-tests/SKILL.md) |

**Term:** **UI test hooks** = stable attributes/classes on real controls so automation (Playwright, DevTools, scrapers) can find them without DevExpress `.dxbl-*` classes or localized captions.

**Where code lives:** **`Visa2026.Blazor.Server`** (controllers, `Model.xafml`); optional **`[ModelDefault("CustomCSSClassName", …)]`** on **`Visa2026.Module`** BO properties. Do not put hook logic in Blazor.Server for domain rules — only UI accessibility.

**Inventory:** [registry.md](./registry.md) — update when adding hooks.

**Element matrix:** [reference.md](./reference.md) — which mechanism per UI element type.

**Experience log:** [learnings.md](./learnings.md) — append-only; read before, append after verified DevTools/Playwright checks.

**Related:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md) (same READ → TRY → TEST → RECORD → PROMOTE loop).

---

## Continuous improvement

```text
READ learnings.md + registry.md → IMPLEMENT hook → RESTART app → VERIFY in DevTools
→ UPDATE registry.md → APPEND learnings.md → PROMOTE if repeated (2+ hits)
```

**Do not** append speculation — only verified DOM discovery or confirmed dead ends.

### Known pitfalls (promoted from learnings)

| Pitfall | Do instead |
|---------|------------|
| Document selectors without implementing model/controller | Hook must exist in running app before registry entry |
| Logon `ViewController` only (no `CreateLogonWindowControllers`) | Register logon controller in `Visa2026BlazorApplication.CreateLogonWindowControllers` |
| Tab selectors via caption text (`"Passports"`) | Use layout `LayoutGroup` **Id** + `BlazorLayoutManager.ItemCreated` → `HeaderCssClass` |
| `ModelDefault` / layout class only for tab **click** | `HeaderCssClass` on `DxFormLayoutTabPageModel`; verify clickable element in DevTools |
| Rely on `.dxbl-*` or `:nth-child` | Use `data-testid`, `#InputId`, or `.e2e-*` from this skill |
| Tag every BO field at once | Incremental hooks aligned to stage smoke journeys |

---

## Naming contract

| Artifact | Pattern | Example |
|----------|---------|---------|
| `data-testid` | `{area}-{target}` kebab-case | `login-user-name`, `person-employee-tab-passports` |
| `InputId` (text boxes) | same slug as test id | `#login-user-name` |
| CSS class | `e2e-{same-slug}` | `.e2e-person-first-name` |
| Tab strip | `{view-context}-tabs` | `person-employee-tabs` |
| Tab page | `{view-context}-tab-{layoutGroupId-kebab}` | `person-employee-tab-passports` |

**Stable ids:** XAF **ViewId**, layout **TabbedGroup** / **LayoutGroup** Id, **Action** Id, BO **member name** — not localized captions or MDI tab titles.

---

## Workflow: add one hook

1. **Read** [learnings.md](./learnings.md) and [reference.md](./reference.md) for the element type.
2. **Choose mechanism** (decision tree in reference).
3. **Implement** — reuse `E2eTextEditorSelectorApplicator` for string fields when possible.
4. **Build** Blazor.Server: `dotnet build Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj -c Debug`
5. **Restart** the app (hooks apply at runtime; hot reload may miss layout events).
6. **Verify** in browser DevTools (see checklist below).
7. **Update** [registry.md](./registry.md).
8. **Append** [learnings.md](./learnings.md) if non-obvious.

---

## Verify checklist (mandatory before done)

On the target view in Chrome DevTools → Console:

```javascript
// Replace with your test id
const el = document.querySelector('[data-testid="person-employee-tab-passports"]')
  ?? document.querySelector('.e2e-person-employee-tab-passports')
  ?? document.querySelector('#person-first-name');
console.log(el, el?.innerText ?? el?.value);
```

- [ ] Query returns **non-null** for the intended control (or tab header).
- [ ] For **tabs**: confirm which node is **clickable** (`HeaderCssClass` vs content panel).
- [ ] For **inputs**: prefer `#InputId`; `data-testid` on the `<input>` is ideal.
- [ ] For **actions**: `data-testid` or `.e2e-*` on the button.
- [ ] Optional: `.click()` or Playwright `click()` switches tab / focuses field.

**E2E scripts stay English** ([`docs/LOCALIZATION_PLAN.md`](../../../docs/LOCALIZATION_PLAN.md)); hooks avoid caption coupling.

---

## Build when `bin/` is locked

```powershell
dotnet build Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj -c Debug -o _build_out/
```

See `.gitignore` (`_build_out/`, `_agent_build_out/`).

---

## Stage smoke (pre-prod gate)

Goal: small Playwright (or scripted) suite against **stage URL** before prod — uses **hooks**, not full EasyTest matrix.

| Item | Guidance |
|------|----------|
| Environment | Stage base URL + test officer user (not prod) |
| Scope | 5–15 journeys; only views with verified registry entries |
| Data | Stage DB fixtures or seeded users — document in learnings |
| CI | Optional later; separate from Windows EasyTest PR gate |

Not implemented in repo yet — record URL/auth quirks in **learnings.md** when first run.

---

## Agent workflow

When the user asks to add test hooks, verify selectors, or plan stage E2E:

1. Read **learnings.md**, **registry.md**, **reference.md**.
2. Mirror existing controllers: `*E2eSelectorsController`, `E2eTextEditorSelectorApplicator`.
3. Implement minimal diff; update **registry.md**.
4. Tell user to **restart app** and run verify checklist.
5. Append **learnings.md** for verified fixes (login logon registration, tab HeaderCssClass, etc.).

When promoting a pattern twice → add row to **Known pitfalls** above or expand **reference.md**.

---

## Additional resources

- [reference.md](./reference.md) — element type → mechanism matrix, code patterns
- [registry.md](./registry.md) — implemented hooks inventory
- [learnings.md](./learnings.md) — append-only experience
- [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) — E2E pyramid vs unit tests
- DevExpress: [tab control in layout](https://github.com/DevExpress-Examples/xaf-how-to-access-a-tab-control-in-a-detail-view-layout) (`BlazorLayoutManager.ItemCreated`)
