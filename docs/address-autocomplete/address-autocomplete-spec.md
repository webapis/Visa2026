# Address Autocomplete (Address 1 and Address 2)

Feature specification for the **Application** step ("Choose Application Profile", last step before create).

Status: prototype ready, pending answers to the open questions in section 14.
Reference prototype: `address-combobox.html` (open in a browser; it is the source of truth for behavior and feel).

This document **replaces the earlier version** of this spec. The earlier version let users type a new address straight into the field. That is no longer the design: **the field only searches and selects; adding and editing happen in a dialog.**

Related feature: **Location Multi-Select** (`location-multiselect-spec.md`) uses the same search, the same dropdown and the same dialog, but holds several values as chips. Build one shared dialog component and one shared search module for both features.

---

## 1. Summary

Replace the **Address 1** dropdown with a searchable field, and give **Address 2** the same behavior. Address 1 and Address 2 are the same component: identical width, height and styling.

Each field consists of:

- a **search box**: the user types, a dropdown of matching saved addresses appears, and picking one **selects** it;
- a **selected value** state: once an address is selected, the box shows it as text with a pen icon (edit) and an x (clear);
- a **+ button** outside the box: opens an **Add address** dialog for an address that does not exist yet;
- clicking the **selected value** (it shows a pen icon) opens an **Edit address** dialog to correct it.

**The text box never creates or edits data.** Typing an address that does not exist adds nothing; the status line points to the + button.

## 2. Problem

- Address 1 is a required `<select>` ("— Select —"). Users must scroll or pick from a long list.
- If the address is not in the list there is no way to proceed.
- Address 1 looks different from Address 2 (shorter, different control).
- Letting the text box create values silently turns typos into permanent saved addresses. Creating and correcting get their own explicit step.

## 3. Goals

1. Search-as-you-type selection instead of a dropdown, with diacritic-insensitive search.
2. Address 1 and Address 2 have exactly the same size and behavior.
3. Add a new address only through the + button and its dialog.
4. Edit an address only after it is selected, only through the dialog.
5. Keyboard and screen reader friendly.
6. Address 1 stays required; Address 2 stays optional.

## 4. Non-goals

- Deleting or merging saved addresses (see open question 6).
- Geocoding or validating that an address really exists.
- Changing any other Application field.
- Server-side search (lists are small; see section 8).

---

## 5. User experience

### 5.1 Layout

Each field row is: **[ box ] [ + ]**.

- The box fills the row width minus the + button. **Minimum height 84px** (same as the existing Address 2 textarea) for both Address 1 and Address 2; it grows if a long selected address needs more lines. Same border, radius and padding for both.
- The **+ button** is a 34px square icon button outside the box, aligned to the top of the box. Tooltip "Add new"; accessible name "Add new Address 1" / "Add new Address 2".
- The suggestion list opens **below the box** as an overlay (never pushes the form down), as wide as the box, maximum height about 252px, then scrolls.
- A one-line status sits below the row control.
- The box has two visual states (5.2 and 5.3).

### 5.2 Search state (nothing selected)

The box is a multi-line text area (3 rows) with a placeholder ("Start typing a street, district or city" for Address 1, "Apartment, building, floor or landmark" for Address 2). It is used only to type a search.

| Step | What the user does | What happens |
|---|---|---|
| 1 | Focuses the box | No list. Status shows the empty hint. |
| 2 | Types text | List opens with up to **6** matching saved addresses, matched text highlighted, "Used N×" on the right (or "New" if never used). |
| 3 | Clicks a suggestion, or Arrow keys then Enter | The address becomes the **selected value** (5.3). List closes. Focus moves to the selected value's edit button. |
| 4 | Types the exact text of a saved address and presses Enter | That saved address is selected (case and diacritics ignored). |
| 5 | Types text that matches nothing | The list disappears. Status (info): "No saved address matches “text”. Use the + button to add it as a new address." **Nothing is created.** |
| 6 | Presses Enter with nothing highlighted and no exact match | Nothing happens. Enter never inserts a line break and never submits the form here. |
| 7 | Leaves the box (Tab, click elsewhere) with unpicked text | The typed text is **discarded**. |

Rules:

- The list opens only while the user types: never on an empty box and not just from tabbing in.
- Escape closes the list and keeps the typed text.
- **Enter picks a suggestion only if one is highlighted** (arrow keys or mouse) or the typed text exactly equals a saved address. The first suggestion is not auto-highlighted.
- Search text is limited to 500 characters.

### 5.3 Selected state

When an address is selected, the search box is replaced inside the same border by the **selected value**:

- the full address text, wrapping onto as many lines as needed (never truncated), with a pen icon after it;
- the whole text plus pen is one button: **click it to open the Edit dialog**. Hover underlines the text; the pen icon is always visible so touch users can see it is editable;
- a small round **x** button at the top-right of the box: clears the value and returns to the search state, with focus in the search box.

