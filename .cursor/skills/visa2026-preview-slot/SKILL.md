---
name: visa2026-preview-slot
description: >-
  Global #visa-preview-slot shell: Resminamalar, Document copies, Progress letters, and file
  preview occupants; catalog card UX, exclusive preview mode, resize/theme/owner-close, adding new
  slot modes. Use for preview slot layout bugs, catalog vs preview CSS, occupant switching,
  JS resize, theme sync, or designing new inline catalog features — not for Resminamalar ZIP/readiness
  (visa2026-resminamalar), scan merge/package ZIP (visa2026-document-copies), or XFA mapping.
  Read learnings.md first; append after verified fixes.
disable-model-invocation: false
---

# Visa2026 — Global preview slot

**User prompts:** [prompts.md](./prompts.md) (`@visa2026-preview-slot`).

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) (**## Entries**, newest first) and **Scenarios** below.
2. **Classify** — shell / layout / occupant lifecycle (**this skill**) vs feature catalog content (**resminamalar**, **document-copies**, **application-progress**).
3. **Fix** with minimal diff; **catalog styling must not constrain preview mode** (full slot width).
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; hard-refresh after `_Host.cshtml` or `site.css` changes; smoke both **catalog** and **preview** modes.
5. **Record** — append [learnings.md](./learnings.md) after **verified** fix ([MATURITY.md](./MATURITY.md)).
6. **Promote** — same root cause twice → update **Scenarios** or [reference.md](./reference.md).

## Canonical doc

**[`docs/PREVIEW_SLOT.md`](../../../docs/PREVIEW_SLOT.md)** — shell behaviour, occupants, catalog vs preview, file map.

**Related skills (do not duplicate):**

| Topic | Skill |
|-------|--------|
| Resminamalar catalog, readiness, Word ZIP batch | [visa2026-resminamalar](../visa2026-resminamalar/SKILL.md) |
| Document copies slots, scan merge, PDF ZIP | [visa2026-document-copies](../visa2026-document-copies/SKILL.md) |
| Person document copies (planned — master Person catalog) | [visa2026-person-document-copies](../visa2026-person-document-copies/SKILL.md) |
| Person dossier (main area; not a slot occupant) | [visa2026-person-dossier](../visa2026-person-dossier/SKILL.md) |
| ApplicationProgress ministry letters domain | [visa2026-application-progress](../visa2026-application-progress/SKILL.md) |
| User template merge / placeholders | [visa2026-user-report-templates](../visa2026-user-report-templates/SKILL.md) |
| Docker / deploy | [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) |

**Long reference:** [reference.md](./reference.md). **Experience log:** [learnings.md](./learnings.md). **Maturity:** [MATURITY.md](./MATURITY.md).

---

## Scenarios (promoted — check first)

| Symptom | First step | Likely owner |
|---------|------------|--------------|
| Preview PDF narrow / catalog padding on viewer | Check `.resminamalar-slot-panel--preview .report-package-inline-preview` — `max-width: none` | **This skill** |
| Open Resminamalar on items while app slot open — stale PDF | `OccupantKey` change + `@key` Version; panel resets `_previewActive` | **This skill** |
| Slot text invisible (dark theme) | `visaPreviewDrawer.syncSlotTheme()`; slot outside `<app>` | **This skill** |
| Resize handle not draggable | Grid on `#visa-app-shell`, handle `z-index`, `--visa-preview-slot-width` | **This skill** |
| Slot closes when nested ListView still active | `OwnerViewId` + `VisaPreviewSlotCloseController` | **This skill** |
| Catalog empty / Check chips / ZIP fail | Feature domain | **resminamalar** / **document-copies** / **person-document-copies** |
| Preview generates but wrong PDF bytes | Generator / merge path | **resminamalar** / **document-copies** / **pdf-form-mapping** |

---

## Scope (this skill)

| In scope | Out of scope |
|----------|----------------|
| `#visa-preview-slot`, `#visa-app-shell` grid, resize JS | Word/Excel template merge |
| `IVisaPreviewSlotService`, occupant keys, `Version` remount | Readiness evaluators, catalog builders |
| `*SlotPanel.razor`, `*InlinePreview.razor` shell wiring | `PdfFormMapping`, scan eligibility rules |
| Shared catalog card CSS (`.resminamalar-slot-panel*`) | Batch workers (`WordReportGenerationBatch`, `PdfGenerationBatch`) |
| `UseInlinePreview` integration pattern | Modal legacy paths (unless removing) |
| Owner-aware close, theme sync, nav collapse | E2E test authoring (unless slot-specific) |

---

## Design principles (catalog + preview)

1. **One occupant** — single global slot; last `Open*` preempts; distinct `OccupantKey` + bump `Version`.
2. **Two modes per panel** — catalog (list card) vs exclusive preview (viewer only).
3. **Catalog card** — top-aligned, `--resminamalar-slot-content-max: 100%`, flexible height, scroll list body only when needed.
4. **Preview** — full slot width; never inherit catalog `max-width` or centering.
5. **Shared shell class** — `resminamalar-slot-panel` + optional `document-copies-slot-panel`; feature flag via component class (`--inline-slot`).
6. **Parity** — inline preview must call same generate/download APIs as modal/dialog path (owned by feature skills).
7. **Document-copies catalog chrome** — Person / ApplicationItem / Header copies use shared `.doc-copies-catalog*` ([`document-copies-catalog.css`](../../../Visa2026.Blazor.Server/wwwroot/css/document-copies-catalog.css)); see [`PREVIEW_SLOT.md`](../../../docs/PREVIEW_SLOT.md) § Document-copies catalog chrome contract. Do not reintroduce flat `__group` cards for copies.

---

## UX / layout changes (checklist)

1. **CSS:** `wwwroot/css/site.css` — `.visa-preview-slot*`, `.resminamalar-slot-panel*`; scope catalog rules to **non-preview** selectors.
2. **JS:** `_Host.cshtml` — `visaPreviewDrawer.*` (resize, theme, `syncInlinePreviewHeight`).
3. **Panel:** `*SlotPanel.razor` — catalog / preview toggle (`_previewActive`).
4. **Component:** feature `*Component.razor` — `UseInlinePreview`, hide duplicate labels in inline mode if applicable.
5. **Docs:** update [`docs/PREVIEW_SLOT.md`](../../../docs/PREVIEW_SLOT.md) + feature doc if officer-visible.
6. **Verify:** Resminamalar **and** Document copies; short + long catalog; open preview → full width; close preview → catalog returns.

---

## Build / verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual: open each occupant → catalog layout → Preview → Close preview → resize slot → switch theme if available.

---

## Recording experience

| After verified fix | Action |
|--------------------|--------|
| Shell, CSS, JS, occupant switch, theme, resize | Append [learnings.md](./learnings.md) |
| Resminamalar catalog rows / ZIP | [resminamalar/learnings.md](../visa2026-resminamalar/learnings.md) + **Cross-skill** |
| Document copies scans / package | [document-copies/learnings.md](../visa2026-document-copies/learnings.md) |
| Officer-visible shell behaviour | Update [docs/PREVIEW_SLOT.md](../../../docs/PREVIEW_SLOT.md) |

**Do not** append speculative fixes. **Do not** delete old learnings entries.
