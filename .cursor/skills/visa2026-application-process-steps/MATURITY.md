# Application process steps — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) · **File map:** [reference.md](./reference.md)

**Canonical doc:** [`docs/APPLICATION_PROFILE_PROGRESS_STEPS.md`](../../../docs/APPLICATION_PROFILE_PROGRESS_STEPS.md)

## The loop

```text
1. READ     → learnings.md + the progress-step matrix
2. CLASSIFY → this skill vs application-progress vs application-profile vs bo-state-colors
3. CHANGE   → seed ProgressSteps, timeline/completion code, and the matrix doc together
4. TEST     → isolated build; Module.Tests when the gate or visibility changes
5. RECORD   → append learnings.md (verified only)
6. PROMOTE  → 2× same root cause → checklist or Which skill row in SKILL.md
```

## Promotion ladder

| Hits | Action |
|------|--------|
| **1** verified change | Append **learnings.md** |
| **2** same root cause | Update the checklist or Which skill table in SKILL.md |
| **3+** | Add a path or failure note in **reference.md** |
| Officer-facing visibility or completion change | Update `docs/APPLICATION_PROFILE_PROGRESS_STEPS.md` and, when color precedence changes, `docs/APPLICATION_PROFILE_LISTVIEW_PROGRESS_COLORS.md` |