The box border turns **green** while a saved address is selected. When Address 1 is required and empty after a failed Create, the row turns red (existing error behavior).

The edit button has an accessible name like "Edit Address 1: {address text}".

### 5.4 Add dialog (+ button)

Modal dialog, centered.

- Title: "Add Address 1" / "Add Address 2".
- One multi-line input, **max 500 characters**, **prefilled with whatever the user had typed in the search box** (if anything) and fully selected, so a failed search turns into an add in one click.
- Hint: "It is saved to the list right away and selected in this field. Line breaks become spaces."
- Buttons: **Add address** (primary), **Cancel**, close (x).
- Enter submits; Shift+Enter inserts a line break (which is collapsed to a space on save).
- On submit: normalize whitespace, validate (7.4). On error keep the dialog open, show the message under the input, mark it invalid, keep focus in it. On success: create the address in that field's saved list, **select it** (replacing any current selection), clear the search text, close the dialog.
- Cancel, x, Escape and a click on the backdrop close the dialog and change nothing. Any text the user had typed in the search box is kept.

### 5.5 Edit dialog (click the selected value)

Same dialog as Add, with differences:

- Title: "Edit Address 1" / "Edit Address 2".
- Input prefilled with the current text and selected.
- Hint: "Used N×. Renaming changes this address wherever it is selected. Line breaks become spaces." (see open question 1 for what "wherever" means).
- Primary button: **Save changes**.
- On success: the saved address is renamed, the field and the saved list update, it stays selected, dialog closes.
- Only the currently selected address can be edited. There is no other edit entry point.

### 5.6 Dialog behavior (both)

- Native modal behavior: focus moves to the input on open, is trapped, and **returns to the field** that opened it on close (the edit button if an address is selected, otherwise the search box).
- Validation messages use `role="alert"` and are linked with `aria-describedby`; the input gets `aria-invalid="true"` while an error shows; typing clears the error.
- One shared dialog component serves both fields and both modes. Its accessible name is its title (`aria-labelledby`).

### 5.7 Status line

| Situation | Text | Style |
|---|---|---|
| Nothing selected, empty text | Address 1: "Start typing to search saved addresses. Use the + button to add a new one." Address 2: "Optional. Start typing to search saved entries. Use the + button to add a new one." | neutral |
| Typed text has matches | N saved addresses match. Pick one from the list. | neutral |
| Typed text equals a saved address | Saved address. Press Enter to select it. | success |
| Typed text has no match | No saved address matches “text”. Use the + button to add it as a new address. | info |
| An address is selected | Saved address. Click it to edit, or × to choose another. | success |

### 5.8 Validation (Create application)

- Address 1 is required. Empty (no selected address) blocks Create with the existing red row and banner: "Complete required Case summary fields before continuing. Missing: Project, Address 1." (list only what is missing). The banner and row state update live once the user has attempted Create.
- Address 2 is optional.

---

## 6. Search behavior

1. **Fold:** per-character map `ç→c ş→s ž→z ý→y ň→n ä→a ö→o ü→u ı→i ğ→g İ→i`, then lowercase. One-to-one so string length does not change (highlighting relies on it). Typing `asgabat` finds `Aşgabat`. Collapse whitespace and line breaks to single spaces when comparing or saving.
2. **Tokens:** split the query on whitespace. A saved address matches when **every token appears anywhere** in its folded text. The currently selected address is not offered again.
3. **Ranking:** addresses whose folded text **starts with the first token** first, then higher use count, then more recently used.
4. **Limit:** at most **6** results, plus the total match count for the status line.
5. **Exact match:** folded, whitespace-normalized equality with a saved address. When the typed text is an exact match the list closes and the status says "Saved address".
6. **Highlight:** merge token match ranges in the folded text and wrap the same ranges of the original text. Build DOM from text nodes, never raw HTML.

## 7. Data model

### 7.1 Saved addresses

One list per field (Address 1 and Address 2 are independent). Suggested table (adapt to the existing schema; see open question 1 about existing lookup data):

```
saved_address
  id              uuid / bigint
  field           enum('address1', 'address2')
  text            string        -- whitespace normalized
  text_folded     string        -- normalized search key (6.1), indexed
  use_count       int           -- 0 when created, +1 each time an application is created with it
  last_used_at    timestamp
  source          enum('lookup', 'user')   -- seeded from the old Address 1 list vs added by a user
  created_at, created_by, updated_at, updated_by
  scope_id        tenant / organization id (see open question 2)
  UNIQUE (scope_id, field, text_folded)
```

Seed Address 1's list from the lookup data that feeds the old dropdown so existing addresses keep working.

### 7.2 Field value

