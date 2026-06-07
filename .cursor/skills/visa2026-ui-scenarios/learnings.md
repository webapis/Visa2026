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
| Stable scenario family | Move YAML to `tools/UiScenarioRunner/scenarios/` when runner exists |

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
