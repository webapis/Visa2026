---
name: visa2026-resminamalar
description: >-
  Resminamalar report package dialog (Application + ApplicationItem): catalog, readiness chips,
  checkboxes, in-app PDF preview, ZIP batch enqueue, WordReportGenerationBatch worker, empty template
  list / UserReportTemplateSeedGate, permissions, gear toggle, gap confirm, UX improvements.
  User templates only (UserReportTemplate from Resources/Templates). Use for Resminamalar bugs,
  batch ZIP failures, preview errors, template missing from catalog, officer workflow UX — not for
  authoring new .docx/.xlsx seeds (visa2026-user-report-templates). Always read learnings.md first;
  append after verified fixes; skill accumulates experience per MATURITY.md. User prompts:
  prompts.md.
disable-model-invocation: false
---

# Visa2026 — Resminamalar (application report package)

**User prompts:** copy-paste chat openers → [prompts.md](./prompts.md) (`@visa2026-resminamalar`).

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) (**## Entries**, newest first) and **Scenarios** below.
2. **Classify** — dialog/catalog/batch/preview/seed/permissions (**this skill**) vs DocxTemplater merge/token errors (**[user-report-templates](../visa2026-user-report-templates/SKILL.md)**).
3. **Fix** with minimal diff; **preview and ZIP must share** `ApplicationWordReportEntryGenerator`.
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; officer path (preview + subset ZIP) when UI/worker touched.
5. **Record** — append [learnings.md](./learnings.md) after **verified** fix ([MATURITY.md](./MATURITY.md)).
6. **Promote** — same root cause twice → add/update a **Scenarios** row; three times → tighten **Triage** or [reference.md](./reference.md).

## Canonical doc

**[`docs/APPLICATION_REPORT_PACKAGE.md`](../../../docs/APPLICATION_REPORT_PACKAGE.md)** — officer workflow, architecture, file map summary.

**Related skills (do not duplicate):**

| Topic | Skill |
|-------|--------|
| Template seeds, maps, placeholders, merge (`RowNo`, Sanaw, Extract/Validate) | [`.cursor/skills/visa2026-user-report-templates/SKILL.md`](../visa2026-user-report-templates/SKILL.md) |
| Roles / navigation to User Report Template | [`.cursor/skills/visa2026-security-access/SKILL.md`](../visa2026-security-access/SKILL.md) |
| Docker / `FORCE_XAF_DB_UPDATE` / schema columns | [`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`](../visa2026-lifecycle-docker/SKILL.md) |
| Document copies (PDF ZIP, scan preview) | [`.cursor/skills/visa2026-document-copies/SKILL.md`](../visa2026-document-copies/SKILL.md) |
| XFA visa application form mapping | [`.cursor/skills/visa2026-pdf-form-mapping/SKILL.md`](../visa2026-pdf-form-mapping/SKILL.md) |
| Legacy code-backed Word / XtraReports (removed) | [`.cursor/skills/visa2026-word-reports/SKILL.md`](../visa2026-word-reports/SKILL.md) — **deprecated** |

**Long reference:** [reference.md](./reference.md). **Experience log:** append-only [learnings.md](./learnings.md). **Maturity:** [MATURITY.md](./MATURITY.md).

---

## Scenarios (promoted from learnings — check first)

| Symptom | First step | Likely owner |
|---------|------------|--------------|
| **User Report Template** list empty; Resminamalar shows no rows | Restart app; look for `User report template seed completed` in logs; [UserReportTemplateSeedGate](../../../Visa2026.Blazor.Server/Services/UserReportTemplateSeedGate.cs) | **This skill** — see learnings *Empty list* |
| Template exists in DB, missing from dialog | `IsActive`, applicable types/contracts, Application vs Item scope, `IUserReportVisibilityService` | **This skill** + template visibility (user-report-templates) |
| **Check** chip but ZIP “should work” | Gap confirm is optional; hard fail = worker log | **This skill** (UX) vs **user-report-templates** (merge) |
| `'{{…}}' could not be replaced` in batch | `UserReportGenerator` / row builders — not catalog | **user-report-templates** |
| Extract security error from Edit template | `UserReportPlaceholder` permissions, non-secured OS in controller | **This skill** / security-access |
| Preview OK, ZIP wrong or empty | Compare `SelectedReportKeysJson`, `SelectedApplicationItemIdsJson` | **This skill** |
| **Sanaw** preview fails; `RowNo` empty hint | `UsesSingleDocumentItemList` / `BuildSanawyStyleRows` — not labor-contract per-item path | **This skill** + user-report-templates |
| `Invalid column name` on batch table | `BatchWorkerSchemaGate`, updaters, `FORCE_XAF_DB_UPDATE` | **lifecycle-docker** |

---

## Scope (this skill)

| In scope | Out of scope |
|----------|----------------|
| **Resminamalar** dialog UI (`ApplicationReportPackageComponent`) | Designing Word/Excel layout in `.docx`/`.xlsx` |
| Catalog + readiness + selection + preview + enqueue | New `*_map.md` / placeholder tokens |
| `UserReportTemplate` **visibility in catalog** (symptom: missing row) | Seed registration in `UserReportTemplateUpdater` (template skill) |
| `WordReportGenerationBatch` worker / toast / ZIP | [Document copies](../visa2026-document-copies/SKILL.md) / [PDF form mapping](../visa2026-pdf-form-mapping/SKILL.md) |
| `UserReportTemplateSeedGate` (empty list after deploy) | DevExpress XtraReports / `Reports/` (removed) |

**Reports source:** only **`UserReportTemplate`** rows seeded from **`Visa2026.Module/Resources/Templates/`** (+ DB edits). Catalog keys: **`user:{Guid}`** only.

---

## Entry points

| Where | Action | Scope |
|-------|--------|--------|
| **`Application`** DetailView | **Resminamalar** (`GenerateWordReports`) | `WordReportPackageScope.Application` — templates with `RootBoType.Application` |
| **`ApplicationItem`** ListView (multi-select, same application) | **Resminamalar** (`ViewApplicationItemWordReports`) | `WordReportPackageScope.ApplicationItem` — `RootBoType.ApplicationItem` or `Person` |

Controllers: `WordReportsController`, `ApplicationItemWordReportsController`.

---

## Officer workflow (v2)

1. Open dialog → see **user templates** (checkboxes, Ready / Check chips).
2. Optional **gear** (footer): show **Edit template** + readiness hint lines (hidden by default).
3. **Preview** → generate same bytes as ZIP → Office → PDF in popup.
4. **Download package** → gap confirm if checked rows have warnings → `WordReportGenerationBatch` → toast **Download ZIP**.

**Parity rule:** preview and ZIP must use **`ApplicationWordReportEntryGenerator`** only — no second merge path.

---

## Triage (symptom → first check)

| Symptom | First check |
|---------|-------------|
| **User Report Template** list empty / Resminamalar empty | [`UserReportTemplateSeedGate`](../../../Visa2026.Blazor.Server/Services/UserReportTemplateSeedGate.cs) ran? Console: `User report template seed completed`. DEBUG re-seeds every startup. |
| Template in DB but not in dialog | `IsActive`, applicable types/contracts, `IUserReportVisibilityService`, scope (Application vs Item). Template skill § visibility. |
| **Check** chip / gap confirm only | Advisory — does **not** block ZIP unless user cancels confirm. |
| ZIP fails hard (`could not be replaced`, DocxTemplater) | Usually **merge/data** → **user-report-templates** + `UserReportGenerator` / `EnsureSanawyRowsWhenNeeded`. |
| Extract/Validate security error | `UserReportPlaceholder` delete permission + non-secured OS in `UserReportTemplateController`. |
| Preview works, ZIP fails (or reverse) | Same generator — diff is selection keys / item filter JSON; check `SelectedReportKeysJson`, `SelectedApplicationItemIdsJson`. |
| Old batch includes wrong files | Legacy batches with null keys = all applicable; new batches store explicit `user:{id}` array. |
| `Invalid column name` on batch | `BatchWorkerSchemaGate`, `WordReportGenerationBatchSelected*Updater`, or `FORCE_XAF_DB_UPDATE`. |

*(Extend this table when a learnings entry is promoted — see [MATURITY.md](./MATURITY.md).)*

---

## UX / UI changes (checklist)

1. **Blazor:** `ApplicationReportPackageComponent.razor` (+ item property editor if shared).
2. **Localization:** `tools/GenerateModelLocalization/UiStrings.messages.json` → prefix `ApplicationReportPackage.*` (and `ApplicationItemReportPackage.*` if item-specific).
3. Regenerate: `dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj`
4. **Model:** `Model.DesignedDiffs.xafml` — action captions, detail view for hosts.
5. **Permissions:** `DatabaseUpdate/Updater.cs` — host BOs, `UserReportTemplate` officer block.
6. **Docs:** update [`docs/APPLICATION_REPORT_PACKAGE.md`](../../../docs/APPLICATION_REPORT_PACKAGE.md) if behaviour changes.
7. **Verify:** Application + ApplicationItem scopes; preview + ZIP for Word and Excel rows.

---

## Build / verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual: open Application with known templates → Resminamalar → preview one row → download subset ZIP.

---

## Recording experience

| After verified fix | Action |
|--------------------|--------|
| Catalog, seed gate, dialog, preview, batch, permissions, UX | Append [learnings.md](./learnings.md) |
| Placeholder / merge / map / row builder | Append [user-report-templates/learnings.md](../visa2026-user-report-templates/learnings.md) + **Cross-skill** link |
| Same root cause **2+** times | Promote row to **Scenarios** (above) per [MATURITY.md](./MATURITY.md) |
| Officer-visible behaviour change | Update [docs/APPLICATION_REPORT_PACKAGE.md](../../../docs/APPLICATION_REPORT_PACKAGE.md) |

**Do not** append speculative fixes. **Do not** delete or rewrite old learnings entries.
