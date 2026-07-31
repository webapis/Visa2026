# Person dossier — user prompts

Copy-paste in Cursor (`@visa2026-person-dossier` when available).

## Design / planning

- Follow PERSON_DOSSIER.md: keep dossier in main area; copies in preview slot.
- Design Phase 2 deep-link from a dossier section row into person document copies.
- Should section lists be capped with Show all? Product decision — do not invent silently.

## Implementation

- Add a dossier section / status tile using PersonDossierResolver patterns.
- Fix dossier loading UX (staged progress + skeleton).
- Fix Screen | Paper so Paper uses BuildFragment and does not open the preview slot.
- Fix director export ZIP folder layout (Visas/ vs nested under Passports/).

## Triage

- Is this dossier page/export, Person search, copies catalog, or preview-slot shell? Route to the right skill.
- Open dossier from Person search closes copies — check OwnerViewId.