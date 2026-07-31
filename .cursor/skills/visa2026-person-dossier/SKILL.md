---
name: visa2026-person-dossier
description: >-
  Person dossier (dosye): read-only 360 page, Screen/Paper views, staged loading,
  Open dossier entry points, director hand-over export (PersonExportBatch ZIP).
  Use for dossier UI/resolver/export bugs, Phase 2 section deep-link to copies,
  or designing dossier sections — not Person search SQL (visa2026-report-dashboard),
  copies catalog content (visa2026-person-document-copies), or preview-slot shell
  (visa2026-preview-slot). Read learnings.md first; append after verified fixes.
disable-model-invocation: false
---

# Visa2026 — Person dossier

**User prompts:** [prompts.md](./prompts.md) (`@visa2026-person-dossier`).

## Agent workflow (every task — mandatory)

1. **Read** [`docs/PERSON_DOSSIER.md`](../../../docs/PERSON_DOSSIER.md) and [learnings.md](./learnings.md) (**## Entries**, newest first).
2. **Classify** — dossier page / resolver / export (**this skill**) vs Person search (**[report-dashboard](../visa2026-report-dashboard/SKILL.md)**) vs copies catalog (**[person-document-copies](../visa2026-person-document-copies/SKILL.md)**) vs slot shell (**[preview-slot](../visa2026-preview-slot/SKILL.md)**).
3. **Implement** with minimal diff; respect **Design rules** below.
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; open dossier from Person + from Person search; Document copies stay in the **slot** beside the dossier.
5. **Record** — append [learnings.md](./learnings.md) after **verified** fix ([MATURITY.md](./MATURITY.md)).
6. **Promote** — same root cause twice → **Scenarios** row or [reference.md](./reference.md).

## Canonical doc

**[`docs/PERSON_DOSSIER.md`](../../../docs/PERSON_DOSSIER.md)**

**Related skills (do not duplicate):**

| Topic | Skill |
|-------|--------|
| Person search (`vw_rd_person_search`), row → dossier | [visa2026-report-dashboard](../visa2026-report-dashboard/SKILL.md) |
| Person copies catalog / preview in slot | [visa2026-person-document-copies](../visa2026-person-document-copies/SKILL.md) |
| `#visa-preview-slot` shell, owner-close | [visa2026-preview-slot](../visa2026-preview-slot/SKILL.md) |
| Typed Person DetailView tabs (editable) | [`PERSON_DETAIL_NESTED_COLLECTION_TABS.md`](../../../docs/PERSON_DETAIL_NESTED_COLLECTION_TABS.md) |
| Roles / read-only director role (if ever) | [visa2026-security-access](../visa2026-security-access/SKILL.md) |

**Long reference:** [reference.md](./reference.md). **Experience log:** [learnings.md](./learnings.md). **Maturity:** [MATURITY.md](./MATURITY.md).

---

## Design rules (do not violate)

1. **Main area, not preview slot** — dossier is an XAF DetailView over `PersonDossierHost`. Putting it in `#visa-preview-slot` would evict Document copies (one occupant).
2. **Not a Report Dashboard panel** — one person, no Status buckets / Preview↔ListView parity. Search entry only is a dashboard category.
3. **Read-only** — officers edit on typed Person DetailView; dossier never writes domain data.
4. **Copies `OwnerViewId` = dossier view** — `PersonDossierViewIds.DetailView` (or `VisaPreviewSlotViewHelper.ResolveOwnerViewId`). Wrong owner → slot closes when navigating search → dossier.
5. **Director export ≠ ministry ZIP** — `PersonExportBatch` / packer / toast. Do **not** route through `PdfGenerationBatch`.
6. **Paper = export HTML fragment** — `PersonDossierDocumentHtmlBuilder.BuildFragment` in A4 chrome; do not open the preview slot for Paper (keeps copies beside dossier).
7. **Loading feedback** — staged `LoadingMessage` + percent (+ skeleton). Use `Task.Delay(16)` before heavy resolve so Blazor paints; mirror Report Dashboard pattern.
8. **Localization** — `PersonDossier.*` in `UiStrings.messages.json` (en / tr-TR / tk-TM / ru-RU). Export worker has **no** XAF `CaptionHelper` — use `PersonDossierResolver.LOr` / message keys for PDF text.

---

## Scope (this skill)

| In scope | Out of scope |
|----------|----------------|
| `PersonDossierResolver` / snapshot DTOs | `vw_rd_person_search` SQL / fold normalizer |
| `PersonDossierComponent` / PropertyEditor / CSS | Person copies catalog resolver rows |
| Screen \| Paper toggle | Preview-slot resize / theme / CSS shell |
| Open dossier actions + `PersonDossierOpenHelper` | Typed Person nested tab editing |
| Staged loading overlay | Ministry `PdfGenerationBatch` |
| `PersonExportBatch*` director ZIP | Resminamalar Word packages |
| Phase 2: per-section deep-link into copies | ApplicationItem document copies |

---

## Phases

| Phase | Focus | Status |
|-------|--------|--------|
| **1** | Snapshot + page + Open dossier | **Shipped** |
| **2** | Toolbar copies + **per-row / section deep-link** into person copies | **Partial** (toolbar done) |
| **3** | Person search → dossier | **Shipped** (owned with report-dashboard) |
| **4** | Director export ZIP | **Shipped** |

---

## Scenarios (promoted — check first)

| Symptom | First step | Likely owner |
|---------|------------|--------------|
| Dossier opens then copies slot closes | `OwnerViewId` must be dossier DetailView id | **This skill** + preview-slot |
| Stuck "Opening dossier… 0%" on dashboard after close | `_localLoading` cleared in `finally` on row select | **report-dashboard** |
| Plain "Loading dossier…" / no progress | Staged load in `PersonDossierPropertyEditor.LoadAsync` | **This skill** |
| Paper empties preview slot / kills copies | Paper must use in-page `BuildFragment`, not slot PDF | **This skill** |
| Export ZIP puts visas under Passports/ | `PersonExportPacker.FolderKeyByRecordType` | **This skill** |
| Export PDF English enums | `PersonDossier.*` keys + `LOr`; worker has no CaptionHelper | **This skill** |
| Wrong sections for family / temp visitor | Role-aware `BuildSections` (mirror Person Appearance) | **This skill** |
| Controller on host never runs | Prefer `View.Id` match, not `ObjectViewController<, PersonDossierHost>` | **This skill** |
| Search term misses ü/ğ | Fold / `vw_rd_person_search` | **report-dashboard** |
| Catalog empty / preview narrow | Copies content vs slot CSS | **person-document-copies** / **preview-slot** |

---

## Modelling traps (short)

1. **No `Person.Visas`** — flatten `Passports` → `Visas`; current via `PersonCurrentItems.GetCurrentVisa`.
2. **No `Registration` collection** — Applications vs TravelHistories vs AddressesOfResidence.
3. **`Person.Photo` is `byte[]`** — data URI; no FileData round trip.
4. Non-persistent host: chrome controller = plain `ViewController` on `View.Id`; re-hide CRUD in `OnViewControlsCreated`.

Details: [`PERSON_DOSSIER.md`](../../../docs/PERSON_DOSSIER.md) § Modelling traps.

---

## Build / verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual: Person → Open dossier → progress panel → tiles/sections → Document copies in **right** slot → Screen/Paper → Export for director (toast). From Report Dashboard Person search → row → dossier.

---

## Recording experience

| After verified fix | Action |
|--------------------|--------|
| Dossier page, resolver, export, loading, Paper, open helpers | Append [learnings.md](./learnings.md) |
| Person search / fold / dashboard row hand-off | [report-dashboard/learnings.md](../visa2026-report-dashboard/learnings.md) |
| Copies catalog rows / preview bytes | [person-document-copies/learnings.md](../visa2026-person-document-copies/learnings.md) |
| Slot width / owner-close / occupant | [preview-slot/learnings.md](../visa2026-preview-slot/learnings.md) |