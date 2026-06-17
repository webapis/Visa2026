# User report template in-app editing — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) · **Reference:** [reference.md](./reference.md)

**Canonical plan:** [docs/USER_REPORT_TEMPLATE_IN_APP_EDITING_PLAN.md](../../../docs/USER_REPORT_TEMPLATE_IN_APP_EDITING_PLAN.md)

Shared promotion rules: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)

---

## The loop

```text
1. READ   → learnings.md + Scenarios in SKILL.md + plan phase gate
2. CLASSIFY → this skill vs preview-slot vs resminamalar vs user-report-templates
3. TRY    → Resminamalar → Edit template → save → validate → back → preview
4. TEST   → dotnet build; hard-refresh after site.css / new Blazor components
5. FIX    → minimal diff; respect phase gates (no Phase 3 before 1b POC)
6. RECORD → append learnings.md (verified only); POC → plan §7.5
7. PROMOTE → 2+ same root cause → Scenarios row in SKILL.md
```

## Which skill owns the entry?

| Symptom | Log to |
|---------|--------|
| Save, Rich Edit, Extract/Validate, Univer/ONLYOFFICE | **visa2026-user-report-template-editing** |
| Slot width, theme, occupant lifecycle | **visa2026-preview-slot** |
| Gear link, catalog, ZIP, readiness | **visa2026-resminamalar** |
| Git seed, map, embed, merge row builder | **visa2026-user-report-templates** |