Each field holds **one saved address id or empty**. The text shown in the box is always read from the saved list by id, so a rename updates the field automatically.

### 7.3 Migration of existing data

- If the backend stores Address 1/Address 2 as plain text on existing applications, keep the text in storage and convert at the boundary: on load, match the stored text to a saved address by folded text; if none matches, **create it in the saved list** (do not drop it) so it appears as a normal selected value that can be edited.
- If it stores an id for Address 1 (from the old dropdown), keep using the id.
- Decide how renames affect already saved applications (open question 1).

### 7.4 Validation (client and server, same rules)

- Trim and collapse whitespace; empty is rejected: "Enter an address."
- Maximum 500 characters: "Use at most 500 characters."
- Duplicate in the same field by folded text (excluding the address being edited): "This address already exists in the list. Close this dialog and search for it."
- Reject control characters other than whitespace.

## 8. API and loading

- Load each field's saved list once when the Application step opens and filter on the client (tens to a few hundred items). If a list grows past about 500 items, move search to the server with debounce (about 150 ms), request cancellation and the same ranking, returning at most 6 plus a total count.
- Endpoints (adapt to the existing style):
  - `GET   /api/saved-addresses?field=address1` returns `[ { id, text, useCount, lastUsedAt } ]`
  - `POST  /api/saved-addresses` with `{ field, text }` returns the created address. Atomic upsert by `(scope_id, field, text_folded)`; if it already exists return **409** with the existing item so the client shows the duplicate message.
  - `PATCH /api/saved-addresses/{id}` with `{ text }` renames. 409 on duplicate, 403 if the user may not edit, 404 if it no longer exists.
- Add and Edit save **immediately** when the dialog's primary button is pressed (they do not wait for "Create application"). On failure the dialog stays open with the error.
- Creating the application: send the selected ids (or text, per 7.3). The server increases `use_count` and sets `last_used_at` for the selected addresses inside the same transaction and validates that each id exists in that field's list.
- Concurrency: the unique key decides simultaneous adds; the loser gets 409. Optionally send `updatedAt` with a rename and reject stale renames with 409 "This address was changed by someone else. Reopen it to see the new text."

## 9. Client implementation notes

- Build one reusable single-value `AddressAutocomplete` (search state + selected-value state) used by both fields, and one shared `SavedValueDialog` (modes `add` and `edit`) also used by the Location multi-select. Share the search/rank/highlight module too.
- Props along the lines of: `field`, `required`, `label`, `placeholder`, `emptyHint`, `value` (id), `options` (saved list), `onChange`, `onAdd(text)`, `onRename(id, text)`, `disabled`, `readOnly`.
- If the project uses Material UI: `Autocomplete` (single, **no** `freeSolo`, `openOnFocus={false}`, `clearOnBlur`, custom `filterOptions` and `renderOption` using the shared module). Render the selected-value state with a custom component instead of the default input (a read-only block with the pen and x buttons), keep the box at 84px minimum height for both fields, put the + `IconButton` beside the Autocomplete, and use MUI `Dialog` for the dialog (inside a `<form>`, `aria-labelledby`, focus return).
- Otherwise build it on the project's existing UI primitives to the same behavior.
- Remove the old Address 1 `<select>` and any code that only fed it. Address 2's old plain textarea is replaced too.

## 10. Accessibility

- Search box (search state): `role="combobox"`, `aria-autocomplete="list"`, `aria-expanded`, `aria-controls`, `aria-activedescendant`, `aria-required="true"` for Address 1, labelled by the row label, described by the status line.
- List: `role="listbox"`; rows `role="option"` with `aria-selected`. Focus stays in the box while navigating.
- Selected value: the edit button is a real button with accessible name "Edit Address 1: {text}"; the clear button is named "Clear Address 1". Focus moves to the edit button after a selection and to the search box after clearing.
- + button: accessible name "Add new Address 1".
- Dialog: modal, focus trap, `aria-labelledby`, initial focus in the input, focus returns to the field on close, Escape cancels.
- A visually hidden live region announces: "Selected {text}.", "New address added and selected.", "Address updated.", "Address 1 cleared."
- Status line: `role="status"`, `aria-live="polite"`.
- Do not rely on color alone: states also change the status text. Respect `prefers-reduced-motion`.

## 11. Edge cases

- Whitespace-only text behaves as empty everywhere.
- Very long selected addresses wrap inside the box; the box grows, nothing is cut off.
- Opening the Add dialog with typed text that already exists as a saved address: validation shows the duplicate message; the user closes and picks it.
- Renaming to the same text, or only changing case or diacritics of itself, is allowed.
- Add while an address is already selected is possible; the new address replaces the selection.
- Field disabled or read-only: no + button, no pen, no x; the selected text is shown plainly.
- User lacks permission to add or edit (open question 4): hide the + button and the pen icon; the selected value can still be cleared.
- Saved list fails to load: the field shows "Addresses unavailable", an already selected value stays visible and clearable, + is disabled; Create is not blocked.
- Dropdown and dialog must not be clipped by scrolling containers (portal if the form uses `overflow: hidden`).
- If the selected address was deleted by someone else, a rename returns 404: show "This address no longer exists." and clear the selection on close.

