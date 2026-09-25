# Learnings (append-only): Visa2026 BO state colors and ListView appearance

Purpose: this skill **gets smarter over time**. Capture **verified** outcomes from wiring state codes, `[Appearance]` rules, Blazor row CSS, and registry migrations. Agents **read before** similar work; **append after** a lesson is confirmed.

Keep **`SKILL.md`** stable; **promote** repeated lessons into **`SKILL.md`** or [reference.md](./reference.md).

**Do not** delete or rewrite old entries — **append only**.

---

## How to use

**Before** adding state row colors, `ApplicationProgress` appearance, or `BoStateAppearanceColors`: skim **## Entries**.

**After** verified work (manual ListView check, build pass, confirmed appearance bug):

1. Append one entry using the template below.
2. Tag **Outcome**: `positive`, `negative`, or `anti-pattern`.

---

## Entry template

```markdown
### YYYY-MM-DD — [+/−] <short title> (<BO name | Application | registrar>)

- **Outcome**: positive | negative | anti-pattern
- **Context**: (ListView id, nested vs root, Appearance vs controller)
- **What we tried**:
- **What worked / failed**:
- **Reuse next time**:
- **Promote**: pending | done → SKILL.md | reference.md
```

---

## Entries

### 2026-09-25 — [+] Instance ListView row tint follows Fluent Dark

- **Outcome**: positive
- **Context**: `Application_ListView_ViaMinistries` and other `ApplicationProfileInstance` lists (`visa-progress-row`, `lv-plain-cell`, result chips, progress stepper)
- **What we tried**: Keep the registered pastel as the light-mode row. Under `.dxbl-theme-fluent-mode-dark`, mix `--visa-row-hue` 28% into `--dxds-color-surface-neutral-default-rest` and use theme text. Progress and result columns no longer force `#fff` / `#1b2430`.
- **What worked / failed**: Light pastels stay on the same classes. Dark mode was a light green/white grid because those hexes ignored the Fluent surface. `--bs-body-bg` is not the surface token.
- **Reuse next time**: Do not paint instance-list rows, chips, or the progress stepper with raw light hex when the shell is Fluent Dark. Add `--visa-row-hue` on any new `visa-progress-row--state-*` rule so the dark mix picks it up.
- **Promote**: pending

### 2026-08-14 — [+] Case workspace progress stepper uses registry tones for terminal outcomes

- **Outcome**: positive
- **Context**: Application workspace Overview + Progress tab (not ListView `[Appearance]`)
- **What we tried**: Map `PROCESS_ISSUED` / `PROCESS_REJECTED` / `PROCESS_CANCELLED` / `*_REVIEW_REJECTED` to `OutcomeKind` and CSS from `BO_STATE_COLORS.md` (Green T6, Red T6′, Red T6). Approved/completed stay mint green.
- **What worked / failed**: Unit tests for `ResolveOutcomeKind`. Build passed. Manual F5 pending.
- **Reuse next time**: Do not color workspace steppers from slot `done` alone; read the progress `State.Code`.
- **Promote**: pending
