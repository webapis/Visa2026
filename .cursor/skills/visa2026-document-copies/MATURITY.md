# Document copies skill — continuous improvement

**Skill:** [SKILL.md](./SKILL.md) · **Log:** [learnings.md](./learnings.md) · **Files:** [reference.md](./reference.md)

**Canonical doc:** [docs/APPLICATION_ITEM_DOCUMENT_COPIES.md](../../../docs/APPLICATION_ITEM_DOCUMENT_COPIES.md)

**Related logs:**

| Topic | Log |
|-------|-----|
| XFA field mapping, empty PDF fields | [visa2026-pdf-form-mapping/learnings.md](../visa2026-pdf-form-mapping/learnings.md) |
| Resminamalar row progress pattern | [visa2026-resminamalar/learnings.md](../visa2026-resminamalar/learnings.md) |
| Batch worker / Docker / schema | [visa2026-lifecycle-docker/SKILL.md](../visa2026-lifecycle-docker/SKILL.md) |

**User prompts:** [prompts.md](./prompts.md)

---

## Which skill owns the entry?

| Symptom / work | Log to |
|----------------|--------|
| Dialog layout, preview scans, gap confirm, package options, toast registration, scan merge preview | **visa2026-document-copies** |
| Application form PDF **field content** empty/wrong | **visa2026-pdf-form-mapping** |
| `PdfGenerationBatch` worker crash, schema, deploy | **lifecycle-docker** |
| Word / Resminamalar | **visa2026-resminamalar** |

When unsure: log under the skill where you **changed code**; add **Cross-skill** in the other folder if officers reported via Document copies.

---

## Promotion ladder

| Hits | Action |
|------|--------|
| **1** verified fix | Append **learnings.md** only |
| **2** same root cause | Add/update **Scenarios** in **SKILL.md** |
| **3+** | Triage line or **reference.md** snippet |
| Officer-visible change | Update **APPLICATION_ITEM_DOCUMENT_COPIES.md** |

Shared rules: [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)
