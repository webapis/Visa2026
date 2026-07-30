# Learnings (append-only): Global preview slot

Purpose: **shell, layout, occupants, catalog card UX, JS/CSS** — not Resminamalar merge or document-copy scan rules.

**Read before every preview-slot task:** skim **## Entries** (newest first).  
**Maturity:** [MATURITY.md](./MATURITY.md).

**After a verified fix:** append one entry. **Do not** edit or delete prior entries.

```markdown
### YYYY-MM-DD — <short title> (<Resminamalar | DocumentCopies | shell | CSS>)

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: preview-slot | resminamalar | document-copies | —
```

---

## Entries

### 2026-07-30 - New surface must live in main area, not as an occupant; OwnerViewId from a PropertyEditor

- **Symptom (design-time)**: A person dossier page that also opens document copies would fight the slot: `Open*` is last-wins with **one occupant at a time**, so hosting the dossier as an occupant means opening copies evicts the dossier the officer is reading.
- **Fix**: Dossier renders in the **main content area** (`#visa-app-shell` grid `1fr` + slot width), so data and scans are visible side by side. Slot stays reserved for files.
- **Second trap**: `VisaPreviewSlotCloseController` closes the slot when the **owning View** deactivates. Navigating search -> dossier would therefore close a slot opened from the previous view.
- **Fix**: Pass the dossier view id explicitly. A `BlazorPropertyEditorBase` has no `View`, so `VisaPreviewSlotViewHelper.ResolveOwnerViewId(view)` is unavailable - added the constant `PersonDossierViewIds.DetailView` (`"PersonDossierHost_DetailView"`) and passed it as `ownerViewId`.
- **Prevent**: When opening the slot from a property editor / component rather than a `ViewController`, use a view-id constant; do not pass `null` (that makes the slot owner-less and closes unpredictably).
- **Not verified in a running app session**: build only.
- **Cross-skill**: person-document-copies

### 2026-06-06 — Catalog card UX polish (Resminamalar + Document copies)

- **Symptom**: Inline catalog cramped, duplicate report names, centered card floating in empty space; preview viewer incorrectly narrowed when catalog CSS applied globally.
- **Try**: Open Resminamalar slot → long template names → Preview Excel → Close preview.
- **Test**: Single display name per row; top-aligned full-width card; list grows with slot height; preview uses full slot width after hard-refresh.
- **Root cause**: Separate `slot-entry` markup; `OutputFileName` shown under `DisplayName`; fixed `max-height` on list; catalog `max-width` leaked into `--preview` mode.
- **Fix**: Unified `group-head` rows; hide `OutputFileName` when `UseInlinePreview`; shared `.resminamalar-slot-panel` card CSS for `app-report-package--inline-slot` and `app-item-doc-copies--inline-slot`; explicit preview full-width rules under `--preview`.
- **Prevent**: Scope catalog layout to catalog selectors only; feature skills own content, **preview-slot** skill owns shell CSS split.
- **Cross-skill**: resminamalar, document-copies

### 2026-06-06 — Occupant switch left stale inline PDF (historical)

- **Symptom**: Application Resminamalar open; ApplicationItem Resminamalar left previous PDF visible.
- **Root cause**: Panel reused without remount; `_previewActive` stuck; close on any view deactivate.
- **Fix**: `OccupantKey`, `Version`, `@key`, owner-aware close — see [resminamalar/learnings.md](../visa2026-resminamalar/learnings.md) same date.
- **Prevent**: Any new occupant must bump `Version` and reset local preview flags.
- **Cross-skill**: resminamalar
