---
name: visa2026-user-report-template-editing
description: >-
  In-app officer editing of UserReportTemplate (Word/Excel) in the preview slot TemplateEditor
  occupant — DxRichEdit save to FileData, Extract/Validate, Excel upload strip (Phase 1), Univer POC
  (Phase 1b), Univer embed or ONLYOFFICE Community fallback (Phase 3). Use when implementing or
  debugging Edit template from Resminamalar, TemplateEditorSlotPanel, OpenTemplateEditorAsync,
  IUserReportTemplateMaintenanceService, in-browser Excel strategy, or officer template save flow —
  not for git seeds/maps (visa2026-user-report-templates), slot shell CSS only (visa2026-preview-slot),
  or Resminamalar catalog/ZIP (visa2026-resminamalar). Read learnings.md first; append after verified fixes.
disable-model-invocation: false
---

# Visa2026 — User report template in-app editing

**Status:** Planned — implement per phase checklist below. **Canonical plan:** [`docs/USER_REPORT_TEMPLATE_IN_APP_EDITING_PLAN.md`](../../../docs/USER_REPORT_TEMPLATE_IN_APP_EDITING_PLAN.md) (v0.3+).

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) (**## Entries**, newest first) and **Scenarios** below.
2. **Read** plan §5–§7 for current phase scope — do not ship Phase 3 before Phase 1b POC is logged in plan §7.5.
3. **Classify** ownership (table below) — minimal diff in the right project/skill.
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; manual: Resminamalar → gear → Edit template → save → Extract/Validate toast → back → preview.
5. **Record** — append [learnings.md](./learnings.md) after **verified** fix ([MATURITY.md](./MATURITY.md)).
6. **Promote** — same root cause twice → **Scenarios** row or [reference.md](./reference.md).

## Canonical doc

**[`docs/USER_REPORT_TEMPLATE_IN_APP_EDITING_PLAN.md`](../../../docs/USER_REPORT_TEMPLATE_IN_APP_EDITING_PLAN.md)** — phases, file map, Excel OSS strategy (Univer / ONLYOFFICE), security.

**Related skills (do not duplicate):**

| Topic | Skill |
|-------|--------|
| `#visa-preview-slot` shell, occupant keys, full-width CSS | [visa2026-preview-slot](../visa2026-preview-slot/SKILL.md) |
| Resminamalar gear link, replace `target="_blank"` edit | [visa2026-resminamalar](../visa2026-resminamalar/SKILL.md) |
| Git seeds, `*_map.md`, embed, merge families | [visa2026-user-report-templates](../visa2026-user-report-templates/SKILL.md) |
| ClosedXML merge pipeline (already shipped) | [`docs/EXCEL_TEMPLATE_REPORTING_PLAN.md`](../../../docs/EXCEL_TEMPLATE_REPORTING_PLAN.md) |
| Officer placeholder rules | [`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`](../../../docs/USER_TEMPLATE_AUTHOR_GUIDE.md) |
| ONLYOFFICE Docker on Ubuntu | [setup-docker-engine](../setup-docker-engine/SKILL.md) |

**Long reference:** [reference.md](./reference.md). **Experience log:** [learnings.md](./learnings.md). **Maturity:** [MATURITY.md](./MATURITY.md).

---

## Skill ownership

| Owns (**this skill**) | Does **not** own |
|------------------------|------------------|
| `VisaPreviewSlotMode.TemplateEditor`, `OpenTemplateEditorAsync` | Resminamalar readiness chips, ZIP batch |
| `TemplateEditorSlotPanel.razor`, Word/Excel editor bodies | Catalog card layout (`visa2026-preview-slot`) |
| `IUserReportTemplateMaintenanceService` (save, Extract/Validate) | `Resources/Templates/*` seeds in git |
| `DxRichEdit` load/save → `UserReportTemplate.TemplateFile` | Code-backed `FormTemplates/` Word reports |
| Excel Phase 1 upload strip; Phase 1b POC; Phase 3 Univer/ONLYOFFICE | ClosedXML merge implementation details |
| Unsaved-changes confirm on Back / occupant switch | `PdfFormMapping`, document copies |

---

## Scenarios (promoted — check first)

| Symptom | First step | Likely owner |
|---------|------------|--------------|
| Edit template opens new tab / DetailView | Wire gear to `OpenTemplateEditorAsync` | **resminamalar** + **this skill** |
| Rich Edit blank or save does not persist | `FileData` Write permission; `CommitChanges`; stream dispose | **this skill** |
| Placeholders missing after save | `ExtractAndValidateAsync` not called or failed silently | **this skill** |
| Editor narrow / wrong theme | Slot `--preview` full width; `syncSlotTheme` | **preview-slot** |
| Excel in-browser not ready | Phase 1 = upload strip only; check plan §7 | **this skill** |
| Univer round-trip breaks `{{#ds.rows}}` | Record in plan §7.5; do not ship Phase 3a | **this skill** |
| ONLYOFFICE callback 403 / AGPL concern | Legal sign-off + auth on file/callback endpoints | **this skill** + **setup-docker-engine** |
| New ministry seed in git | Map + embed workflow | **user-report-templates** |

---

## Phase gates (do not skip)

| Phase | Ship when | Blockers |
|-------|-----------|----------|
| **1** | Word `DxRichEdit` + Excel upload strip + back to Resminamalar | Plan §6 |
| **1b** | POC note in plan §7.5 on both Excel seeds | `433_gurlusyk_ckl.xlsx`, `Sanaw_hasaba_alys.xlsx` |
| **3a** | POC **pass** | Univer embed in slot |
| **3b** | POC **fail** + AGPL sign-off | ONLYOFFICE Community Docker |
| **2 / 4** | After Phase 1 stable | Duplicate, demote upload, revisions |

**Excel OSS (decided):** Univer primary (Apache-2.0); ONLYOFFICE Community fallback (AGPL). No commercial grids; no DevExpress Blazor Spreadsheet.

---

## Hard boundaries

- **Officer path:** edit **DB `FileData`** only — never “fix” officer templates in `Resources/Templates/` (see **user-report-templates**).
- **Developers:** maintain placeholder reference docs when BO aliases change; optional one-time seeds only.
- **Save pipeline:** persist bytes → **Extract** → **Validate** → toast summary (default: auto after save).
- **Return navigation:** `template-editor:{id}:return:resminamalar:…` — Back restores prior Resminamalar occupant.
- **Phase 1 Excel:** upload strip is **interim** — do not block Word editor waiting for Univer.

---

## Build / verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual (Phase 1): Application → Resminamalar → gear → **Edit template** → edit Word → **Save** → validation summary → **Back to reports** → preview same template.

---

## Recording experience

| After verified fix | Action |
|--------------------|--------|
| Editor save, Extract/Validate, Univer/ONLYOFFICE | Append [learnings.md](./learnings.md) |
| Slot panel / occupant switch for TemplateEditor | [preview-slot/learnings.md](../visa2026-preview-slot/learnings.md) + **Cross-skill** |
| Gear link / catalog entry | [resminamalar/learnings.md](../visa2026-resminamalar/learnings.md) |
| Officer-visible workflow | Update plan + [`USER_TEMPLATE_AUTHOR_GUIDE.md`](../../../docs/USER_TEMPLATE_AUTHOR_GUIDE.md) when Phase 1 ships |
| POC result | Update plan **§7.5** decision log |

**Do not** append speculative fixes. **Do not** delete old learnings entries.
