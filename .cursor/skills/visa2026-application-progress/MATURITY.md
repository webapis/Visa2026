# Application progress skill — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) · **File map:** [reference.md](./reference.md)

**Canonical docs:** [`docs/APPLICATION_PROGRESS_STATE_VALIDATION.md`](../../../docs/APPLICATION_PROGRESS_STATE_VALIDATION.md), [`docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md`](../../../docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md)

**Related experience logs:**

| Topic | Log |
|-------|-----|
| ListView row colors, registry | [visa2026-bo-state-colors/learnings.md](../visa2026-bo-state-colors/learnings.md) |
| Lookup seed / tenant JSON | [visa2026-lookup-data/SKILL.md](../visa2026-lookup-data/SKILL.md) |
| Docker / schema deploy | [visa2026-lifecycle-docker/SKILL.md](../visa2026-lifecycle-docker/SKILL.md) |

Shared promotion rules: [`docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md`](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)

---

## The loop

```text
1. READ   → learnings.md + Scenarios in SKILL.md
2. CLASSIFY → workflow (this skill) vs colors (bo-state-colors) vs deploy (lifecycle-docker)
3. TRY    → smallest repro: save blocked? missing column? wrong next step?
4. TEST   → dotnet build; optional Module.Tests; manual officer path on progress detail
5. FIX    → Module first; Blazor only for grid appearance
6. RECORD → append learnings.md (verified only)
7. PROMOTE → 2× same root cause → Scenarios row in SKILL.md
```

---

## Promotion ladder

| Hits | Action |
|------|--------|
| **1** verified fix | Append **learnings.md** |
| **2** same root cause | Update **Scenarios** in SKILL.md |
| **3+** or officer-blocking | Add **reference.md** section or triage bullet |
| Officer-facing rule change | Update canonical **docs/APPLICATION_PROGRESS_*.md** |

---

## Entry template

```markdown
### YYYY-MM-DD — <short title>

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: application-progress | bo-state-colors | lifecycle-docker | lookup-data | —
```
