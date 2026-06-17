# Preview slot — user prompts

Copy-paste in Cursor chat (prefix `@visa2026-preview-slot` if your UI supports skill mention).

## Layout / UX

- Preview slot catalog looks cramped — align Resminamalar and Document copies with the shared card style.
- Document preview in the slot is too narrow; catalog CSS must not affect preview mode.
- Resminamalar catalog should use full slot height before scrolling the list.

## Shell / bugs

- Resminamalar slot does not update when I open item-scoped Resminamalar from Application detail.
- Preview slot resize handle does not drag / width does not persist.
- Text in the preview slot is invisible in dark theme.

## Design / new feature

- Add a new occupant to `#visa-preview-slot` for [feature] — follow existing Resminamalar / Document copies pattern.
- Design checklist: inline catalog + exclusive preview for [feature].

## Triage

- Is this a preview-slot shell issue or Resminamalar / document-copies domain logic?
