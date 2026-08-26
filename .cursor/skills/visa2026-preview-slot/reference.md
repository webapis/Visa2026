# Preview slot — reference

Canonical narrative: [`docs/PREVIEW_SLOT.md`](../../../docs/PREVIEW_SLOT.md).

## Open flow

```text
XAF Controller
  → IVisaPreviewSlotService.Open*Async(request, ownerViewId)
  → VisaPreviewSlotService (State, Version++, OccupantKey)
  → StateChanged
  → VisaPreviewSlotHost re-render (@key Version on panel)
  → *SlotPanel (catalog) or *InlinePreview (exclusive preview)
```

## Occupant keys (VisaPreviewSlotOccupantKeys)

Also: `issue-issued-header:{catalogKey}:{appId:N}` via `ForIssueIssuedHeader` (compose New invitation / work permit / rejection / border zone).

| Pattern | When |
|---------|------|
| `resminamalar:app:{applicationId}` | Application-scope Resminamalar |
| `resminamalar:items:{appId}:{sortedItemIds}` | ApplicationItem ListView scope |
| `document-copies:items:{sortedItemIds}` | Document copies |
| `progress-letters:app:{appId}` | Ministry letter catalog |
| `progress-letters:app:{appId}` plus `preview:{progressId}` | Workspace Progress filename (`OpenPreviewOnly`) |
| `file:{sourceType}:{objectId}` | Generic file preview (`progress-letter`, `user-report-template`, `application-profile-template`) |
| `issue-issued-header:{catalogKey}:{appId}` | New invitation / WP / rejection / border zone compose (ForIssueIssuedHeader) |

Register new Word/Excel “look” previews as `IFilePreviewSource` + `OpenFileAsync`. Do not add a Resminamalar occupant from the Application Profile wizard (no live application to merge).

## Panel state machine

| Panel class | Catalog | Preview |
|-------------|---------|---------|
| default | `resminamalar-slot-panel__catalog` visible | hidden |
| `resminamalar-slot-panel--preview` | hidden | `*InlinePreview` full width |

Reset `_previewActive` when `OccupantKey` changes (`OnParametersSet` in slot panels).

## Feature components (inline)

| Feature | Component | Inline class |
|---------|-----------|--------------|
| Resminamalar | `ApplicationReportPackageComponent` | `app-report-package--inline-slot` |
| Document copies | `ApplicationItemDocumentCopiesComponent` | `app-item-doc-copies--inline-slot` |
| Progress letters | `ProgressLettersCatalogComponent` | (panel-specific; share slot CSS) |

Both Resminamalar and document copies set `UseInlinePreview="true"` from their slot panels.

## CSS variables (catalog card)

Defined on `.resminamalar-slot-panel`:

| Variable | Purpose |
|----------|---------|
| `--resminamalar-slot-inset-x` | Horizontal padding (header + catalog) |
| `--resminamalar-slot-content-max` | Catalog card max width (`100%` = use slot) |
| `--resminamalar-slot-border` | Card borders / dividers |
| `--resminamalar-slot-surface` | Card background |
| `--resminamalar-slot-surface-muted` | Toolbar / footer bands |
| `--resminamalar-slot-radius` | Card corner radius |
| `--resminamalar-slot-shadow` | Card elevation |

**Preview override** (required):

```css
.resminamalar-slot-panel--preview .report-package-inline-preview {
    max-width: none;
    margin-inline: 0;
    width: 100%;
}
```

## JS API (`visaPreviewDrawer` in `_Host.cshtml`)

| Function | Role |
|----------|------|
| `setSlotOpen(open)` | Grid column / nav collapse |
| `initSlotResize` / `applySlotWidth` | Drag resize |
| `syncSlotTheme()` | Copy theme vars into slot |
| `syncInlinePreviewHeight()` | PDF iframe height after resize |
| `registerHost` / `unregisterHost` | `VisaPreviewSlotHost` JS interop |

Hard-refresh required after JS/CSS changes (Ctrl+F5).

## Controllers (open slot)

| Controller | API |
|------------|-----|
| `WordReportsController` | `OpenResminamalarAsync` (Application) |
| `ApplicationItemWordReportsController` | `OpenResminamalarAsync` (items) |
| `ApplicationItemDocumentCopiesController` | `OpenDocumentCopiesAsync` |
| `ApplicationProgressMinistryLettersController` | `OpenProgressLettersAsync` |

Always pass `VisaPreviewSlotViewHelper.ResolveOwnerViewId(View)`.

## Adding a new occupant (files to touch)

1. `Visa2026.Module/Services/PreviewSlot/IVisaPreviewSlotService.cs` — mode, request, method
2. `VisaPreviewSlotOccupantKeys.cs` — key builder
3. `VisaPreviewSlotService.cs` — implement open/close
4. `VisaPreviewSlotHost.razor` — branch + `@key`
5. New `*SlotPanel.razor` + optional `*InlinePreview.razor`
6. XAF controller → `Open*Async`
7. `site.css` — reuse `.resminamalar-slot-panel` patterns
8. `docs/PREVIEW_SLOT.md` + this file
