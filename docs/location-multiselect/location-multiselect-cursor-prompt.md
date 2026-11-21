# Cursor prompt: Location multi-select (Border zone and Work permit location)

Paste everything below the line into Cursor (Agent mode). Attach `location-multiselect-spec.md` and `location-multiselect.html` to the chat, or place them in the repo (for example in `/docs`) and reference them with `@`. Also attach `address-autocomplete-spec.md`, or mention that the Address autocomplete feature already exists in the repo if it does.

---

You are implementing a UI feature in this existing codebase. Work like a careful senior engineer: understand the code first, reuse what exists, change as little as needed, and keep the diff reviewable.

## Context

On the "Application" step (last step before "Create application"), the fields **Requested border zone location** and **Requested work permit location** are currently filled through a picker: a "..." button opens a modal with a long checkbox list (title "Border zones", "Selected: N", OK / Cancel), and the field shows the chosen values as one comma-joined, truncated string.

I want to **remove those picker dialogs** and replace each field with an inline **multi-select that works exactly like the Address 1 / Address 2 autocomplete** (same dropdown, same search, same "new value gets saved" rule), except that it holds **several values shown as chips**, in the style of the Material UI Autocomplete "Multiple values" example (https://mui.com/material-ui/react-autocomplete/).

Read these first and treat them as the source of truth:

- `@location-multiselect-spec.md` for behavior, search rules, data, accessibility and acceptance criteria.
- `@address-autocomplete-spec.md` for the dropdown and search behavior this feature must match.
- `@location-multiselect.html` is a working prototype. Open it and mirror its behavior and feel. Do not copy its markup, CSS, storage code or sample data: it is a standalone demo. Use this codebase's own components, styling and real data.

## What to build

1. One reusable multi-select autocomplete component used by both fields (or extend the Address autocomplete component with a `multiple` mode). Each field has its own list of saved locations.
2. **Dropdown identical to the Address autocomplete dropdown:** opens only while the user types (never on an empty box and not on tab focus); shows up to 6 matching saved locations that are not already selected, ranked (starts with first token, then use count, then most recent), matched text highlighted, "Used N×" on the right, overlay below the field, scrolls after about 252px.
3. Picking a suggestion (click, or Arrow keys then Enter) adds a **chip**, clears the text and closes the list.
4. **Auto-add of missing values:** if what the user typed is not in the list, Enter adds it as a **new chip** (dashed style). The same happens on comma, on paste of comma/semicolon/line-break separated text (split into several chips), and when the user leaves the field with text still in the box. If the text equals a saved location (ignoring case and diacritics) the saved one is used instead of creating a new one. When nothing matches, the list disappears and the status line says it is a new location that will be saved when the application is created.
5. **Enter only selects a suggestion if one is highlighted** with the arrow keys or mouse; otherwise Enter adds the typed text. Do not auto-highlight the first option.
6. On **Create application**, save every new chip to that field's saved list (atomic upsert, no duplicates) and increase the use count of reused ones. New chips then lose their dashed style and are suggested next time.
7. Chips have a remove (x) button; Backspace in an empty text box removes the last chip; a "clear all" button appears when anything is selected. Already selected locations are hidden from the suggestions and return when their chip is removed. Chips are in the order added; a location can be selected only once.
8. When the field is not focused and more than 3 are selected, show 3 chips plus a `+N` chip; focusing the field shows all chips. Long labels truncate inside chips with a tooltip. The field's minimum height matches the other single-line inputs and grows as chips wrap.
9. Status line under the field with the states listed in section 5.5 of the spec.
10. Remove the "..." buttons, the dialogs, and everything used only by them.

Search rules (identical to Address autocomplete):

- Fold diacritics per character, one-to-one so string lengths stay equal: `ç→c ş→s ž→z ý→y ň→n ä→a ö→o ü→u ı→i ğ→g İ→i`, then lowercase; collapse whitespace.
- Split the query into tokens; a saved location matches if every token appears anywhere in its folded label.
- Highlight by merging token match ranges and applying them to the original label. Render with text nodes, never raw HTML.
- **If the Address autocomplete code already exists in this repo, reuse its normalization/ranking/highlight/saved-list code. If it does not, create it as a small pure shared module that both features can use.**

Accessibility (details in the spec): ARIA combobox on the text input with `aria-controls`, `aria-expanded`, `aria-activedescendant`; list is `role="listbox"` with `aria-multiselectable="true"` and `role="option"` rows; focus stays in the input; chip remove buttons have accessible names; a visually hidden live region announces added, new, already selected, removed and cleared; the status line is `role="status"` with `aria-live="polite"`.

If the project uses Material UI, build it with `Autocomplete` (`multiple`, `freeSolo`, `filterSelectedOptions`, `limitTags={3}`, `openOnFocus={false}`, `autoSelect`, no `autoHighlight`, custom `filterOptions` using the shared module, `renderOption` for highlighting and "Used N×", `renderValue` for chips with a dashed style for new ones), mapping the typed strings that `freeSolo` returns to `{ id, label }` objects. If not, implement the same behavior with the project's existing UI primitives. Do not add a new UI library for this.

## How to work

**Step 1: Explore, then report before coding.** Find and summarize:

- The Application step component and how the two fields and their dialogs are implemented and wired (state, props, events).
- Where the dialog options come from (API calls, lookup tables/stores, constants), whether the two dialogs use the same list or different ones, and whether anything else (reports, integrations, exports, other screens) depends on those lookup rows, ids or codes.
- **How the field values are stored and sent today** (comma-joined labels? ids?) and how existing saved cases load them back into the field.
- The UI library, styling approach and design tokens, and any existing autocomplete/combobox/chip components, including the Address autocomplete implementation if it exists.
- The test setup and conventions.

Then give me a short plan: files to change, the value format you propose, **where new user-added locations will be stored** (default: a separate saved-locations list per field flagged `source = user`, seeded from the existing lookup data; tell me if inserting into the existing lookup tables is safe or risky), how existing data and the API stay compatible, and anything in the spec that conflicts with this codebase. Ask me only about real blockers. For the spec's "Open questions", use the stated defaults and tell me which assumptions you made.

**Step 2: Implement** after the plan, in clearly separated changes:

1. The shared search module (or reuse of the existing one) with unit tests.
2. The reusable multi-select component and its styles.
3. Backend: saved-locations endpoint (or reuse of the existing lookup endpoint), migration with a unique key on the folded label per scope and field, and an atomic upsert (insert or increment use count and update last used) executed inside the create-application flow so a failed create saves nothing. Validate on the server: trim, collapse whitespace, max 100 characters, ignore empty and duplicate items.
4. Wire both fields into the Application step: load saved locations once per step; hydrate existing values into chips (stored values that match no saved location must still appear as chips and must never be silently dropped); keep the create payload compatible with the backend (convert at the boundary if the API stays string-based).
5. Delete the picker dialogs, the "..." buttons, and all code and styles only they used.

**Step 3: Verify.**

- Tests: search and ranking unit tests (diacritics such as `anew` → `Änew şäheri`, multi-token, ranking by use count); component tests for list opening only while typing, 6-result cap, Enter adding typed text vs adding a highlighted suggestion, new chip for unmatched text, saved match used for exact text, comma and paste splitting, add on leaving the field, selected locations hidden then restored on removal, Backspace removal, clear all, `+N` collapse and expand, status messages, long-label truncation, legacy value hydration; backend tests for upsert without duplicates, concurrent adds of the same new location, and rollback when create fails.
- Run the project's linter, type check and tests, and fix what you broke.
- Check visually: height matches neighboring inputs when empty and grows with chips, the dropdown looks the same as the Address 1/Address 2 dropdown, the list overlays without pushing the layout and is not clipped by the form container, works in the app's themes and at narrow widths.

## Constraints

- Match existing code style, naming, folder structure and design tokens. Do not change unrelated fields, routes or styling.
- Existing cases must open with the same selections they had.
- Neither field is required; zero selections must remain valid.
- If loading the saved locations fails, the field must still work as free entry and Create must not be blocked.
- Do not leave TODOs, dead code, commented-out code or console logs.

## Done means

All acceptance criteria in section 12 of the spec are met and verified, tests and lint pass, and your final message lists what changed, where new locations are stored and why, the value format and compatibility approach you chose, the assumptions you made on the open questions, how to run the tests, and anything left for me to decide.
