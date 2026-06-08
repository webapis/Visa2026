# Learnings (append-only): Visa2026 UI scenarios

Capture **verified** outcomes from authoring YAML scenarios and running Playwright journeys. Read before similar work; append after confirmed runs.

**Hook prep** belongs in [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/learnings.md) — not here.

**Do not** delete old entries — append only.

---

## Entry template

```markdown
### YYYY-MM-DD — [+/−] <short title> (<scenario id>)

- **Outcome**: positive | negative | anti-pattern
- **Scenario**: yaml file id
- **Context**: local / stage URL, browser
- **Symptom** / **Goal**:
- **Try**:
- **Result**:
- **Reuse** / **Avoid**:
- **Promote**: none | pending | done → SKILL/reference
```

---

## Promotion ladder

| Signal | Action |
|--------|--------|
| First verified run | Append **learnings.md** only |
| Same lesson twice | **SKILL.md** Known pitfalls or **reference.md** |
| Stable scenario family | Promote to **`tools/UiScenarioRunner/scenarios/`** when **Ready for YAML** |

---

## Entries

### 2026-06-07 — [+] Map-first workflow (_map.md before yaml)

- **Outcome**: positive
- **Scenario**: *(skill design)*
- **Goal**: Plan YAML + hook gaps before authoring executable scenario
- **Try**: `<id>_map.md` with §3 hook inventory vs UI_TEST_HOOKS.md; ui-test-hooks for gaps; yaml only when Ready for YAML
- **Reuse**: Same pattern as user-report `*_map.md` contracts
- **Promote**: done → reference-map-contract.md + SKILL.md Process

### 2026-06-07 — [+] Scenarios must use hook ids, not CSS duplicates (design)

- **Outcome**: positive
- **Scenario**: *(skill design)*
- **Goal**: Login + Person fill journey without coupling to captions or `.dxbl-*`
- **Try**: YAML `fill: { person-first-name: "Ada" }` resolved via hooks-manifest.json
- **Reuse**: Author scenarios only after hooks verified in docs/UI_TEST_HOOKS.md
- **Promote**: done → SKILL.md Process + reference.md

### 2026-06-07 — [+] person-employee-create hooks verified → Ready for YAML

- **Outcome**: positive
- **Scenario**: person-employee-create
- **Goal**: Unblock full create journey after scalar + toolbar DevTools verify on Employee detail
- **Try**: Map §3 all **verified**; §5 hook blockers closed; draft yaml with tenant lookup env placeholders
- **Reuse**: Distinguish **hook blockers** (map §3) from **run-time notes** (tenant catalog text, combo `fill` behavior)
- **Promote**: pending → examples/README + person-employee-create_map.md v0.2

### 2026-06-08 — [+] Blazor runner: avoid NetworkIdle; visa family custom editor hook

- **Outcome**: positive (partial — fill reaches save path after fixes)
- **Scenario**: person-employee-create
- **Symptom**: Runs hung minutes on login; `person-visa-application-family-members-text` not in DOM
- **Try**: `WaitUntilState.Load` not `NetworkIdle`; `--slow-mo` / `--headed` + maximize; `data-testid` on `VisaFamilyMembersTextComponent` root + `InputId` on summary; runner no-op fill when display is Ýok/Yok
- **Reuse**: Custom Blazor property editors must forward E2eTestId to razor DOM; read-only inline editors need scenario-specific fill, not `FillAsync` on wrapper
- **Nav**: `goto: /Person_ListView_Employees` after login more reliable than accordion toggle when People group state varies
- **Promote**: pending → reference.md pitfalls

### 2026-06-08 — [+] Run lifecycle: dedicated :5052, step screenshots, Invoke-UiScenarioRun.ps1

