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

### 2026-08-14 — [+] Case workspace progress stepper uses registry tones for terminal outcomes

- **Outcome**: positive
- **Context**: Application workspace Overview + Progress tab (not ListView `[Appearance]`)
- **What we tried**: Map `PROCESS_ISSUED` / `PROCESS_REJECTED` / `PROCESS_CANCELLED` / `*_REVIEW_REJECTED` to `OutcomeKind` and CSS from `BO_STATE_COLORS.md` (Green T6, Red T6′, Red T6). Approved/completed stay mint green.
- **What worked / failed**: Unit tests for `ResolveOutcomeKind`. Build passed. Manual F5 pending.
- **Reuse next time**: Do not color workspace steppers from slot `done` alone; read the progress `State.Code`.
- **Promote**: pending