## 12. Acceptance criteria

- [ ] Address 1 is no longer a dropdown, and Address 1 and Address 2 are the same component with identical width, minimum height (84px) and styling at all breakpoints.
- [ ] Typing shows up to 6 ranked suggestions with highlighted matches and "Used N×"; `asgabat` finds `Aşgabat`; multi-word queries match in any order.
- [ ] Picking a suggestion selects it and shows the selected-value state with pen and x; the border turns green.
- [ ] Typing something that matches nothing and pressing Enter, or leaving the box, creates nothing and discards the text; the status points to the + button.
- [ ] Each field has a + button outside the box; it opens the Add dialog prefilled with the typed text.
- [ ] Add validates (empty, too long, duplicate), saves immediately, selects the new address, clears the search text and returns focus correctly.
- [ ] Clicking the selected value opens the Edit dialog prefilled with its text; saving renames it and updates the field and the list; duplicates are rejected.
- [ ] No way exists to add or edit an address without a dialog.
- [ ] Cancel, x, Escape and backdrop click close a dialog without changes and keep the typed search text.
- [ ] The x button clears the selected value and returns to the search state.
- [ ] Empty Address 1 blocks Create with the existing red row and banner; Address 2 is optional.
- [ ] Creating the application increases the use count of the selected addresses, atomically, and they rank higher next time.
- [ ] Existing applications open with the same Address 1 and Address 2 values; nothing is dropped.
- [ ] Fully keyboard operable (search, selected value, both dialogs) and announced by a screen reader; focus returns correctly after dialogs.
- [ ] No saved text is ever rendered as raw HTML.
- [ ] The old Address 1 dropdown code is removed.

## 13. Suggested tests

- Unit: fold, tokenize, rank, highlight ranges, validation (empty, length, duplicate excluding self, case/diacritic-only rename).
- Component: list opens only while typing; 6-result cap; Enter behavior (highlighted, exact match, nothing otherwise, never a line break); unmatched text creates nothing and is discarded on blur; + opens Add prefilled; Add selects the address; selected-value click opens Edit; rename updates field and list; duplicate errors; Cancel/Escape/backdrop leave data unchanged; focus return; x clears; required validation for Address 1.
- API: POST 409 on duplicate, PATCH 409/403/404, use counts inside the create-application transaction, rollback when create fails, concurrent adds of the same text.
- Visual: both fields identical in size, + button aligned, selected-value state with long text, dialog readable in all themes and at narrow widths.

## 14. Open questions

Defaults used in the prototype are in brackets.

1. **What does "edit" change?** [Renames the saved address itself, so every application referencing it by id shows the new text.] If applications store the address as text, older applications keep the old text. If other systems (reports, letters, integrations, government forms) rely on these values, renaming could change documents unexpectedly. Should edit instead create a corrected copy for this application only? Also: the old Address 1 dropdown is probably fed by a lookup table; is it acceptable for users to add or change entries there? [Separate saved list per field, `source = user`, seeded from the lookup.]
2. **Scope:** are added and renamed addresses shared by the whole organization or private to each user? [Whole organization.]
3. **Save timing:** save immediately on the dialog's button (prototype) or only when "Create application" is pressed? Immediate is simpler for shared lists, but a cancelled application still leaves the new or renamed address behind. [Immediate.]
4. **Permissions:** can every user add and rename addresses, or only some roles? [Everyone.]
5. **Address 2:** it looks like a free-form detail field (apartment, floor, landmark). Should it really be restricted to pick-or-add through a dialog, or should Address 2 keep plain free typing? [Same behavior as Address 1, as requested.]
6. **Delete:** should the Edit dialog offer "Delete address" (only when unused)? [No.]
7. **Pen position:** the prototype makes the whole selected text a button with a pen icon, and puts only the + button outside the box. Should a separate pen button sit outside the box next to the +? [Pen on the selected value.]
8. **Near-duplicates:** should Add warn about similar addresses (not only exact matches), for example "Magtymguly 84" when "Magtymguly şaýoly 84" exists? [Exact (folded) duplicates only.]
9. **Structure:** is free text fine, or do addresses need separate city, district and street parts?
10. **Cyrillic:** do users also type Russian/Cyrillic addresses? If yes, search needs transliteration or alternate keys.
11. **Maintenance:** who cleans up typos and duplicates that get saved?