- **Outcome**: positive (process rule)
- **Goal**: Scenario runs must not reuse IDE host; fresh build; stop server after run; reviewable screenshots
- **Try**: `reference-run-lifecycle.md` + `Invoke-UiScenarioRun.ps1`; runner `--screenshot-steps` (before/after each YAML step, not per keystroke)
- **Reuse**: Port **5052** + profile **Visa2026 - UI Scenarios (LocalDB)**; wait `person-first-name` after **New** (not save button — save exists on wrong views); login user from yaml (`standarduser` for officer flows)
- **Avoid**: Running against :5001 while IDE holds DLL locks; screenshot every field in `fill` (use **steps** tier; **fields** tier only when debugging combos)
- **Promote**: done → SKILL.md + reference-run-lifecycle.md

### 2026-06-08 — [+] WaitForBusyOverlayAsync before/after clicks (person-employee-create)

- **Outcome**: positive
- **Scenario**: person-employee-create
- **Symptom**: **New** clicked while employee list still showed **Loading…**; `person-first-name` never appeared; or wrong MDI tab (Education) when waiting on generic **Save**
- **Try**: `WaitForBusyOverlayAsync` **before** click + after click; extra settle after `*-new`; YAML `wait-for: person-first-name` not `person-detail-employee-save` after **New**
- **Reuse**: Treat visible hook ≠ ready UI; review `step-04-click-after.png` in run folder — list spinner means wait failed
- **Promote**: done → reference-run-lifecycle.md § Blazor wait discipline + SKILL pitfalls

### 2026-06-08 — [+] `select-listbox-item` + login-language-switch (toolbar menu, not listbox)

- **Outcome**: positive (green run on :5052)
- **Scenario**: login-language-switch
- **Symptom**: First run timed out on `.dxbl-listbox-item` — language switcher dropdown uses **`[role="menuitem"]`** in **`.dxbl-dropdown-body`**, not combo listbox rows
- **Try**: Runner step `select-listbox-item` tries listbox → menuitem → dropdown buttons; culture labels are **localized** (`Türkmen Dili (Türkmenistan)`, not `Turkmen (Turkmenistan)`)
- **YAML**: `env.targetCultureLabel` pinned from headed screenshot; partial match works (`Türkmen`)
- **Reuse**: Toolbar SingleChoice + language switcher → `click` hook then `select-listbox-item`; full page reload after pick — `wait-for` logon fields

### 2026-06-08 — [-] Stale MDI tabs on repeat scenario runs (not browser history)

- **Outcome**: negative → fixed
- **Symptom**: Headed runs show many in-app tabs (Applications, Işgärler, old Person detail) after logon; hooks / `wait-for` fail or wrong view active
- **Cause**: `Model.xafml` **`RestoreTabbedMdiLayout="True"`** + user **ModelDifference** in SQL — same `standarduser` restores prior TabbedMDI layout; unrelated to Playwright history
- **Fix**: `VISA2026_UI_SCENARIOS=true` on `:5052` host → ephemeral user `ModelDifferenceStore` (no SQL layout load) + `RestoreTabbedMdiLayout=false`; runner uses incognito context + `ClearCookiesAsync()`
- **Reuse**: Always use `Invoke-UiScenarioRun.ps1`; close orphaned Chromium windows after interrupted headed runs

### 2026-06-08 — [+] Fresh scenario DB: Visa2026UiScenario + LookupCatalogs baseline

- **Outcome**: positive (implemented)
- **Context**: Shared IDE `Visa2026` DB accumulated `E2E-CREATE-*` employees; scenarios need lookup-only baseline
- **Try**: Dedicated LocalDB `Visa2026UiScenario`; `-FreshDatabase` / `-All` → `Reset-UiScenarioDatabase.ps1` (greenfield `--updateDatabase`); `-UseBaselineSnapshot` → restore `tools/UiScenarioRunner/baseline/*.bak`; `tenant/subcontractor.json` seeds **Çalyk Enerji** for employee scenarios
- **Reuse**: `New-UiScenarioBaselineSnapshot.ps1` after schema/catalog changes; CI stays greenfield (fresh LocalDB each job)
