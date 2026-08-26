# Global preview slot (`#visa-preview-slot`)

Officer-facing **right-side panel** for inline catalogs and document preview. Replaces modal DetailViews for **Resminamalar**, **Document copies**, and **Progress ministry letters**, and hosts **file preview** (FileData / ministry letter links).

**Agent skill:** [`.cursor/skills/visa2026-preview-slot/SKILL.md`](../.cursor/skills/visa2026-preview-slot/SKILL.md)

## Why one slot

| Before | After |
|--------|--------|
| Resminamalar modal + separate file drawer | One resizable column beside main app |
| Document copies modal | Same shell, shared catalog card UX |
| Ministry letter filename → modal or split pane | `ProgressLetters` occupant in same slot (workspace Progress tab: viewer only) |

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
| `PersonDocumentCopies` | `PersonDocumentCopiesSlotPanel` | `OpenPersonDocumentCopiesAsync` | [`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md) |
| `HeaderDocumentCopies` | `HeaderDocumentCopiesSlotPanel` | `OpenHeaderDocumentCopiesAsync` | Invitation / WP / Rejection / BorderZone document copies. Case workspace issued-row **Preview** opens this occupant with `OpenPreviewOnly`. |
| `ProgressLetters` | `ProgressLettersSlotPanel` | `OpenProgressLettersAsync` | Application progress ministry letters. Case workspace Progress filename uses `OpenPreviewOnly` (viewer only). ListView toolbar / grid link still opens the slot catalog. |
| `File` | `VisaFilePreviewDrawer` | `OpenFileAsync` / JS bridge | File preview sources registry (`progress-letter`, `user-report-template`, `application-profile-template`) |
| `IssueIssuedHeader` | `IssueIssuedHeaderSlotPanel` | `OpenIssueIssuedHeaderAsync` | Case workspace **New invitation / work permit / rejection / border zone** compose ([prototypes](prototypes/issue-issued-header-slot-README.md)). Officers upload the letter copy (`InvitationDocument` / matching header `Documents`) from the compose panel. Issued visa still modal. |

**Occupant keys:** `VisaPreviewSlotOccupantKeys` (e.g. `resminamalar:app:{id}`, `document-copies:items:{ids}`, `progress-letters:app:{id}` or `…|preview:{progressId}` when `OpenPreviewOnly`, `file:{source}:{id}`).

## File preview sources (`IFilePreviewSource`)

Reuse the **File** occupant — do not add a new `VisaPreviewSlotMode` for “look at a Word/Excel master”. Convert office bytes to PDF with `ApplicationWordReportOfficePreviewPdfConverter` so the drawer iframe can render.

| `SourceType` | `objectId` | Used from |
|--------------|------------|-----------|
| `progress-letter` | `ApplicationProfileInstanceProgress.ID` | Workspace Progress letter |
| `user-report-template` | `UserReportTemplate.ID` | Application Profile wizard Shared catalog Preview |
| `application-profile-template` | `ApplicationProfileTemplate.ID` | Wizard Profile-specific Preview (wizard ObjectSpace first so unsaved uploads work) |

Preview of a template master shows **placeholders**, not a merged Resminamalar package. That merge still requires a live `ApplicationProfileInstance`.

## Catalog vs preview (exclusive mode)

Each feature panel uses **`--preview` CSS modifier** when a document is open:

1. **Catalog mode** — elevated card list (top-aligned, full slot width, flexible height, internal scroll on long lists).
2. **Preview mode** — catalog hidden; **viewer uses full slot width** (no catalog `max-width`).

Feature components set `UseInlinePreview="true"` and raise `OnInlinePreviewRequested` → panel sets `_previewActive` → `*InlinePreview` child.

Case workspace **Progress** filename opens `ProgressLetters` with `OpenPreviewOnly` (no catalog in the slot). Same pattern as Resminamalar / Document copies from their workspace tabs.

**Do not** reuse catalog card constraints on `.report-package-inline-preview` in `--preview` mode.

## Shared catalog card UX (2026)

CSS lives under `.resminamalar-slot-panel` (also used by `document-copies-slot-panel`, `progress-letters-slot-panel`):

- CSS variables: `--resminamalar-slot-inset-x`, `--resminamalar-slot-content-max`, `--resminamalar-slot-surface`, etc.
- Inline catalog classes: `.app-report-package--inline-slot` (Resminamalar), `.app-item-doc-copies--inline-slot` (document copies).
- Footer band: Download package / Refresh / gear — sticky inside card, not slot bottom float.

## Document-copies catalog chrome contract

All **document-copies** occupants (Person, ApplicationItem, Header / Invitation–WP–Rejection–BorderZone) share one **sectioned catalog** look — the Person / dossier-adjacent language — not bordered flat `__group` cards.

| Layer | Rule |
|-------|------|
| **Shell** | Still `resminamalar-slot-panel` + `.app-item-doc-copies--inline-slot` |
| **Catalog chrome** | `.doc-copies-catalog` in [`document-copies-catalog.css`](../Visa2026.Blazor.Server/wwwroot/css/document-copies-catalog.css): **Prototype A** vertical nav cards (colored icon circle + title + summary + Open/Close) → expand exclusive section → table-like rows (Record / Files / Status / Preview), nested indent, status pills (`person-dossier__pill`), gear file rows |
| **Data** | Resolvers stay feature-owned (Person ≠ ApplicationItem ≠ Header). Do not merge indexing rules. |
| **AppItem-only** | Package options, gap confirm, Download package footer stay below the shared catalog |
| **Not the dossier page** | Do not reuse `person-dossier__table` domain columns; dossier identity/tiles stay on the main DetailView ([`PERSON_DOSSIER.md`](PERSON_DOSSIER.md)) |

**Visual source of truth:** Person document copies sectioned catalog ([`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md)). **All** copies UIs (Person, ApplicationItem, Header) use Prototype A nav cards (`.doc-copies-catalog__section-head--nav`) + shared icons (`DocumentCopiesCatalogNavIcons`).

