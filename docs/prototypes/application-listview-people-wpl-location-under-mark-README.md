# People column — WPL clickable + granted location under mark

**File:** `application-listview-people-wpl-location-under-mark-prototype.jpg`  
**Status:** implemented (2026-11-05)  
**Related:** goşmaça barjak ýeri / `WorkPermitLocationGrant`; ListView People marks; `#visa-preview-slot` document copies

## Story

On **Applications via ministry** ListView, an issued location permission shows as a green **WPL** circle next to the person (same hit target as Inv / WP / Visa).

1. **Click** the WPL circle → opens **document copy** preview in the global preview slot (grant letter / scan), same pattern as other issued marks.
2. **Under the mark** (not under the person name): granted work-permit location text, e.g. `Mary şäheri` (from `WorkPermitLocationGrantItem.GrantedLocations`).

Progress chip **WPL 1/1** stays as today.

## Locked UX notes for implementation

| Element | Behavior |
|--------|----------|
| Circle letters | `WPL` (localized mark key) |
| Tone when issued | green / done; missing stays gray |
| Click | `CanPreview` + grant header id; own document-copies family (not WorkPermit parent id) |
| Caption under mark | comma-separated granted locations; hide line when empty |
| Hover title | Location permission + in force / not issued (existing mark title pattern) |

## Out of scope for this prototype

- Changing Application progress chip shape
- Editing granted locations from the ListView