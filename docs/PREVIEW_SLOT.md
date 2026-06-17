# Global preview slot (`#visa-preview-slot`)

Officer-facing **right-side panel** for inline catalogs and document preview. Replaces modal DetailViews for **Resminamalar**, **Document copies**, and **Progress ministry letters**, and hosts **file preview** (FileData / ministry letter links).

**Agent skill:** [`.cursor/skills/visa2026-preview-slot/SKILL.md`](../.cursor/skills/visa2026-preview-slot/SKILL.md)

## Why one slot

| Before | After |
|--------|--------|
| Resminamalar modal + separate file drawer | One resizable column beside main app |
| Document copies modal | Same shell, shared catalog card UX |
| Ministry letter filename → modal or split pane | `ProgressLetters` occupant in same slot |

**Design rule:** **one occupant at a time** — last `Open*` wins; `@key` on panel remounts local state (`_previewActive`, catalog selection).

## Shell behaviour

- **Layout:** `#visa-app-shell` CSS grid — main app `1fr` + slot width (`--visa-preview-slot-width`, default ~50vw).
- **Resize:** drag handle on slot left edge; width persisted in `sessionStorage` (`visa.previewSlot.widthPx`).
- **Nav:** while open, left nav collapses; expanding nav closes slot.
- **Theme:** slot renders outside `<app>` — `visaPreviewDrawer.syncSlotTheme()` copies DevExpress/Bootstrap CSS variables on open.
- **Close:** panel X button, `CloseAsync`, nav expand, or **owner-aware** auto-close when owning XAF `View` deactivates (`VisaPreviewSlotCloseController` + `OwnerViewId`).

## Occupants (`VisaPreviewSlotMode`)

| Mode | Panel | Open API | Domain doc |
|------|-------|----------|------------|
| `Resminamalar` | `ResminamalarSlotPanel` | `OpenResminamalarAsync` | [`APPLICATION_REPORT_PACKAGE.md`](APPLICATION_REPORT_PACKAGE.md) |
| `DocumentCopies` | `DocumentCopiesSlotPanel` | `OpenDocumentCopiesAsync` | [`APPLICATION_ITEM_DOCUMENT_COPIES.md`](APPLICATION_ITEM_DOCUMENT_COPIES.md) |
| `PersonDocumentCopies` *(planned)* | `PersonDocumentCopiesSlotPanel` | `OpenPersonDocumentCopiesAsync` | [`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md) |
| `ProgressLetters` | `ProgressLettersSlotPanel` | `OpenProgressLettersAsync` | Application progress ministry letters (controller + catalog builder) |
| `File` | `VisaFilePreviewDrawer` | `OpenFileAsync` / JS bridge | File preview sources registry |

**Occupant keys:** `VisaPreviewSlotOccupantKeys` (e.g. `resminamalar:app:{id}`, `document-copies:items:{ids}`, `file:{source}:{id}`).

## Catalog vs preview (exclusive mode)

Each feature panel uses **`--preview` CSS modifier** when a document is open:

1. **Catalog mode** — elevated card list (top-aligned, full slot width, flexible height, internal scroll on long lists).
2. **Preview mode** — catalog hidden; **viewer uses full slot width** (no catalog `max-width`).

Feature components set `UseInlinePreview="true"` and raise `OnInlinePreviewRequested` → panel sets `_previewActive` → `*InlinePreview` child.

**Do not** reuse catalog card constraints on `.report-package-inline-preview` in `--preview` mode.

## Shared catalog card UX (2026)

CSS lives under `.resminamalar-slot-panel` (also used by `document-copies-slot-panel`, `progress-letters-slot-panel`):

- CSS variables: `--resminamalar-slot-inset-x`, `--resminamalar-slot-content-max`, `--resminamalar-slot-surface`, etc.
- Inline catalog classes: `.app-report-package--inline-slot` (Resminamalar), `.app-item-doc-copies--inline-slot` (document copies).
- Footer band: Download package / Refresh / gear — sticky inside card, not slot bottom float.

## File map (shell only)

| Area | Path |
|------|------|
| Service + state | `Visa2026.Module/Services/PreviewSlot/` (`IVisaPreviewSlotService`, requests, `VisaPreviewSlotOccupantKeys`) |
| Host impl | `Visa2026.Blazor.Server/Services/VisaPreviewSlotService.cs` |
| Blazor host | `Visa2026.Blazor.Server/Components/VisaPreviewSlotHost.razor` |
| Slot panels | `ResminamalarSlotPanel.razor`, `DocumentCopiesSlotPanel.razor`, `ProgressLettersSlotPanel.razor` |
| Inline previews | `ReportPackageInlinePreview.razor`, `DocumentCopiesInlinePreview.razor`, `ProgressLettersInlinePreview.razor` |
| Close policy | `Visa2026.Blazor.Server/Controllers/VisaPreviewSlotCloseController.cs` |
| Shell markup | `Visa2026.Blazor.Server/Pages/_Host.cshtml` (`#visa-preview-slot`, `visaPreviewDrawer.*` JS) |
| Shell CSS | `Visa2026.Blazor.Server/wwwroot/css/site.css` (`.visa-preview-slot*`, `.resminamalar-slot-panel*`) |
| Wiring | `Startup.cs` — register `IVisaPreviewSlotService` |

Feature-specific catalog logic stays in feature components and Module services — see domain docs above.

## Adding a new occupant (checklist)

1. Add `VisaPreviewSlotMode` value + request DTO + `Open*Async` on `IVisaPreviewSlotService`.
2. Add `VisaPreviewSlotOccupantKeys.For*` stable key; bump `Version` on open.
3. Create `*SlotPanel.razor` (reuse `resminamalar-slot-panel` classes for catalog card).
4. Branch in `VisaPreviewSlotHost.razor` with `@key="_state.Version"`.
5. Controller calls `Open*Async(..., VisaPreviewSlotViewHelper.ResolveOwnerViewId(View))`.
6. CSS: catalog card under `.resminamalar-slot-panel`; preview full width under `--preview`.
7. Update this doc + [visa2026-preview-slot/reference.md](../.cursor/skills/visa2026-preview-slot/reference.md).