## Templates brand mark

Officer caption for the report package occupant is **Templates** (not Document Copies’ paperclip). Assets: `Templates.svg` (XAF ImageName), `templates-mark.svg` / `templates-mark-mask.svg`, `templates-brand.css`, `TemplatesBrandMark`. Use for Application / ApplicationItem toolbar actions and slot title (`.templates-brand-title`). Do not reuse `BO_FileAttachment` or DocumentCopies mark.

## Resminamalar catalog chrome contract

**Templates** (code/skills still say Resminamalar in places) shares the **same visual language** (elevated cards, colored icon circle, uppercase title, muted summary, action links) but **not** Document Copies section expand / Open.

| Layer | Rule |
|-------|------|
| **Shell** | Still `resminamalar-slot-panel` + `.app-report-package--inline-slot` |
| **Catalog chrome** | `.resminamalar-catalog` in [`resminamalar-catalog.css`](../Visa2026.Blazor.Server/wwwroot/css/resminamalar-catalog.css): **flat selectable cards** (checkbox + Word/Excel icon + title + format summary + READY/CHECK + Preview) |
| **Interaction** | Multi-select checkboxes, Select all / Clear, Download package, Sync, gear / Edit template — unchanged |
| **Icons** | `ResminamalarCatalogFormatIcons` (Word blue / Excel green); do not reuse DocumentCopies brand mark |
| **Not Document Copies** | No exclusive Open/expand sections; each template row is already a ZIP leaf |

**Visual source of truth for Resminamalar cards:** Document Copies Prototype A **surface** ([`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md) / `document-copies-catalog.css` tokens), adapted for selection. Domain behaviour: [`APPLICATION_REPORT_PACKAGE.md`](APPLICATION_REPORT_PACKAGE.md).

## File map (shell only)

| Area | Path |
|------|------|
| Service + state | `Visa2026.Module/Services/PreviewSlot/` (`IVisaPreviewSlotService`, requests, `VisaPreviewSlotOccupantKeys`) |
| Host impl | `Visa2026.Blazor.Server/Services/VisaPreviewSlotService.cs` |
| Blazor host | `Visa2026.Blazor.Server/Components/VisaPreviewSlotHost.razor` |
| Slot panels | `ResminamalarSlotPanel.razor`, `DocumentCopiesSlotPanel.razor`, `ProgressLettersSlotPanel.razor`, `IssueIssuedHeaderSlotPanel.razor` |
| Inline previews | `ReportPackageInlinePreview.razor`, `DocumentCopiesInlinePreview.razor`, `ProgressLettersInlinePreview.razor` |
| Close policy | `Visa2026.Blazor.Server/Controllers/VisaPreviewSlotCloseController.cs` |
| File preview sources | `Visa2026.Blazor.Server/Services/*FilePreviewSource.cs`, `OfficeFilePreviewResultFactory.cs` |
| Shell markup | `Visa2026.Blazor.Server/Pages/_Host.cshtml` (`#visa-preview-slot`, `visaPreviewDrawer.*` JS) |
| Shell CSS | `Visa2026.Blazor.Server/wwwroot/css/site.css` (`.visa-preview-slot*`, `.resminamalar-slot-panel*`) |
| Resminamalar catalog CSS | `Visa2026.Blazor.Server/wwwroot/css/resminamalar-catalog.css` |
| Document-copies catalog CSS | `Visa2026.Blazor.Server/wwwroot/css/document-copies-catalog.css` |
| Issue issued-header compose CSS | `Visa2026.Blazor.Server/wwwroot/css/issue-issued-header-slot.css` |
| Wiring | `Startup.cs` — register `IVisaPreviewSlotService` |

Feature-specific catalog logic stays in feature components and Module services — see domain docs above.

## Adding a new occupant (checklist)

0. If the need is “show this stored Word/Excel/PDF”, register an `IFilePreviewSource` and call `OpenFileAsync` — do not add a mode.
1. Add `VisaPreviewSlotMode` value + request DTO + `Open*Async` on `IVisaPreviewSlotService`.
2. Add `VisaPreviewSlotOccupantKeys.For*` stable key; bump `Version` on open.
3. Create `*SlotPanel.razor` (reuse `resminamalar-slot-panel` classes for catalog card).
4. Branch in `VisaPreviewSlotHost.razor` with `@key="_state.Version"`.
5. Controller calls `Open*Async(..., VisaPreviewSlotViewHelper.ResolveOwnerViewId(View))`.
6. CSS: catalog card under `.resminamalar-slot-panel`; preview full width under `--preview`.
7. Update this doc + [visa2026-preview-slot/reference.md](../.cursor/skills/visa2026-preview-slot/reference.md).
