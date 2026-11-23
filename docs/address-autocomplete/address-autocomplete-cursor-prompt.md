# Cursor prompt: Address 1 / Address 2 autocomplete with Add and Edit dialogs

This replaces the earlier `cursor-prompt.md`. Paste everything below the line into Cursor (Agent mode). Attach `address-autocomplete-spec.md` and `address-combobox.html` to the chat, or place them in the repo (for example in `/docs`) and reference them with `@`. If the Location multi-select feature (`location-multiselect-spec.md`) is already implemented or being built, mention that too: the two features share the dialog and the search module.

---

You are implementing a UI feature in this existing codebase. Work like a careful senior engineer: understand the code first, reuse what exists, change as little as needed, and keep the diff reviewable.

## Context

The "Application" step (last step before "Create application", breadcrumb "Profile · Approval legs · Application") has a required **Address 1** dropdown and an optional **Address 2** textarea.

I want to replace both with one reusable **search-and-select field**:

1. A **search box**: the user types, matching saved addresses appear in a dropdown, picking one **selects** it. Once selected, the box shows the address text with a pen icon and an x.
2. A **+ button outside the box** that opens an **Add address dialog**.
3. **Clicking the selected address** opens an **Edit address dialog**.

**The key rule: the text box never creates or edits anything.** It only searches and selects existing saved addresses. Adding a new address and correcting an existing one happen only in a dialog. Address 1 and Address 2 must be the same component with identical width and height (minimum height 84px, matching the current Address 2 textarea).

Read these first and treat them as the source of truth:

- `@address-autocomplete-spec.md` for behavior, search rules, data, API, accessibility and acceptance criteria.
- `@address-combobox.html` is a working prototype. Open it and mirror its behavior and feel (there is a "New search fields / Current form" toggle to compare). Do not copy its markup, CSS, storage code or sample data: it is a standalone demo using localStorage. Use this codebase's own components, styling and real data.

## What to build

1. One reusable single-value **`AddressAutocomplete`** component (search state + selected-value state) used by both fields, and one **shared dialog component** (modes `add` and `edit`). Each field has its own list of saved addresses.
2. **Search state:** multi-line text area (3 rows). Dropdown opens only while the user types (never on an empty box, not on tab focus), shows up to 6 matching saved addresses ranked (starts with first token, then use count, then most recent), matched text highlighted, "Used N×" on the right ("New" when never used), overlay below the box (never pushes the form), scrolls after about 252px. The currently selected address is not offered again.
3. Picking a suggestion (click, or Arrow keys then Enter) selects it, closes the list, and moves focus to the selected value's edit button. **Enter picks only a highlighted suggestion or a text that exactly equals a saved address; otherwise it does nothing, never inserts a line break and never submits the form.** Do not auto-highlight the first option. **Do not use `freeSolo`.**
4. Text that matches nothing creates nothing: the list disappears, the status says "No saved address matches “text”. Use the + button to add it as a new address.", and unpicked text is discarded when the box loses focus.
5. **Selected state:** the search box is replaced, inside the same border, by the full address text (wrapping, never truncated) with a pen icon after it, as one button that opens the Edit dialog, plus a small round x button at the top-right that clears the value and returns to the search state with focus in the search box. Border turns green while a saved address is selected. The edit button's accessible name is "Edit Address 1: {text}".
6. **+ button** (34px square icon button, outside the box, aligned to its top, accessible name "Add new Address 1" / "Add new Address 2"): opens the Add dialog with the input **prefilled with whatever was typed in the search box** and selected. On success the address is created in that field's saved list **immediately**, becomes the selected value (replacing any current one), the search text is cleared, and focus returns to the field.
7. **Dialog:** modal, centered, title ("Add Address 1", "Edit Address 2", etc.), one multi-line input (max 500 chars), buttons Cancel / primary ("Add address" or "Save changes") and a close icon. Enter submits, Shift+Enter inserts a line break (collapsed to a space on save), Escape cancels, backdrop click cancels. Focus goes to the input on open, is trapped, and returns to the field on close. Validation errors show under the input (`role="alert"`, `aria-invalid`, `aria-describedby`) and keep the dialog open: empty ("Enter an address."), too long ("Use at most 500 characters."), duplicate in the same field by folded text excluding itself ("This address already exists in the list. Close this dialog and search for it."). In Edit mode the hint shows "Used N×. Renaming changes this address wherever it is selected." Cancel changes nothing and keeps the text typed in the search box.
8. **Edit** renames the saved address immediately; the field and the saved list update because the field reads its text from the saved list by id. Only the selected address can be edited; there is no other edit entry point.
9. **Validation on Create application:** Address 1 stays required (existing red row and banner "Complete required Case summary fields before continuing. Missing: ..."); Address 2 stays optional. Creating the application increases the use count of the selected addresses inside the same transaction.
10. Status line under each field with the states in section 5.7 of the spec.
11. Remove the old Address 1 dropdown, the old Address 2 textarea, and everything used only by them.

Search rules:

- Fold diacritics per character, one-to-one so string lengths stay equal: `ç→c ş→s ž→z ý→y ň→n ä→a ö→o ü→u ı→i ğ→g İ→i`, then lowercase; collapse whitespace and line breaks.
- Split the query into tokens; an address matches if every token appears anywhere in its folded text.
- Highlight by merging token match ranges and applying them to the original text. Render with text nodes, never raw HTML.
- **If the Location multi-select feature already created a shared search module in this repo, reuse it. Otherwise create it as a small pure module that both features can use.**

Accessibility (details in the spec): the search box is an ARIA combobox with `aria-controls`, `aria-expanded`, `aria-activedescendant`, `aria-required` for Address 1; the list is `role="listbox"` with `role="option"` rows; focus stays in the box while navigating; the edit, clear and + buttons have accessible names; the dialog is modal with `aria-labelledby`; a visually hidden live region announces selected, added, updated and cleared; the status line is `role="status"` with `aria-live="polite"`.

If the project uses Material UI, build the search state with `Autocomplete` (single, `openOnFocus={false}`, `clearOnBlur`, no `freeSolo`, custom `filterOptions`, `renderOption` for highlighting and "Used N×"), render the selected state with a custom component, put the + `IconButton` beside it, and build the dialog with MUI `Dialog`. If not, implement the same behavior with the project's existing UI primitives. Do not add a new UI library for this.

## How to work

**Step 1: Explore, then report before coding.** Find and summarize:

- The Application step component and how Address 1 and Address 2 are rendered, wired and validated (state, props, events, the required-field banner).
- **Where the Address 1 dropdown options come from** (API, lookup table, store, constants), and **whether anything else (reports, letters, integrations, exports, other screens) depends on those rows, ids or text**. This decides whether users may safely add or rename entries there.
- **How Address 1 and Address 2 are stored and sent today** (id or text), how existing saved cases load them back, and **what happens to existing applications when an address is renamed** (reference by id or copied text?).
- Whether there is a permission model for editing lookup data.
- The UI library, styling approach and design tokens, any existing autocomplete/combobox/dialog components, and whether the Location multi-select or shared search module already exists.
- The test setup and conventions.

Then give me a short plan: files to change, the value format you propose, **where user-added and renamed addresses will be stored** (default: a separate saved-addresses list per field flagged `source = user`, seeded from the existing Address 1 lookup; tell me if writing to the existing lookup is safe or risky), how existing data and the API stay compatible, and anything in the spec that conflicts with this codebase. Ask me only about real blockers. For the spec's "Open questions", use the stated defaults and tell me which assumptions you made.

**Step 2: Implement** after the plan, in clearly separated changes:

1. The shared search module (or reuse) with unit tests.
2. The reusable `AddressAutocomplete` component and the shared dialog component with their styles.
3. Backend: list endpoint (or reuse of the existing one), `POST` create (atomic upsert by field and folded text; 409 with the existing item on duplicate), `PATCH` rename (409 on duplicate, 403 without permission, 404 when gone), migration with a unique key on the folded text per scope and field, and the use-count increment inside the create-application transaction. Validate on the server with the same rules as the client (section 7.4 of the spec).
4. Wire both fields into the Application step: load saved addresses once per step; hydrate existing values (stored text that matches no saved address must be created in the list on load or first save, never dropped); keep the create payload compatible with the backend (convert at the boundary if the API stays string-based); keep the existing Address 1 required validation and banner.
5. Delete the old Address 1 dropdown, the old Address 2 textarea, and all code and styles only they used.

**Step 3: Verify.**

- Tests: search and ranking unit tests (diacritics such as `asgabat` → `Aşgabat`, multi-token, ranking by use count); validation unit tests (empty, length, duplicate excluding self, case/diacritic-only rename allowed); component tests for list opening only while typing, 6-result cap, Enter behavior (highlighted, exact match, nothing otherwise, never a line break or form submit), unmatched text creating nothing and being discarded on blur, + opening Add prefilled with typed text, Add selecting the new address and returning focus, selected-value click opening Edit, rename updating field and list, duplicate errors, Cancel/Escape/backdrop changing nothing and keeping typed text, x clearing, Address 1 required validation, Address 2 optional, long-address wrapping; backend tests for create 409, rename 409/403/404, atomic upsert under concurrency, use counts inside the create transaction, rollback when create fails.
- Run the project's linter, type check and tests, and fix what you broke.
- Check visually: Address 1 and Address 2 render at identical size, the + button aligns with the box, the selected-value state with a long address, dropdown and dialog not clipped by the form container, themes and narrow widths.

## Constraints

- Match existing code style, naming, folder structure and design tokens. Do not change unrelated fields, routes or styling.
- Existing applications must open with the same Address 1 and Address 2 they had.
- If loading the saved addresses fails, keep any selected value visible and clearable, disable the + button, and do not block Create.
- Do not leave TODOs, dead code, commented-out code or console logs.

## Done means

All acceptance criteria in section 12 of the spec are met and verified, tests and lint pass, and your final message lists what changed, where added and renamed addresses are stored and why, how renames affect existing applications, the value format and compatibility approach you chose, the assumptions you made on the open questions, how to run the tests, and anything left for me to decide.
