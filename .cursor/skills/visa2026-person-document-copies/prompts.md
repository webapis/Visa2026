# Person document copies — user prompts

Copy-paste in Cursor (`@visa2026-person-document-copies` when available).

## Design / planning

- Draft Person document copies section layout for Passports, Visas, Education — follow PERSON_DOCUMENT_COPIES.md.
- How should Person document copies differ from ApplicationItem document copies?
- Add Person document copies to the global preview slot — what files do we need?

## Implementation (phase 1)

- Implement `PersonLinkedDocumentsResolver` and `PersonDocumentCopiesController` (DetailView only).
- Wire `PersonDocumentCopiesSlotPanel` + `OpenPersonDocumentCopiesAsync` occupant.
- Reuse document copy PDF preview for a single Passport's `PassportDocument` rows.

## UI

- Person ListView: add a Document copies column button (phase 2).
- Show Current badge using `PersonCurrentItems` in the catalog.
- Cross-link from Person catalog to ApplicationItem document copies when application lines exist.

## Triage

- Is this Person master-data preview or ApplicationItem ministry package? Route to the right skill.
