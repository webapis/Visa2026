# Issue work permit — preview-slot UI prototypes (person cards)

Case workspace **Issued records → + Add work permit** / **New work permit**. Host is `#visa-preview-slot` (same occupant as today’s header compose). **Prototype only — not implemented.**

Replaces the checkbox people table in [`issue-work-permit-slot-01-compose.png`](./issue-work-permit-slot-01-compose.png) with **per-employee cards**, same pattern as issued visa. Shared family: [`issue-issued-header-slot-README.md`](./issue-issued-header-slot-README.md).

| # | File | Screen |
|---|------|--------|
| 01 | [`issue-work-permit-slot-01-compose.png`](./issue-work-permit-slot-01-compose.png) | Compose — one letter header + employee cards |
| 02 | [`issue-work-permit-slot-02-prefill.png`](./issue-work-permit-slot-02-prefill.png) | Start/End from last work permit when still valid; expired last WP needs manual dates |
| 03 | [`issue-work-permit-slot-03-validation.png`](./issue-work-permit-slot-03-validation.png) | Block Create until required header and item fields are filled |
| 04 | [`issue-work-permit-slot-04-created.png`](./issue-work-permit-slot-04-created.png) | After create — stay in slot; workspace Work permit count = 1 |

## Locked UX (officer agreed 2026-08-26)

1. **Header vs lines:** one `WorkPermit` per case (`WorkPermit_ApplicationProfileInstanceSingleUse`). Many `WorkPermitItem` cards on that letter. Not one work-permit BO per person.
2. **People source:** case roster **employees only** (`Person.IsEmployee`). Guests are omitted. Linked skip-nav `WorkPermitItems` is not a source.
3. **Header fields:** work-permit number, issued date, work-permit copy (`WorkPermitDocument`). **AS number is not a header field.**
4. **Each employee card (required on `WorkPermitItem`):**
   - Item work-permit number
   - AS number
   - Position (`CurrentPositionHistory`) — dropdown, default current position
   - Passport — read-only from the person
   - Start and End (`ExpirationDate`)
   - Work permitted locations — same popup editor as DetailView (`WorkPermittedLocationName`), not two free-text dropdowns
5. **Prefill Start / End:** copy from the person’s **last work permit item** (`PersonCurrentItems.GetCurrentWorkPermitItem`) when that item is still valid (End ≥ today). If missing or expired, leave dates empty and show a note so the officer types them.
6. **Do not prefill** item number or AS number from the last work permit (officer types those). Locations may default from case `MovementPermitLocation`.
7. **Create** blocked until header number + every included card’s required fields are complete. After create, stay in the slot; Issued records Work permit count updates.

## Mock caveats (PNGs vs locked spec)

- **02** may show AS number on the header — ignore; AS stays on the person card only.
- **03** may show extra Location / Work location dropdowns — implement as one **Work permitted locations** control (Ýok + …), like visa Border zone.

## Stamps on Create (planned)

- `WorkPermit.ApplicationProfileInstance` = this case
- `WorkPermit.WorkPermitNumber` / `IssuedDate` / optional copy from header
- One `WorkPermitItem` per included employee: Person, Passport, CurrentPositionHistory, WorkPermitNumber, ASNumber, StartDate, ExpirationDate, WorkPermittedLocations

## Implementation (shipped 2026-08-26)

`IssueIssuedHeaderSlotPanel` WorkPermit kind uses per-employee cards. `IssueIssuedHeaderComposeService` stamps item number, AS number, position, Start/End, locations. Start/End copy from last valid `WorkPermitItem`. `EnsureRosterWorkPermitItems` does not add extra employees when compose already created lines.