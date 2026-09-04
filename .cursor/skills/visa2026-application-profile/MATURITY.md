# Application Profile skill — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) · **Tracker:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

**Canonical doc:** [`docs/APPLICATION_PROFILE_PLAN.md`](../../../docs/APPLICATION_PROFILE_PLAN.md)

**Related experience logs:**

| Topic | Log |
|-------|-----|
| Progress transitions, contract legs | [visa2026-application-progress/learnings.md](../visa2026-application-progress/learnings.md) |
| ApplicationType JSON / lookup seed | [visa2026-lookup-data/SKILL.md](../visa2026-lookup-data/SKILL.md) |
| Resminamalar / Word templates on profile | [visa2026-resminamalar/learnings.md](../visa2026-resminamalar/learnings.md) |
| VISA2014 import / Type mapping | [visa2014-to-visa2026-import/learnings.md](../visa2014-to-visa2026-import/learnings.md) |
| Docker / schema deploy | [visa2026-lifecycle-docker/SKILL.md](../visa2026-lifecycle-docker/SKILL.md) |

Shared promotion rules: [`docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md`](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)

---

## The loop

```text
1. READ    → learnings.md + IMPLEMENTATION_PLAN.md + Scenarios in SKILL.md
2. CLASSIFY → profile (this skill) vs progress vs lookup vs resminamalar vs import
3. LOCK    → re-check APPLICATION_PROFILE_PLAN.md §2 before binding/default changes
4. TRY     → smallest repro: seed gap? lock not enforced? wrong visibility?
5. TEST    → dotnet build; manual create + lock path; seed idempotency on second deploy
6. FIX     → Module first; Blazor for wizard/picker/M2M DetailView
7. RECORD  → append learnings.md (verified only)
8. TRACK   → IMPLEMENTATION_PLAN.md + plan §12 when slice moves
9. SUGGEST → officer config questions → SKILL.md Configuration suggestions
10. PROMOTE → 2× same root cause → Scenarios row in SKILL.md
```

---

## Which skill owns the entry?

| Symptom / work | Log to |
|----------------|--------|
| Profile BO, seed, lock, wizard, picker, M2M, defaults | **visa2026-application-profile** |
| Illegal progress transition, ministry letter | **visa2026-application-progress** |
| ApplicationType catalog JSON maintenance | **visa2026-lookup-data** |
| Template ZIP / Resminamalar catalog | **visa2026-resminamalar** |
| Legacy import sets Type not Profile | **visa2014-to-visa2026-import** |
| Missing column after deploy | **visa2026-lifecycle-docker** |

When unsure: log where you **changed code**; add **Cross-skill** in the other folder.

---

## Promotion ladder

| Hits | Action |
|------|--------|
| **1** verified fix / slice lesson | Append **learnings.md** |
| **2** same root cause | Update **Scenarios** in **SKILL.md** |
| **3+** or officer-blocking | Add **reference.md** section or configuration suggestion |
| Slice ships | **IMPLEMENTATION_PLAN.md** + **APPLICATION_PROFILE_PLAN.md** §12 |
| Officer-facing rule change | Update **APPLICATION_PROFILE_PLAN.md** §2 (locked decisions) |

---

## Entry template

```markdown
### YYYY-MM-DD — <short title>

- **Symptom**:
- **Slice**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: application-profile | application-progress | lookup-data | resminamalar | visa2014-import | lifecycle-docker | —
```
