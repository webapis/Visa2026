# Cursor prompt: Address 1 / Address 2 autocomplete

Paste everything below the line into Cursor (Agent mode). Attach `address-autocomplete-spec.md` and `address-combobox.html` to the chat, or place them in the repo (for example in `/docs`) and reference them with `@`.

---

You are implementing a UI feature in this existing codebase. Work like a careful senior engineer: understand the code first, reuse what exists, change as little as needed, and keep the diff reviewable.

## Context

The "Application" step (last step before "Create application", breadcrumb "Profile · Approval legs · Application") has a required **Address 1** dropdown and an optional **Address 2** textarea. I want to replace the Address 1 dropdown with a type-ahead field and give Address 2 the same behavior.

Read these first and treat them as the source of truth:

- `@address-autocomplete-spec.md` for the full behavior, search rules, data model, API, accessibility and acceptance criteria.
- `@address-combobox.html` is a working prototype. Open it and mirror its behavior and feel. Do not copy its markup, CSS or storage code: it is a standalone demo using localStorage and sample data. Use this codebase's own components, styling system and data layer.

## What to build

1. **Address 1** becomes a text field (same control, same width, same height as Address 2) with a suggestion list. No dropdown.
2. While the user types, show up to 6 matching saved addresses below the field with the matched text highlighted. The user can pick one (click, or Arrow keys then Enter) or ignore the list.
3. If nothing matches, the list disappears, the typed text stays, and a status line says it is a new address that will be saved for next time.
4. When the application is created, save any new Address 1 value (and Address 2 if not empty) so it is suggested next time. If the value already exists, increase its usage count instead of creating a duplicate.
5. **Address 2** gets the same behavior with its own separate saved list. It stays optional; an empty Address 2 is never saved.
6. Address 1 stays required, with the existing red row and error banner behavior.

Key rules (details in the spec):

- Matching ignores case and Turkmen/Turkish diacritics (`asgabat` finds `Aşgabat`), splits the query into tokens, and requires every token to match anywhere in the address.
- Ranking: starts-with first token, then usage count, then most recently used.
- **Enter must not select a suggestion unless one is highlighted.** Escape closes the list. Blur closes the list and keeps the text. Never clear the user's text automatically.
- Suggestion text must be rendered as text, never as raw HTML.
- Follow the ARIA combobox pattern (combobox, listbox, option, `aria-activedescendant`, live status line).

## How to work

**Step 1: Explore, then report before coding.** Find and summarize:

- The component and file(s) for the Application step and where Address 1 and Address 2 are rendered.
- The UI library, styling approach and design tokens in use.
- How form state and validation work, and how the required-field banner is produced.
- Any existing autocomplete/combobox component I should reuse.
- Where the Address 1 dropdown options come from today (API, store, constants) and where the create-application request is built and handled on the backend.
- The test setup (unit, component, e2e) and conventions.

Then give me a short plan: files to change, the data model and API you propose (adapted to our existing patterns, not the spec's names if ours differ), and anything in the spec that conflicts with how this codebase works. Ask me only about real blockers. For anything in the spec's "Open questions", use the stated defaults and tell me which assumptions you made.

**Step 2: Implement** after the plan, in small logical commits or clearly separated changes:

1. A reusable autocomplete component used by both fields (props along the lines of `field`, `required`, `label`, `placeholder`, `emptyHint`, `value`, `onChange`), built on our existing primitives.
2. The search normalization and ranking as a small pure, well-tested module shared by client (and server if search happens there).
3. Backend: search endpoint and an atomic upsert (insert, or increment usage count and update last used) performed inside the create-application flow so a failed create saves nothing. Add the migration and a unique key on the normalized text per scope and field.
4. Wire both fields into the Application step, keep the existing validation and banner behavior, and remove the old Address 1 dropdown and any code only it used.

**Step 3: Verify.**

- Add tests: normalization and ranking unit tests; component tests for keyboard behavior, Enter-only-when-highlighted, blur keeps text, clear button and each status message; backend tests for ranking, empty query, upsert without duplicates, concurrent creates, and rollback on failed create.
- Run the project's linter, type check and tests, and fix what you broke.
- Confirm Address 1 and Address 2 render at identical size, the list overlays without pushing the layout, and everything works in the app's themes and at narrow widths.

## Constraints

- Match existing code style, naming, folder structure and design tokens. Do not introduce a new UI library or state manager.
- Do not change unrelated fields, routes or styling.
- Do not log or expose saved addresses beyond what the current feature already exposes.
- Enforce a max length (500 chars) and trim/collapse whitespace on both client and server.
- If search is slow or fails, the field must still work as a normal text field and Create must not be blocked.
- Do not leave TODOs, dead code, commented-out code or console logs.

## Done means

All acceptance criteria in section 12 of the spec are met and verified, tests and lint pass, and your final message lists what changed, the assumptions you made on the open questions, how to run the tests, and anything left for me to decide.
