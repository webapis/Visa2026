# People & Links — Nusga preview + Edit files + missing-scan badge

Rename the people-table **Upload** link to localized **Nusga** (EN Copies / TK Nusga / TR Nüsha / RU Копии). Click opens document-copy **preview** for that linked BO. Preview header has an **Edit** (pencil) control that switches the **same** preview-slot occupant into the file editor (list / remove / add). Save returns to refreshed preview. Empty scans use **File is not uploaded yet** with a path into Edit.

When the linked BO exists but has **no scan files**, show a warning badge **under Nusga** in the same cell (small red/amber icon + localized **No file**). Badge click behaves like Nusga (empty preview → Edit).

**Status:** implemented.

## Locked decisions

| Decision | Choice |
|----------|--------|
| Link label | Localized: EN **Copies**, TK **Nusga**, TR **Nüsha**, RU **Копии** |
| Nusga click | Open preview of existing scans for that linked BO |
| No files yet | Empty preview **File is not uploaded yet** + path to edit/upload |
| Missing-scan badge | Only when linked BO exists and has **no** scan files |
| Badge look | Small red/amber icon + short localized text (**No file**) |
| Badge placement | **Under Nusga** in the same cell |
| Badge click | Same as Nusga (empty preview → Edit) |
| Edit control | Pencil on preview header → **same slot** switches to upload/edit panel |
| After Save | Return to document preview with refreshed pages |
| Scope | People & Links table only (same columns as former Upload) |

## Screen

| File | Notes |
|------|--------|
| [case-workspace-people-nusga-preview-edit-prototype.jpg](./case-workspace-people-nusga-preview-edit-prototype.jpg) | Nusga + No file badge under it; preview + Edit; same-slot editor |

## Out of scope

- Warning when the linked BO itself is missing
- Left-nav Document copies
- Stacked edit tab under preview
- Creating a missing owner BO from this flow