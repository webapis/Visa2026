# Address Autocomplete (Address 1 and Address 2)

Feature specification for the **Application** step ("Choose Application Profile", last step before create).

Status: prototype approved, ready for implementation.
Reference prototype: `address-combobox.html` (open in a browser; it is the source of truth for behavior and feel).

---

## 1. Summary

Replace the **Address 1** dropdown with a type-ahead text field, and give **Address 2** the same behavior.

The user starts typing and a list of matching saved addresses appears. They can pick one, or ignore the list and type a full new address. New addresses are saved when the application is created and are suggested next time.

No more scrolling through a long dropdown. No dead end when the address is not in the list.

## 2. Problem

- Address 1 is a required `<select>` ("— Select —"). Users must scroll or pick from a long list.
- If the address is not in the list, the user has no way to proceed.
- Address 1 is also visually different from Address 2 (shorter, different control), which looks inconsistent.

## 3. Goals

1. Address 1 is a free-text field with search suggestions instead of a dropdown.
2. Address 1 has exactly the same width and height as Address 2.
3. Address 2 gets the same suggestion behavior as Address 1, with its own list of saved entries.
4. Anything the user types that is not in the list is saved on create and offered next time.
5. Fully usable by keyboard and screen reader.

## 4. Non-goals

- Geocoding, map lookup, or third-party address validation.
- Editing or deleting saved addresses (separate admin feature, see Open questions).
- Changing any other field in the Application form.
- Cyrillic to Latin transliteration in search (see Open questions).

---

## 5. User experience

### 5.1 Layout and size

