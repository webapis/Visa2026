# Preview slot skill — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) · **Reference:** [reference.md](./reference.md)

**Canonical doc:** [docs/PREVIEW_SLOT.md](../../../docs/PREVIEW_SLOT.md)

Shared promotion rules: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)

---

## The loop

```text
1. READ   → learnings.md + Scenarios in SKILL.md
2. CLASSIFY → shell/CSS/JS vs feature catalog (route to resminamalar / document-copies)
3. TRY    → reproduce in catalog mode AND preview mode
4. TEST   → dotnet build; hard-refresh after site.css / _Host.cshtml
5. FIX    → minimal diff; never narrow preview viewer for catalog aesthetics
6. RECORD → append learnings.md (verified only)
7. PROMOTE → 2+ same root cause → Scenarios row in SKILL.md
```

## Which skill owns the entry?

| Symptom | Log to |
|---------|--------|
| Layout, resize, theme, occupant, catalog card, preview width | **visa2026-preview-slot** |
| Resminamalar readiness, ZIP, template visibility | **visa2026-resminamalar** |
| Scan slots, PDF package, application form download | **visa2026-document-copies** |
| Ministry letter file on progress row | **visa2026-application-progress** |
