# Location Multi-Select (Border Zone and Work Permit Location)

Feature specification for the **Application** step ("Choose Application Profile", last step before create).

Status: prototype ready, pending answers to the open questions in section 13.
Reference prototype: `location-multiselect.html` (open in a browser; it is the source of truth for behavior and feel).

**This feature reuses the Address Autocomplete behavior** (`address-autocomplete-spec.md`): same dropdown, same search strategy, same "type a new value and it gets saved" rule. The only differences are that these fields accept **several values shown as chips** (like the Material UI Autocomplete "Multiple values" example, https://mui.com/material-ui/react-autocomplete/) and that Address 1 and Address 2 hold a single value. Reuse the Address autocomplete code (search, ranking, highlight, saved-list storage) if it already exists.

---

## 1. Summary

The fields **Requested border zone location** and **Requested work permit location** are currently filled through a checkbox picker dialog (the "..." button opens "Border zones" and a similar dialog for work permit locations).

Remove the dialogs. Replace each field with a **searchable multi-select with chips**:

- The user types; a dropdown of matching saved locations appears (identical to the Address 1/Address 2 dropdown).
- Picking a suggestion adds it as a chip.
- If what the user typed is not in the list, pressing Enter (or leaving the field) **adds it automatically as a new chip**. It is saved when the application is created and suggested next time.

## 2. Problem

- Every change needs the "..." button, a modal, scrolling a long checkbox list (25+ rows), then OK.
- The list cannot be searched.
- The field shows only a comma-joined, truncated string (for example `Türkmenbaşy şäheri, Türkmenbaşy etraby, Serdarabat e...`), so users cannot see or fix the selection without reopening the dialog.
- A location that is not in the list cannot be entered at all.

## 3. Goals

1. Select locations by typing, with the same diacritic-insensitive search as Address autocomplete.
2. Multiple values, shown as removable chips.
3. A value that is not in the list is added automatically and saved for next time.
4. No modal and no "..." button; everything is inline.
5. Keyboard and screen reader friendly.
6. Same behavior for both fields; each field has its own saved list.

## 4. Non-goals

- Editing, merging or deleting saved locations (see open question 8).
- Changing other Application fields or the Address fields.
- Geocoding or validating that a typed location really exists.
- Server-side search (lists are small; see section 8).

---

## 5. User experience

### 5.1 Layout

- The control sits in the existing label/control row, same width as neighboring inputs.
- Minimum height equals the other single-line inputs (34px in the prototype). It **grows** when chips wrap onto more lines; it never scrolls internally and never clips chips.
- Inside the control, left to right: chips, then the text input (fills the remaining space, minimum about 110px), and on the far right a **clear all** button (only when something is selected).
- No dropdown arrow button (the list opens by typing, like the address fields).
- The suggestion list opens **below** the control as an overlay (never pushes the form down), as wide as the control, styled exactly like the Address autocomplete list.
- A status line sits below the control (section 5.5).
- Remove the old "..." button and the dialog completely.

### 5.2 Behavior

| Step | What the user does | What happens |
|---|---|---|
| 1 | Focuses an empty field | No list. Status: "Search saved locations, or type a new one and press Enter." |
| 2 | Types text | List opens with up to **6** matching saved locations that are not already selected, matched text highlighted, each with "Used N×" on the right. Status: "N saved locations match. Pick one, or press Enter to add “text” as new." |
| 3 | Clicks a suggestion, or uses Arrow keys then Enter | Location is added as a chip, the text box is cleared, the list closes. |
| 4 | Types text and presses **Enter** with no suggestion highlighted | The typed text is added as a chip. If it equals a saved location (ignoring case and diacritics) the saved one is used; otherwise it becomes a **new** chip. |
| 5 | Types text that matches nothing | The list disappears. Status (info style): "New location. Press Enter to add “text”. It will be saved for next time when you create the application." |
| 6 | Types a comma | Same as Enter: the text typed so far is added. |
| 7 | Pastes text containing commas, semicolons or line breaks | Split into parts; each part is added like step 4. (Handy for the old comma-joined values.) |
| 8 | Leaves the field (Tab, click elsewhere) with text in the box | The text is **added automatically** like step 4, so nothing typed is lost. |
| 9 | Clicks the x on a chip | The chip is removed. A removed saved location returns to the suggestions. Focus stays in the field. |
| 10 | Presses Backspace in an empty text box | The last chip is removed. |
| 11 | Clicks "clear all" | All chips are removed. |
| 12 | Clicks Create application | Every **new** chip is saved to that field's list (use count 1). Every saved chip has its use count increased. Chips then lose their "new" style. |

Rules:

- **Enter only picks a suggestion if one is highlighted with the arrow keys or the mouse.** Otherwise Enter adds the typed text. (Same rule as the address fields, so a new value is never replaced by a similar one by accident.)
- Escape closes the list and keeps the typed text.
- The list never opens on an empty text box, and never opens just from tabbing into the field.
- Already selected locations are hidden from the suggestions. A location can be selected only once; adding one that is already selected does nothing and is announced ("X is already selected").
- Chips appear in the order they were added.
- Typed text is whitespace-normalized (trim, collapse spaces and line breaks) and limited to 100 characters.
- Neither field is required (matches the current form). Zero chips is valid.

### 5.3 Chips

- Pill shape, accent-tinted background, label plus a small x button.
- **New (not yet saved) chips** use a dashed outline and info color instead of the filled style, with the tooltip "(new, saved when you create the application)". After Create they turn into normal chips.
- Long labels (for example `BeýikSaparmyratTürkmenbaşyAd etraby`) truncate with an ellipsis inside the chip, never exceed the control width, and show the full label in a tooltip.
- **Collapsed state (`limitTags`):** when the field is not focused and more than **3** locations are selected, show the first 3 chips plus a `+N` chip. Focusing or clicking the field expands to all chips.

### 5.4 Suggestion row

Identical to Address autocomplete:

- Main text with matched fragments highlighted (bold plus accent color, no background), long text wraps.
- "Used N×" on the right in muted text.
- Active row (keyboard or hover) gets a subtle background.
- Maximum height about 252px, then scrolls; the active row scrolls into view.

### 5.5 Status line

One line below the control, announced politely to screen readers:

| Situation | Text | Style |
|---|---|---|
| Empty text, nothing selected | Search saved locations, or type a new one and press Enter. | neutral |
| Empty text, N selected | "N selected" (plus " · M new, saved when you create the application" when there are new chips) | success |
| Typed text equals a selected location | Already selected. | neutral |
| Typed text equals a saved location | Saved location. Press Enter to add it. | success |
| Typed text has matches | N saved locations match. Pick one, or press Enter to add “text” as new. | neutral |
| Typed text has no match | New location. Press Enter to add “text”. It will be saved for next time when you create the application. | info |

---

## 6. Search behavior

Identical to Address autocomplete, section 6.

1. **Fold:** per-character map `ç→c ş→s ž→z ý→y ň→n ä→a ö→o ü→u ı→i ğ→g İ→i`, then lowercase. One-to-one so string length does not change (highlighting relies on it). Typing `anew` finds `Änew şäheri`.
2. **Tokens:** split the query on whitespace. A saved location matches when **every token appears anywhere** in its folded label.
3. **Ranking:** locations whose folded label **starts with the first token** first, then higher use count, then more recently used.
4. **Limit:** show at most **6**; also keep the total count for the status line.
5. **Exact match:** folded, whitespace-normalized equality with a saved location. When the typed text is an exact match the list closes and the status says "Saved location".
6. **Highlight:** merge token match ranges in the folded text and wrap the same ranges of the original label. Build DOM from text nodes, never raw HTML.

## 7. Data model

### 7.1 Saved locations

One list per field (border zone, work permit location). Suggested table (adapt to the existing schema; the current dialogs are probably fed by lookup tables, see open question 1):

```
saved_location
  id              uuid / bigint
  field           enum('border_zone', 'work_permit_location')
  label           string        -- as typed (whitespace normalized)
  label_folded    string        -- normalized search key (section 6.1), indexed
  use_count       int           -- starts at 1 for user-added entries
  last_used_at    timestamp
  source          enum('lookup', 'user')   -- seeded from the old lists vs added by a user
  created_at, created_by
  scope_id        tenant / organization id (see open question 2)
  UNIQUE (scope_id, field, label_folded)
```

Seed this list from the lookup data that feeds the dialogs today, so existing locations keep working.

### 7.2 Field value

Each field holds an **array of selected locations**. Each item is `{ id, label }` where `id` is null for a location that is new in this form submission.

Prototype output for reference:

```
value:  [{"id":"bz01","label":"Daşoguz şäher"},{"id":null,"label":"Köpetdag etraby"}]
legacy: Daşoguz şäher, Köpetdag etraby
```

The "legacy" line is how the dialog displayed the value. How the value is stored today is not confirmed (open question 3); it looks like a comma-joined string of labels.

### 7.3 Migration of existing data

If the backend currently stores a comma-joined string:

- Either keep the string in storage and convert at the boundary (split on `,`, trim, match to saved locations by folded label, join back on save), or migrate to an array.
- On load, a stored label that matches a saved location shows as a normal chip. A stored label that matches nothing shows as a normal chip too (it is simply a value; it will be saved as a location on the next create). **Never drop stored values silently.**

## 8. API and loading

- Search/list: reuse the existing lookup endpoint(s) if possible; otherwise add `GET /api/saved-locations?field=border_zone` returning `[ { id, label, useCount, lastUsedAt } ]`.
- Load each list once when the Application step opens and filter on the client (tens of items). If a list ever grows past about 500 items, move search to the server with debounce (about 150 ms), request cancellation and the same ranking, returning at most 6 plus a total count.
- Save: inside the existing create-application request or transaction, for each selected location:
  - `id` present: increase `use_count`, set `last_used_at`.
  - `id` null: **upsert** by `(scope_id, field, label_folded)`: insert with `use_count = 1`, or on conflict increase `use_count` and use the existing row. The upsert must be atomic so two users adding the same new location at once do not create duplicates.
  - If application creation fails, nothing is saved.
- Validation: trim, collapse whitespace, max 100 characters, ignore empty items, ignore duplicates within the same field, cap the number of selections if a limit is agreed (open question 4).

## 9. Client implementation notes

- Build one reusable `MultiSelectAutocomplete` (or extend the Address autocomplete component to support `multiple`). Props along the lines of: `field`, `value` (array of `{ id, label }`), `onChange`, `placeholder`, `emptyHint`, `limitTags` (default 3), `disabled`, `readOnly`.
- If the project uses Material UI, use `Autocomplete` with `multiple`, `freeSolo`, `filterSelectedOptions`, `limitTags={3}`, `openOnFocus={false}`, `autoSelect` (adds typed text on blur), a custom `filterOptions` and `renderOption` using the shared fold/token/rank/highlight module, and a `renderValue`/`renderTags` that draws new chips with the dashed style. With `freeSolo` MUI returns typed text as a string; map it to `{ id: null, label }` (or to the matching saved location) in `onChange`. Remember MUI does not auto-highlight the first option with `freeSolo` unless `autoHighlight` is set; keep it **off** so Enter adds the typed text.
- Otherwise build it on the project's existing UI primitives to the same behavior.
- Keep search, rank and highlight helpers in one pure module shared with Address autocomplete.
- Delete the picker dialog components, their open/close state, the "..." buttons and any styles used only by them.

## 10. Accessibility

Same combobox pattern as Address autocomplete, with a multi-select listbox:

- Text input: `role="combobox"`, `aria-autocomplete="list"`, `aria-expanded`, `aria-controls`, `aria-activedescendant`; labelled by the row label.
- List: `role="listbox"` with `aria-multiselectable="true"`; rows `role="option"` with `aria-selected`.
- Focus stays in the text input; options are never focused directly.
- Chip remove buttons have an accessible name ("Remove Etrek etrap"); they are reachable by mouse and by Backspace from the input and do not need to be in the tab order.
- A visually hidden live region announces: "Added X. N selected.", "Added new location X. N selected.", "X is already selected.", "Removed X. N selected.", "All locations cleared."
- The status line has `role="status"`, `aria-live="polite"` and is linked with `aria-describedby`.
- Keyboard: Arrow Down/Up move through suggestions (wrap), Enter adds the highlighted suggestion or else the typed text, comma adds typed text, Escape closes the list, Backspace in an empty input removes the last chip.
- New chips differ by dashed outline plus tooltip and text in the status line, not by color alone.
- Respect `prefers-reduced-motion`.

## 11. Edge cases

- Whitespace-only text behaves as empty.
- Enter or blur on empty text does nothing.
- Typing the same location with different case, spacing or diacritics resolves to the saved one.
- 20+ chips: the control grows and the layout below shifts down. The collapsed `+N` mode applies when the field is not focused.
- Accidental leave-the-field with half-typed text adds it as a chip; the user removes it with the chip x or Backspace. (Deliberate trade-off, see open question 6.)
- Field disabled: chips visible without x buttons, no list. Read-only: same.
- Saved lists fail to load: the field still works as free entry; show no suggestions and do not block Create.
- The popup must not be clipped by scrolling containers (render in a portal if the form container uses `overflow: hidden`).

## 12. Acceptance criteria

- [ ] The "..." buttons and checkbox dialogs are gone for both fields.
- [ ] Typing shows up to 6 ranked suggestions with highlighted matches and "Used N×", styled like the Address 1/Address 2 dropdown.
- [ ] `anew` finds `Änew şäheri`; multi-word queries match in any order.
- [ ] Clicking a suggestion, or arrow keys plus Enter, adds a chip, clears the text and closes the list.
- [ ] Enter with no highlighted suggestion adds the typed text; it uses a saved location when it matches exactly, otherwise creates a new chip.
- [ ] Comma, paste of a comma-separated string, and leaving the field with text all add chips.
- [ ] When nothing matches, the list disappears and the status shows the "New location" message.
- [ ] New chips look different (dashed) until the application is created.
- [ ] Creating the application saves new locations to that field's list and increases use counts of reused ones, without duplicates; they are suggested next time.
- [ ] Selected locations are hidden from suggestions and return when their chip is removed.
- [ ] Chip x, Backspace in an empty input and "clear all" remove chips correctly.
- [ ] Not focused with more than 3 chips shows 3 plus `+N`; focusing shows all.
- [ ] Long labels truncate inside chips with a tooltip; nothing overflows.
- [ ] Control height matches neighboring inputs when empty and grows with chips.
- [ ] Existing saved values load as chips and are never dropped.
- [ ] Fully keyboard operable and announced by a screen reader.
- [ ] No label is ever rendered as raw HTML.
- [ ] Dialog code and its styles are removed.

## 13. Open questions

Defaults used in the prototype are in brackets.

1. **Lookup data:** the old dialogs seem to be fed by lookup tables (the app tree has Lookup/General/Geography). Is it acceptable that users add new entries, and do other systems (reports, integrations, government forms) depend on the lookup ids or codes? If yes, new entries need a different home or an approval step. [Separate saved list per field, flagged `source = user`.]
2. **Scope:** are new locations shared by the whole organization or private to each user? [Whole organization.]
3. **Storage format:** how is the value stored today (comma-joined labels, ids, other) and can the API accept an array of `{ id, label }`? Do existing cases need migrating? [Convert at the boundary if the API stays string-based.]
4. **Limits:** minimum or maximum number of selections, or is either field ever required? [No limits, optional.]
5. **Same list or different lists:** do both fields share one set of locations? The two dialogs showed different lists. [Separate list per field.]
6. **Add on leaving the field:** should leaving the field with typed text add it automatically? [Yes; the alternative is to keep the text uncommitted until Enter.]
7. **Browsing:** the old dialog let users browse the whole list. The new field only lists on typing (like Address). Should ArrowDown on an empty field show the most-used locations? [No.]
8. **Maintenance:** who can edit, merge or delete saved locations (typos get saved too, and the data already has near-duplicates such as `Serhetabat etr` and `Serhetabat etrap`)?
9. **Near-duplicates:** should "Serhetabat" typed with a small typo be offered as a match to the existing entry instead of creating a new one? [Only exact folded matches are merged.]
10. **Gear icon:** what does the gear icon in the old dialog do, and does anything need to replace it?
11. **Stack:** is Material UI already in the project?
12. **Cyrillic:** do users also type Russian/Cyrillic location names? If yes, search needs transliteration or alternate keys.