- Address 1 and Address 2 use the **same component, same width, same height, same padding, same border and radius**. Use Address 2's existing textarea styles as the baseline. Address 1 must not be shorter, narrower or styled differently.
- Prototype values for reference (use the app's own design tokens instead): textarea min-height 84px, padding `8px 32px 8px 10px`, 3 rows, vertical resize allowed.
- A small clear button (x) sits inside the top-right corner of the field when it has text.
- A one-line status message sits below the field.
- The suggestion list opens **below** the field as an overlay (it must not push the rest of the form down) and follows the field if the user resizes the textarea.

### 5.2 Behavior

| Step | What the user does | What happens |
|---|---|---|
| 1 | Focuses empty field | No list. Status: "Start typing to search saved addresses." (Address 2: "Optional. Start typing to search saved entries.") |
| 2 | Types text | List appears with up to 6 matching saved entries, matched text highlighted. Status: "N saved addresses match. Pick one, or keep typing a new address." |
| 3 | Clicks a suggestion, or presses Arrow keys then Enter | Field is filled with the saved text, list closes, status shows "Saved address" in green, field border turns green. |
| 4 | Keeps typing and nothing matches | List disappears (cleared). Status: "New address. It will be saved for next time when you create the application." |
| 5 | Types exactly a saved address | Treated as the saved address: list closes, status "Saved address". |
| 6 | Clicks Create application | Address 1 required check runs. If valid, a new Address 1 and/or Address 2 is saved; a reused one has its usage count increased. |

Rules:

- The list never opens on an empty field (only when the user types).
- **Enter does not select anything unless the user has highlighted an item with the arrow keys or the mouse.** This prevents accidentally replacing a new address the user is typing. With no highlighted item, Enter inserts a newline as usual.
- Escape closes the list and keeps the text.
- Clicking outside (blur) closes the list and keeps the text. The text is never cleared automatically.
- Editing the text after picking a suggestion turns it back into a normal typed value (saved or new status is recalculated).
- Clear button empties the field, closes the list, returns focus to the field.
- Address 1 stays **required**. Address 2 stays **optional**; an empty Address 2 is never saved.

### 5.3 Suggestion row

- Main text: the saved address with matched fragments highlighted (bold plus accent color, no background).
- Secondary text, right aligned: "Used N×" (how many times it was used).
- Active (keyboard or hover) row gets a subtle background.
- Long addresses wrap; they are never cut off.
- Max visible height about 252px, then the list scrolls.

### 5.4 Validation and errors

Keep the existing banner behavior:

- On Create with Address 1 empty: row turns red and the banner reads "Complete required Case summary fields before continuing. Missing: Project, Address 1." (list only what is missing).
- Once the user has attempted Create, the banner and row state update live as fields are filled.
- Whitespace-only counts as empty.

---

## 6. Search behavior

### 6.1 Normalization ("fold")

Matching must ignore case and Turkmen/Turkish diacritics, so typing `asgabat` finds `Aşgabat`.

Per-character map (keep it one-to-one so string length does not change; highlighting relies on that):

```
ç→c  ş→s  ž→z  ý→y  ň→n  ä→a  ö→o  ü→u  ı→i  ğ→g  İ→i
```

Then lowercase. Apply to both the query and the saved text.

Also collapse whitespace and newlines to single spaces when comparing or saving (`norm`).

### 6.2 Matching

1. Split the folded query on whitespace into tokens.
2. An address matches if **every token appears anywhere** in the folded address (order does not matter).
3. Rank: addresses whose folded text **starts with the first token** come first, then higher usage count, then more recently used.
4. Return at most **6** results. Also return the total match count for the status line.

### 6.3 Highlighting

Find every occurrence of every token in the folded text, merge overlapping ranges, and wrap those ranges (taken from the original text by index) in a highlight element. **Build the DOM from text nodes; never inject saved text as HTML** (XSS).

### 6.4 Exact match

Saved check = folded, whitespace-normalized equality with a saved entry.

---

## 7. Data model

Two independent lists: one for Address 1, one for Address 2.

Suggested table (adapt to the existing schema and naming):

```
saved_address
  id              uuid / bigint
  field           enum('address1', 'address2')
  text            string        -- as the user typed it (whitespace normalized)
  text_folded     string        -- normalized search key (see 6.1), indexed
  use_count       int           -- starts at 1
  last_used_at    timestamp
  created_at      timestamp
  created_by      user id
  scope_id        tenant / organization id   -- see Open questions
  UNIQUE (scope_id, field, text_folded)
```

## 8. API (proposed; align with the existing API style)

**Search**

```
GET /api/saved-addresses?field=address1&q=garas
-> 200 { "total": 3, "items": [ { "id": "...", "text": "Garaşsyzlyk şaýoly 12, ...", "useCount": 6 } ] }
```

- Returns at most 6 items, ranked as in 6.2, plus `total`.
- Empty or whitespace `q` returns an empty list.

**Save / reuse**

Do this inside the existing "create application" request or transaction, not as a separate round trip:

```
for each of address1, address2 (skip empty address2):
  upsert by (scope_id, field, text_folded):
    insert with use_count = 1
    or on conflict: use_count = use_count + 1, last_used_at = now()
```

Rules:

- The upsert must be atomic so two users creating the same new address at the same time do not create duplicates.
- If application creation fails, nothing is saved.
- Trim and collapse whitespace before saving. Enforce a max length (suggest 500 chars) on both client and server.

## 9. Client implementation notes

- Build one reusable component (for example `AddressAutocomplete`) used for both fields. Props: `field`, `required`, `label`, `placeholder`, `emptyHint`, `value`, `onChange`.
- Debounce server search by about 150 ms and ignore out-of-order responses (cancel the previous request). If the full list is small, loading it once and filtering locally (as the prototype does) is acceptable.
- Keep the component controlled by the form's existing state/validation library. The submitted value is the plain text string, same as today. No change to the shape of the form payload is required other than the save-on-create behavior above.
- Remove the old Address 1 `<select>` and any code that loads its options once nothing else uses them.
- Prefer the project's existing autocomplete/combobox primitive if it has one; extend it rather than writing a second one.

## 10. Accessibility

Follow the WAI-ARIA combobox pattern with a listbox popup:

- Field: `role="combobox"`, `aria-autocomplete="list"`, `aria-expanded`, `aria-controls` (list id), `aria-activedescendant` (active option id), `aria-required="true"` for Address 1.
- List: `role="listbox"`; rows: `role="option"` with `aria-selected`.
- Focus stays in the field the whole time; options are never focused directly.
- Status line has `role="status"` and `aria-live="polite"` and is linked with `aria-describedby`.
- Keyboard: ArrowDown opens or moves down (wraps), ArrowUp moves up (wraps), Enter selects the highlighted row, Escape closes.
- Clear button has an accessible name ("Clear Address 1").
- Do not rely on color alone: the saved/new states also change the status text.
- Respect `prefers-reduced-motion` for the small open animation.

## 11. Edge cases

- Query with only spaces: treated as empty, no list.
- Very long saved addresses: wrap inside the row.
- Pasted multi-line text: whitespace is normalized when searching and saving.
- Slow or failed search request: keep the field usable as a plain text field, show no list, and do not block Create. Optionally show "Suggestions unavailable".
- Field disabled or read-only: no list, no clear button.
- Same address typed with different case, spacing or diacritics resolves to the same saved entry (via folded key).
- Zero saved addresses (new tenant): field behaves as a plain text field with the "New address" status.

## 12. Acceptance criteria

- [ ] Address 1 is a text field, not a dropdown, and has identical width and height to Address 2 at all breakpoints.
- [ ] Typing in Address 1 shows up to 6 ranked suggestions with highlighted matches.
- [ ] Searching `asgabat` matches `Aşgabat`; multi-word queries match in any order.
- [ ] Picking a suggestion fills the field and shows the "Saved address" state.
- [ ] When nothing matches, the list disappears and the "New address" status shows; the typed text is kept.
- [ ] Enter never selects a suggestion unless one is highlighted.
- [ ] Creating an application with a new Address 1 saves it; the next application suggests it.
- [ ] Reusing a saved address increases its usage count instead of creating a duplicate.
- [ ] Address 2 behaves the same, is optional, and has its own separate list; empty Address 2 is never saved.
- [ ] Empty Address 1 blocks Create with the existing error banner and red row.
- [ ] Fully keyboard operable; screen reader announces options and status.
- [ ] No saved text is ever rendered as raw HTML.
- [ ] The old Address 1 dropdown code is removed.

## 13. Suggested tests

- Unit: fold/normalize function (diacritics, case, whitespace), token matching, ranking, highlight range merging.
- Component: open/close rules, keyboard navigation and wrap, Enter-only-when-highlighted, blur keeps text, clear button, status messages for each state.
- API: search ranking and limit, empty query, upsert increments instead of duplicating, concurrent creates of the same new address, rollback when application creation fails.
- Visual: Address 1 and Address 2 are the same size; list overlays instead of pushing layout; light and dark themes if the app has both.

## 14. Open questions

These are the decisions the prototype assumed. Confirm or change them before shipping.

1. **Scope of saved addresses:** shared by the whole organization (prototype assumption) or private to each user?
2. **When to save:** on Create application (prototype) or as soon as the user leaves the field?
3. **Enter behavior:** prototype requires a highlighted row. Should Enter pick the top suggestion automatically instead?
4. **Empty-focus suggestions:** should the list show the most-used addresses when the field is focused but empty? (Prototype: no.)
5. **Near-duplicates:** should "Magtymguly 84" be blocked or merged when "Magtymguly şaýoly 84" exists? (Prototype: only exact folded matches are merged.)
6. **Structure:** is free text fine, or do addresses need separate city, district and street parts?
7. **Cyrillic:** do users also type Russian/Cyrillic addresses? If yes, search needs transliteration or alternate keys.
8. **Maintenance:** who can edit, merge or delete saved addresses (typos get saved too)?
