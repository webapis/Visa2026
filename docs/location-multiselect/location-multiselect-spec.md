# Location Multi-Select (Border Zone and Work Permit Location)

Feature specification for the **Application** step ("Choose Application Profile", last step before create).

Status: prototype ready, pending answers to the open questions in section 14.
Reference prototype: `location-multiselect.html` (open in a browser; it is the source of truth for behavior and feel).

This feature reuses the **Address Autocomplete** search and dropdown (`address-autocomplete-spec.md`): same dropdown, same search strategy. It differs in two ways:

1. It holds **several values shown as chips** (like the Material UI Autocomplete "Multiple values" example, https://mui.com/material-ui/react-autocomplete/).
2. **The input never creates or edits data.** Adding and editing locations happens only in a **dialog**, opened from a **+ button** or from a **chip**. (Address 1 and Address 2 are different: there, typing a new value is the way to add it.)

Reuse the Address autocomplete code (fold, tokenize, rank, highlight) if it already exists.

---

## 1. Summary

The fields **Requested border zone location** and **Requested work permit location** are currently filled through a checkbox picker dialog opened by a "..." button.

Replace each field with:

- a **search box with chips**: the user types, picks matching saved locations from a dropdown, and each pick becomes a chip;
- a **+ button** next to the box: opens an "Add location" dialog to create a location that does not exist yet;
- an **edit action on every chip** (click the chip, which shows a small pen icon): opens an "Edit location" dialog to rename it.

The text box is only for searching and selecting. Typing a name that does not exist adds nothing; the status line tells the user to use the + button.

## 2. Problem

- Every change needs the "..." button, a modal, scrolling a long checkbox list (25+ rows), then OK.
- The list cannot be searched.
- The field shows only a comma-joined, truncated string, so users cannot see or fix the selection without reopening the dialog.
- A location that is not in the list cannot be entered, and a misspelled one cannot be corrected.
- Mixing "search" and "create" in one input is error-prone: a typo silently becomes a new value. Creating and editing therefore get their own explicit step.

## 3. Goals

1. Select locations by typing, with the same diacritic-insensitive search as Address autocomplete.
2. Multiple values, shown as removable chips.
3. Add a new location only through the + button and its dialog.
4. Edit a location only after it is selected (a chip) and only through the dialog.
5. No picker modal, no "..." button.
6. Keyboard and screen reader friendly.
7. Same behavior for both fields; each field has its own saved list.

## 4. Non-goals

- Deleting or merging saved locations (see open question 6).
- Changing other Application fields or the Address fields.
- Geocoding or validating that a location really exists.
- Server-side search (lists are small; see section 8).

---

## 5. User experience

### 5.1 Layout

Each field row is: **[ search box with chips ] [ + ]**.

- The search box fills the row width minus the + button. Its minimum height equals the other single-line inputs (34px in the prototype) and it **grows** when chips wrap; it never scrolls internally or clips chips.
- Inside the box, left to right: chips, then the text input (fills the remaining space, minimum about 110px). A small **clear all** button sits at the top-right corner inside the box, only when something is selected.
- The **+ button** is a 34px square icon button outside the box, vertically aligned to the box's first line. Tooltip and accessible name: "Add new border zone location" / "Add new work permit location".
- The suggestion list opens **below the box** as an overlay (never pushes the form down), as wide as the box, styled exactly like the Address autocomplete list.
- A status line sits below the row control.
- Remove the old "..." button and the picker dialog completely.

### 5.2 Search box behavior

| Step | What the user does | What happens |
|---|---|---|
| 1 | Focuses the box | No list. Status: "Search saved locations. Use the + button to add a new one." |
| 2 | Types text | List opens with up to **6** matching saved locations that are not already selected, matched text highlighted, "Used N×" on the right (or "New" if never used). |
| 3 | Clicks a suggestion, or Arrow keys then Enter | Location is added as a chip, text is cleared, list closes. |
| 4 | Types the exact name of a saved location and presses Enter | That saved location is added (case and diacritics ignored). |
| 5 | Types text that matches nothing | The list disappears. Status (info): "No saved location matches “text”. Use the + button to add it as a new location." **Nothing is created.** |
| 6 | Presses Enter with nothing highlighted and no exact match | Nothing happens (the key is swallowed so the form is not submitted). |
| 7 | Leaves the box (Tab, click elsewhere) with unpicked text | The typed text is **discarded**. It is never turned into a chip. |
| 8 | Clicks the x on a chip | Chip removed; the location returns to the suggestions. Focus stays in the box. |
| 9 | Presses Backspace in an empty text input | The last chip is removed. |
| 10 | Clicks clear all | All chips removed. |

Rules:

- The list opens only while the user types: never on an empty box and not just from tabbing in.
- Escape closes the list and keeps the typed text.
- **Enter picks a suggestion only if one is highlighted** with the arrow keys or mouse, or if the typed text exactly equals a saved location. The first suggestion is not auto-highlighted.
- Already selected locations are hidden from suggestions; a location can be selected only once.
- Chips are in the order they were added.
- Neither field is required; zero chips is valid.

### 5.3 Chips

- Each chip has two parts: the **edit part** (label plus a small pen icon; click opens the Edit dialog) and a **remove part** (x).
- Pill shape, accent-tinted background. The label shows an underline on hover to signal it is clickable. The pen icon is always visible (not hover-only) so touch users can see the chip is editable.
- Long labels (for example `BeýikSaparmyratTürkmenbaşyAd etraby`) truncate with an ellipsis, never exceed the box width, and show the full label in a tooltip.
- **Collapsed state (`limitTags`):** when the box is not focused and more than **3** locations are selected, show the first 3 chips plus a `+N` chip. Focusing the box expands to all chips.
- Clicking a chip must not first expand/re-render the chips in a way that swallows the click (implementation note: prevent the focus change on mousedown of chip buttons).
- The edit part is reachable by keyboard (Shift+Tab from the text input moves back through chips); the remove part is not in the tab order (Backspace and the mouse cover it).

### 5.4 Add dialog (+ button)

Opened by the + button. Modal dialog, centered.

- Title: "Add border zone location" / "Add work permit location".
- One text input "Location name", max 100 characters, **prefilled with whatever the user had typed in the search box** (if anything) and fully selected, so a failed search turns into an add in one click.
- Hint: "It is saved to the list right away and added to this field."
- Buttons: **Add location** (primary), **Cancel**, and a close (x) icon.
- On submit:
  1. Trim and collapse whitespace.
  2. Validate (section 7.4). On error, keep the dialog open, show the message under the input, mark the input invalid, keep focus in the input.
  3. On success: create the location in this field's saved list, **select it** (new chip), clear the search box, close the dialog.
- Cancel, the x button, Escape, and a click on the backdrop close the dialog and change nothing. The search box keeps the text the user had typed.

### 5.5 Edit dialog (chip click)

Opened by clicking a chip's edit part. Same dialog as Add, with differences:

- Title: "Edit border zone location" / "Edit work permit location".
- Input is prefilled with the current name and selected.
- Hint: "Used N×. Renaming changes this location wherever it is selected." (see open question 1 for what "wherever" means).
- Primary button: **Save changes**.
- On success: the location is renamed in the saved list; its chip and every place showing it update; it stays selected; dialog closes.
- Only locations that are currently chips can be edited. There is no other edit entry point (not from the dropdown, not from the saved list).

### 5.6 Dialog behavior (both)

- Native modal behavior: focus moves into the dialog (to the input) on open, is trapped inside, and **returns to the search box** of the field that opened it on close.
- Enter in the input submits. Escape cancels.
- Validation messages are announced (`role="alert"`) and linked with `aria-describedby`; the input gets `aria-invalid="true"` while an error shows. Typing clears the error.
- Only one dialog exists at a time (a single shared dialog component serves both fields and both modes).
- Dialog title is the dialog's accessible name (`aria-labelledby`).

### 5.7 Status line

| Situation | Text | Style |
|---|---|---|
| Empty text, nothing selected | Search saved locations. Use the + button to add a new one. | neutral |
| Empty text, N selected | "N selected. Click a chip to edit it." | success |
| Typed text equals a selected location | Already selected. | neutral |
| Typed text equals a saved location | Saved location. Press Enter to add it. | success |
| Typed text has matches | N saved locations match. Pick one from the list. | neutral |
| Typed text has no match | No saved location matches “text”. Use the + button to add it as a new location. | info |

---

## 6. Search behavior

Identical to Address autocomplete, section 6.

1. **Fold:** per-character map `ç→c ş→s ž→z ý→y ň→n ä→a ö→o ü→u ı→i ğ→g İ→i`, then lowercase. One-to-one so string length does not change (highlighting relies on it). Typing `anew` finds `Änew şäheri`.
2. **Tokens:** split the query on whitespace. A saved location matches when **every token appears anywhere** in its folded label.
3. **Ranking:** labels whose folded text **starts with the first token** first, then higher use count, then more recently used.
4. **Limit:** show at most **6**; keep the total count for the status line.
5. **Exact match:** folded, whitespace-normalized equality with a saved location. When the typed text is an exact match the list closes and the status says "Saved location".
6. **Highlight:** merge token match ranges in the folded text and wrap the same ranges of the original label. Build DOM from text nodes, never raw HTML.

## 7. Data model

### 7.1 Saved locations

One list per field. Suggested table (adapt to the existing schema; see open question 1 about the existing lookup tables):

```
saved_location
  id              uuid / bigint
  field           enum('border_zone', 'work_permit_location')
  label           string        -- whitespace normalized
  label_folded    string        -- normalized search key (section 6.1), indexed
  use_count       int           -- 0 when created, +1 each time an application is created with it
  last_used_at    timestamp
  source          enum('lookup', 'user')   -- seeded from the old lists vs added by a user
  created_at, created_by, updated_at, updated_by
  scope_id        tenant / organization id (see open question 2)
  UNIQUE (scope_id, field, label_folded)
```

Seed this list from the lookup data that feeds the old dialogs so existing locations keep working.

### 7.2 Field value

Each field holds an **array of saved location ids**. The label shown in a chip is always read from the saved list by id, which is how a rename updates every chip automatically.

Prototype output for reference:

```
[{"id":"bz01","label":"Daşoguz şäher"},{"id":"bz02","label":"Etrek etrap"}]
```

How the value is stored today is not confirmed (open question 3); it looks like a comma-joined string of labels.

### 7.3 Migration of existing data

If the backend currently stores a comma-joined string:

- Either keep the string in storage and convert at the boundary (split on `,`, trim, match to saved locations by folded label, join back on save using the **current** labels), or migrate to an id array.
- A stored label that matches no saved location must **not be dropped silently**. Create it in the saved list on load (or on first save) so it appears as a normal chip and can be edited.
- Keep a record of how renames affect already saved applications (open question 1).

### 7.4 Validation (client and server, same rules)

- Trim and collapse whitespace; empty is rejected: "Enter a location name."
- Maximum 100 characters: "Use at most 100 characters."
- Duplicate in the same field (by folded label, excluding the location being edited): "“X” already exists in this list. Close this dialog and search for it."
- Characters: allow letters (including Turkmen, Turkish, Russian), digits, spaces and common punctuation `. , - ( ) / '`. Reject control characters.

## 8. API and loading

- Load each field's saved list once when the Application step opens and filter on the client (tens of items). If a list ever grows past about 500 items, move search to the server with debounce (about 150 ms), request cancellation and the same ranking, returning at most 6 plus a total count.
- Endpoints (adapt to the existing style):
  - `GET  /api/saved-locations?field=border_zone` returns `[ { id, label, useCount, lastUsedAt } ]`
  - `POST /api/saved-locations` with `{ field, label }` returns the created location. Atomic upsert by `(scope_id, field, label_folded)`; if it already exists, return **409** with the existing location so the client shows the duplicate message.
  - `PATCH /api/saved-locations/{id}` with `{ label }` renames. Returns 409 on a duplicate, 403 if the user may not edit, 404 if it no longer exists.
- Add and Edit save **immediately** when the dialog's primary button is pressed (they do not wait for "Create application"). On success the client updates its list and chips; on failure the dialog stays open with the error.
- Creating the application: send the selected ids (or the legacy string, per 7.3). The server increases `use_count` and sets `last_used_at` for each selected location inside the same transaction, and validates that every id exists in that field's list.
- Concurrency: if two users rename or add at the same moment, the unique key decides; the loser gets 409. Optionally return `updatedAt` and reject stale renames with 409 "This location was changed by someone else. Reopen it to see the new name."

## 9. Client implementation notes

- Build one reusable `MultiSelectAutocomplete` for the box and chips, and one shared `LocationDialog` (modes `add` and `edit`) used by both fields.
- Props for the multi-select along the lines of: `field`, `value` (ids), `options` (saved list), `onChange`, `onAdd(text)`, `onRename(id, text)`, `limitTags` (default 3), `disabled`, `readOnly`.
- If the project uses Material UI, use `Autocomplete` with `multiple`, `filterSelectedOptions`, `limitTags={3}`, `openOnFocus={false}`, **no** `freeSolo` (the input must not create values), a custom `filterOptions` and `renderOption` using the shared fold/token/rank/highlight module, and a custom `renderValue`/`renderTags` that renders the chip with a pen-icon edit area and its own `onDelete`. Use MUI `Dialog` (or the project's modal) for the dialog, with `aria-labelledby`, focus return, and Enter-to-submit inside a `<form>`. Set `clearOnBlur` so unpicked text is discarded.
- Place the + `IconButton` beside the Autocomplete, outside its root.
- Keep search, rank and highlight helpers in one pure module shared with Address autocomplete.
- Delete the old picker dialog components, their state, the "..." buttons and any styles used only by them.

## 10. Accessibility

- Text input: `role="combobox"`, `aria-autocomplete="list"`, `aria-expanded`, `aria-controls`, `aria-activedescendant`; labelled by the row label.
- List: `role="listbox"` with `aria-multiselectable="true"`; rows `role="option"` with `aria-selected`. Focus stays in the text input while navigating.
- Chip edit buttons: accessible name "Edit {label}", focusable. Chip remove buttons: "Remove {label}".
- + button: accessible name "Add new {field name}".
- Dialog: modal with focus trap, `aria-labelledby` on the title, initial focus in the input, focus returns to the field's text input on close, Escape cancels.
- Live region announces: "Added X. N selected.", "Added new location X. N selected.", "Renamed X to Y.", "Removed X. N selected.", "All locations cleared."
- Status line: `role="status"`, `aria-live="polite"`, linked with `aria-describedby`.
- Do not rely on color alone for error, info or success states; messages are text.
- Respect `prefers-reduced-motion`.

## 11. Edge cases

- Whitespace-only text behaves as empty everywhere.
- Renaming a location to the same text (or only changing case/diacritics of itself) is allowed.
- Renaming to a name that another location already has: error 7.4.
- Opening the Add dialog with typed text that already exists as a saved location: validation shows the duplicate message; the user closes and picks it.
- Opening Add while another dialog is open is impossible (modal).
- 20+ chips: the box grows and the layout below shifts down; collapsed `+N` applies when not focused.
- Field disabled: chips visible without x or pen, no + button, no list. Read-only: same.
- User lacks permission to add or edit (open question 4): hide the + button and pen icons, chips only selectable/removable.
- Saved list fails to load: the field shows "Locations unavailable", chips of the saved value stay visible and removable, + is disabled; Create is not blocked.
- The dropdown and dialog must not be clipped by scrolling containers (render in a portal if the form uses `overflow: hidden`).
- If the location being edited was deleted by someone else, the save returns 404: show "This location no longer exists." and remove its chip on close.

## 12. Acceptance criteria

- [ ] The "..." buttons and checkbox picker dialogs are gone for both fields.
- [ ] Typing shows up to 6 ranked suggestions with highlighted matches and "Used N×", styled like the Address 1/Address 2 dropdown; `anew` finds `Änew şäheri`.
- [ ] Picking a suggestion adds a chip, clears the text and closes the list.
- [ ] Typing something that matches nothing and pressing Enter, or leaving the box, creates nothing and discards the text; the status points to the + button.
- [ ] Each field has a + button outside the box; it opens the Add dialog prefilled with the typed text.
- [ ] Add dialog validates (empty, too long, duplicate), saves immediately, selects the new location as a chip, clears the search box, and returns focus to the box.
- [ ] Every chip has an always-visible pen icon; clicking the chip opens the Edit dialog prefilled with its name.
- [ ] Edit dialog renames the saved location; the chip and the saved list update; duplicates are rejected.
- [ ] No way exists to add or edit a location without a dialog.
- [ ] Cancel, x, Escape and backdrop click close a dialog without changes and keep the search text.
- [ ] Selected locations are hidden from suggestions and return when their chip is removed.
- [ ] Chip x, Backspace in an empty input and clear all remove chips correctly.
- [ ] Not focused with more than 3 chips shows 3 plus `+N`; focusing shows all; clicking a chip works in the collapsed state.
- [ ] Long labels truncate inside chips with a tooltip; nothing overflows.
- [ ] Box height matches neighboring inputs when empty and grows with chips.
- [ ] Existing saved values load as chips and are never dropped.
- [ ] Creating the application increases the use count of every selected location, atomically.
- [ ] Fully keyboard operable (including chip edit and both dialogs) and announced by a screen reader; focus returns correctly after dialogs.
- [ ] No label is ever rendered as raw HTML.
- [ ] Old dialog code and its styles are removed.

## 13. Suggested tests

- Unit: fold, tokenize, rank, highlight ranges, validation rules (empty, length, duplicate excluding self, case/diacritic-only rename).
- Component: list opens only while typing; Enter only picks a highlighted or exact-match item; unmatched text creates nothing and is discarded on blur; + opens Add prefilled; Add selects the new chip; chip click opens Edit; rename updates chip and list; duplicate errors; Cancel/Escape/backdrop leave data unchanged; focus returns to the box; `+N` collapse and click on a collapsed chip; Backspace and clear all.
- API: create returns 409 on duplicate, rename returns 409/403/404, use counts increase inside the create-application transaction, rollback when create fails, concurrent adds of the same name.
- Visual: + button aligned with the box, dialog centered and readable in all themes, box height equals neighboring inputs.

## 14. Open questions

Defaults used in the prototype are in brackets.

1. **What does "edit" change?** [Renames the saved location itself, so every chip and every application that references it by id shows the new name.] If applications store the label as text, old applications keep the old text. If other systems (reports, integrations, government forms) rely on these lookup rows or codes, renaming and adding may break them. Should edit instead create a corrected copy for this application only? Also: the old dialogs seem to be fed by lookup tables (Lookup/General/Geography); is it acceptable for users to add or change entries there? [Separate saved list per field, `source = user`, seeded from the lookups.]
2. **Scope:** are added and renamed locations shared by the whole organization or private to each user? [Whole organization.]
3. **Storage format:** how is the value stored today (comma-joined labels, ids, other) and can the API accept an array of ids? Do existing cases need migrating? [Convert at the boundary if the API stays string-based.]
4. **Permissions:** can every user add and rename locations, or only some roles? [Everyone.]
5. **Save timing:** save immediately on the dialog's button (prototype) or only when "Create application" is pressed? Immediate is simpler and safer for shared lists, but a cancelled application still leaves the new or renamed location behind. [Immediate.]
6. **Delete:** should the Edit dialog also offer "Delete location" (only when unused)? [No.]
7. **Pen button position:** the prototype puts the pen icon inside each chip, next to its label, and the + button outside the box. Should instead one pen button sit outside the box next to the + button, acting on a chosen chip? [Pen on each chip.]
8. **Limits:** minimum or maximum number of selections, or is either field required? [No limits, optional.]
9. **Same list or different lists:** do both fields share one set of locations? The two old dialogs showed different lists. [Separate list per field.]
10. **Near-duplicates:** the data already has `Serhetabat etr` and `Serhetabat etrap`. Should the Add dialog warn about similar names (not just exact matches), and is a data cleanup planned? [Exact (folded) duplicates only.]
11. **Gear icon:** what does the gear icon in the old dialog do, and does anything need to replace it?
12. **Stack:** is Material UI already in the project?
13. **Cyrillic:** do users also type Russian/Cyrillic location names? If yes, search needs transliteration or alternate keys.
