# Cursor prompt: Location multi-select (Border zone and Work permit location)

Paste everything below the line into Cursor (Agent mode). Attach `location-multiselect-spec.md` and `location-multiselect.html` to the chat, or place them in the repo (for example in `/docs`) and reference them with `@`. Also attach `address-autocomplete-spec.md`, or mention that the Address autocomplete feature already exists in the repo if it does.

---

You are implementing a UI feature in this existing codebase. Work like a careful senior engineer: understand the code first, reuse what exists, change as little as needed, and keep the diff reviewable.

## Context

On the "Application" step (last step before "Create application"), the fields **Requested border zone location** and **Requested work permit location** are currently filled through a picker: a "..." button opens a modal with a long checkbox list (title "Border zones", "Selected: N", OK / Cancel), and the field shows the chosen values as one comma-joined, truncated string.

I want to **remove those picker dialogs** and replace each field with:

1. a **search box with chips** that uses the same dropdown and search as the Address 1 / Address 2 autocomplete, in the style of the Material UI Autocomplete "Multiple values" example (https://mui.com/material-ui/react-autocomplete/);
2. a **+ button outside the box** that opens an **Add location dialog**;
3. an **edit action on every chip** (click the chip; it shows a small pen icon) that opens an **Edit location dialog**.

**The key rule: the text input never creates or edits anything.** It only searches and selects existing saved locations. Adding a new location and renaming an existing one happen only in a dialog.

Read these first and treat them as the source of truth:

- `@location-multiselect-spec.md` for behavior, search rules, data, accessibility and acceptance criteria.
- `@address-autocomplete-spec.md` for the dropdown and search behavior this feature must match.
- `@location-multiselect.html` is a working prototype. Open it and mirror its behavior and feel. Do not copy its markup, CSS, storage code or sample data: it is a standalone demo. Use this codebase's own components, styling and real data.

## What to build

1. One reusable multi-select autocomplete component (box + chips) and **one shared dialog component** (modes `add` and `edit`) used by both fields. Each field has its own list of saved locations.
2. **Dropdown identical to the Address autocomplete dropdown:** opens only while the user types (never on an empty box and not on tab focus); up to 6 matching saved locations that are not already selected; ranked (starts with first token, then use count, then most recent); matched text highlighted; "Used N×" on the right ("New" when never used); overlay below the box; scrolls after about 252px.
3. Picking a suggestion (click, or Arrow keys then Enter) adds a chip, clears the text and closes the list. **Enter picks only a highlighted suggestion or a text that exactly equals a saved location; otherwise it does nothing** (and must not submit the form). Do not auto-highlight the first option. **Do not use `freeSolo`.**
4. Text that matches nothing creates nothing: the list disappears, the status says "No saved location matches “text”. Use the + button to add it as a new location.", and unpicked text is discarded when the box loses focus.
5. **+ button** (icon button, 34px square, outside the box, accessible name "Add new border zone location" / "Add new work permit location"): opens the Add dialog with the name input **prefilled with whatever was typed in the search box** and selected. On success the new location is created in that field's saved list **immediately**, selected as a chip, the search box is cleared, and focus returns to the box.
6. **Chip edit:** each chip has an edit area (label plus an always-visible small pen icon; click opens the Edit dialog prefilled with the current name) and a remove (x) area. Only locations that are chips can be edited; there is no other edit entry point. Saving renames the saved location immediately; the chip, the dropdown and every other place showing it update because chips read their label from the saved list by id.
7. **Dialog:** modal, centered, title ("Add border zone location", "Edit work permit location", etc.), one "Location name" input (max 100 chars), buttons Cancel / primary ("Add location" or "Save changes") and a close icon. Enter submits, Escape cancels, backdrop click cancels. Focus goes to the input on open, is trapped, and returns to the field's search box on close. Validation errors show under the input (`role="alert"`, `aria-invalid`, `aria-describedby`) and keep the dialog open: empty ("Enter a location name."), too long ("Use at most 100 characters."), duplicate in the same field by folded label excluding itself ("“X” already exists in this list. Close this dialog and search for it."). In Edit mode the hint shows "Used N×. Renaming changes this location wherever it is selected." Cancel changes nothing and keeps the text the user had typed in the search box.
8. Chips: removable, in the order added, each location once. Backspace in an empty input removes the last chip; a clear-all button appears (top-right inside the box) when anything is selected. Selected locations are hidden from the suggestions and return when their chip is removed. When the box is not focused and more than 3 are selected, show 3 chips plus a `+N` chip; focusing shows all. Make sure clicking a chip works in the collapsed state (prevent the focus change on mousedown of chip buttons so a re-render does not swallow the click). Long labels truncate with a tooltip. Box minimum height matches the other single-line inputs and grows as chips wrap. Chip edit buttons are keyboard focusable; chip remove buttons are not in the tab order.
9. Status line under the field with the states in section 5.7 of the spec.
10. Creating the application increases the use count of every selected location inside the same transaction.
11. Remove the "..." buttons, the old picker dialogs, and everything used only by them.

Search rules (identical to Address autocomplete):

- Fold diacritics per character, one-to-one so string lengths stay equal: `ç→c ş→s ž→z ý→y ň→n ä→a ö→o ü→u ı→i ğ→g İ→i`, then lowercase; collapse whitespace.
- Split the query into tokens; a saved location matches if every token appears anywhere in its folded label.
- Highlight by merging token match ranges and applying them to the original label. Render with text nodes, never raw HTML.
- **If the Address autocomplete code already exists in this repo, reuse its normalization/ranking/highlight code. If it does not, create it as a small pure shared module both features can use.**

Accessibility (details in the spec): ARIA combobox on the text input with `aria-controls`, `aria-expanded`, `aria-activedescendant`; list is `role="listbox"` with `aria-multiselectable="true"` and `role="option"` rows; focus stays in the input while navigating; chip edit/remove buttons have accessible names ("Edit X", "Remove X"); the dialog is modal with `aria-labelledby`; a visually hidden live region announces added, renamed, removed and cleared; the status line is `role="status"` with `aria-live="polite"`.

If the project uses Material UI, build the box with `Autocomplete` (`multiple`, `filterSelectedOptions`, `limitTags={3}`, `openOnFocus={false}`, `clearOnBlur`, no `freeSolo`, custom `filterOptions` using the shared module, `renderOption` for highlighting and "Used N×", custom chip rendering with the pen-icon edit area and its own delete), put the + `IconButton` beside it, and build the dialog with MUI `Dialog`. If not, implement the same behavior with the project's existing UI primitives. Do not add a new UI library for this.

## How to work

**Step 1: Explore, then report before coding.** Find and summarize:

- The Application step component and how the two fields and their old dialogs are implemented and wired (state, props, events).
- Where the old dialog options come from (API calls, lookup tables/stores, constants), whether the two dialogs use the same list or different ones, and **whether anything else (reports, integrations, exports, other screens, government forms) depends on those lookup rows, ids or codes**. This decides whether users may safely add or rename entries there.
- **How the field values are stored and sent today** (comma-joined labels? ids?), how existing saved cases load them back, and **what happens to existing applications when a location is renamed** (do they reference it by id or store the label text?).
- Whether there is a permission model for editing lookup data.
- The UI library, styling approach and design tokens, existing autocomplete/combobox/chip/dialog components, and the Address autocomplete implementation if it exists.
- The test setup and conventions.

Then give me a short plan: files to change, the value format you propose, **where user-added and renamed locations will be stored** (default: a separate saved-locations list per field flagged `source = user`, seeded from the existing lookup data; tell me if writing to the existing lookup tables is safe or risky), how existing data and the API stay compatible, and anything in the spec that conflicts with this codebase. Ask me only about real blockers. For the spec's "Open questions", use the stated defaults and tell me which assumptions you made.

**Step 2: Implement** after the plan, in clearly separated changes:

1. The shared search module (or reuse of the existing one) with unit tests.
2. The reusable multi-select component and the shared dialog component with their styles.
3. Backend: list endpoint (or reuse of the lookup endpoint), `POST` create (atomic upsert by field and folded label; 409 with the existing item on duplicate), `PATCH` rename (409 on duplicate, 403 without permission, 404 when gone), migration with a unique key on the folded label per scope and field, and the use-count increment inside the create-application transaction. Validate on the server with the same rules as the client (section 7.4 of the spec).
4. Wire both fields into the Application step: load saved locations once per step; hydrate existing values into chips (stored values that match no saved location must still appear as chips and must never be silently dropped); keep the create payload compatible with the backend (convert at the boundary if the API stays string-based).
5. Delete the old picker dialogs, the "..." buttons, and all code and styles only they used.

**Step 3: Verify.**

- Tests: search and ranking unit tests (diacritics such as `anew` → `Änew şäheri`, multi-token, ranking by use count); validation unit tests (empty, length, duplicate excluding self, case/diacritic-only rename allowed); component tests for list opening only while typing, 6-result cap, Enter behavior (highlighted, exact match, nothing otherwise), unmatched text creating nothing and being discarded on blur, + opening Add prefilled with typed text, Add selecting the new chip and returning focus, chip click opening Edit, rename updating chip and list, duplicate errors, Cancel/Escape/backdrop changing nothing and keeping typed text, `+N` collapse and chip click in the collapsed state, Backspace removal, clear all, long-label truncation, legacy value hydration; backend tests for create 409, rename 409/403/404, atomic upsert under concurrency, use counts inside the create transaction, rollback when create fails.
- Run the project's linter, type check and tests, and fix what you broke.
- Check visually: the + button aligns with the box, box height matches neighboring inputs when empty and grows with chips, the dropdown looks the same as the Address 1/Address 2 dropdown, dropdown and dialog are not clipped by the form container, everything works in the app's themes and at narrow widths.

## Constraints

- Match existing code style, naming, folder structure and design tokens. Do not change unrelated fields, routes or styling.
- Existing cases must open with the same selections they had.
- Neither field is required; zero selections must remain valid.
- If loading the saved locations fails, show chips of the stored value, disable the + button, and do not block Create.
- Do not leave TODOs, dead code, commented-out code or console logs.

## Done means

All acceptance criteria in section 12 of the spec are met and verified, tests and lint pass, and your final message lists what changed, where added and renamed locations are stored and why, how renames affect existing applications, the value format and compatibility approach you chose, the assumptions you made on the open questions, how to run the tests, and anything left for me to decide.
