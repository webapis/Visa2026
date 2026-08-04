# User manual skill — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Tracking:** [tracking.md](./tracking.md) · **Log:** [learnings.md](./learnings.md) · **Reference:** [reference.md](./reference.md)

**Canonical plan:** [docs/USER_MANUAL_IMPLEMENTATION_PLAN.md](../../../docs/USER_MANUAL_IMPLEMENTATION_PLAN.md)

**Lifecycle:** create · update · plan · fix · track — all owned by this skill; use **tracking.md** for current state.

**Before any implementation:** [advisory.md](./advisory.md) — when to document, options menu, quality gates.

---

## Which skill owns the work?

| Symptom / work | Owner |
|----------------|--------|
| Manual site, guides, catalog, publish | **visa2026-user-manual** | — |
| E2E scenario / video / screenshots | **visa2026-easytest-e2e** | [USER_MANUAL_E2E_MEDIA.md](../../../docs/USER_MANUAL_E2E_MEDIA.md) |
| Officer-facing text inside the app (localization) | **visa2026-lookup-data** / Module `UiStrings` — not the manual site |
| Developer implementation plans in `docs/` | Leave in `docs/`; adapt excerpts into guides |

When fixing UI for a guide screenshot, log feature detail in the **feature** skill's learnings; log manual pipeline notes here.

---

## Promotion ladder

| Hits | Action |
|------|--------|
| **1** verified doc/CI pass | Append **learnings.md** |
| **2** same advisory gap (user jumped to code without phase check) | Add row to **advisory.md** §9 anti-patterns |
| **2** same drift root cause | Add **CI rule** or checklist in **SKILL.md** |
| **3+** | **reference.md** snippet (command, frontmatter, path) |
| Phase complete | **tracking.md** phase row + checkboxes in **USER_MANUAL_IMPLEMENTATION_PLAN.md** |
| Guide published / fixed | **tracking.md** inventory + **learnings.md** |
| Pilot guides shipped | Link from **AGENTS.md** if not already |

---

## Phase completion signals

| Phase | Done when |
|-------|-----------|
| **0** | `mkdocs build` green; skill paths exist on disk |
| **1** | PR fails on invalid `bo:`; `bo-catalog.json` lists pilot types |
| **2** | Five pilot guides `status: published`; officer sign-off |
| **3** | Manual URL live; ≥1 CI screenshot |
| **4** | tk-TM assets for top guides; warn on missing `[UserDocumentation]` |
| **5** | Help action opens correct slug on Person + Application |
