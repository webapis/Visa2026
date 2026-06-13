# PDF form mapping skill — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) · **Files:** [reference.md](./reference.md)

**Canonical docs:** [PdfFormMapping.md](../../../Visa2026.Module/BusinessObjects/PdfFormMapping.md), [PDF-Form-Filling.md](../../../Visa2026.Module/Services/PDF-Form-Filling.md)

**Related logs:**

| Topic | Log |
|-------|-----|
| Document copies dialog / download UX | [visa2026-document-copies/learnings.md](../visa2026-document-copies/learnings.md) |
| ApplicationType Show* / lookup | [visa2026-lookup-data/SKILL.md](../visa2026-lookup-data/SKILL.md) |
| Batch worker packaging | [visa2026-lifecycle-docker/SKILL.md](../visa2026-lifecycle-docker/SKILL.md) |

**User prompts:** [prompts.md](./prompts.md)

---

## Which skill owns the entry?

| Symptom / work | Log to |
|----------------|--------|
| `PdfFormMapping`, `PdfMappingHelper`, gate, filler, template, converters, generator | **visa2026-pdf-form-mapping** |
| Preview modal, row progress, gap confirm, scan slots | **visa2026-document-copies** |
| ApplicationType visibility flags affecting gate | **lookup-data** (+ cross-link here) |

---

## Promotion ladder

| Hits | Action |
|------|--------|
| **1** verified fix | Append **learnings.md** only |
| **2** same root cause | **Scenarios** row in **SKILL.md** |
| **3+** | **Triage** / **reference.md** / update **PdfFormMapping.md** |
| New mapping seed pattern | Document in updater + learnings |
