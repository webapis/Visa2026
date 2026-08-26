# Issue issued visa (instance source) — preview-slot UI prototypes

Extension / direct from case workspace **Issued records → + Add issued visa** when the Application Profile **produces Visa but not Invitation** (e.g. visa + work-permit extension). Host is `#visa-preview-slot` (same shell as invitation Path A).

Diagram: [`instance-roster-issued-vs-input.mmd`](../diagrams/issued-visa-origin/instance-roster-issued-vs-input.mmd) · Overview: [`extension-profile.mmd`](../diagrams/issued-visa-origin/extension-profile.mmd) · Canonical: [`APPLICATION_PROFILE_ISSUED_VISA_ORIGIN.md`](../APPLICATION_PROFILE_ISSUED_VISA_ORIGIN.md) · Path A (invitation source): [`issue-issued-visa-slot-README.md`](./issue-issued-visa-slot-README.md)

| # | File | Screen |
|---|------|--------|
| 01 | [`issue-issued-visa-instance-slot-01-compose.png`](./issue-issued-visa-instance-slot-01-compose.png) | Compose — per-person visa cards from the case roster |
| 02 | [`issue-issued-visa-instance-slot-02-used-person.png`](./issue-issued-visa-instance-slot-02-used-person.png) | Person who already has a visa issued by this case is not selectable |
| 03 | [`issue-issued-visa-instance-slot-03-validation.png`](./issue-issued-visa-instance-slot-03-validation.png) | Block Create when every person on this case already has a visa |
| 04 | [`issue-issued-visa-instance-slot-04-created.png`](./issue-issued-visa-instance-slot-04-created.png) | After create — stay in slot; workspace Issued visa count updates |

## Locked UX (officer agreed 2026-08-26)

1. **People source:** people on this `ApplicationProfileInstance` (case roster). One visa per person. `IssuingInvitationItem` stays **null**.
2. **Not a visa source:** input/linked M2M `ApplicationProfileInstance.Visas` (and invitation items). Work permit is a **sibling issued tile**, not a visa source and not a card group.
3. **Issued records chrome:** show **Work permit** (if May produce WP) and **Issued visa**. Do **not** show an Invitation produce tile on this profile family.
4. **Layout:** one card per roster person under **People on this case** (not grouped by invitation number). Same per-person fields as Path A (number, type, category, period, issued place, dates, border zone, visa copy). Prefill from **case summary**, not an invitation header. Passport read-only from the person.
5. **Already issued:** a person who already has a visa with `IssuingApplicationProfileInstance` = this case is locked (Visa issued). Create is blocked when no unused person remains.
6. **Entry:** **+ Add issued visa** / **New issued visa**. Row click still edits in the same slot.

## Stamps on Create (planned)

Each included unused person creates one `Visa`:

- `IssuingApplicationProfileInstance` = this case
- `IssuingInvitationItem` = null
- `Passport` = current passport on that person

## Implementation (shipped 2026-08-26)

Same occupant as Path A (`VisaPreviewSlotMode.IssueIssuedVisa`). `CanOpenInSlot` is **May produce Visa**. `UsesInvitationSource` is false when the profile does not produce invitation: people from `ApplicationRosterHelper.GetRosterPeople`; Create stamps `IssuingApplicationProfileInstance` and leaves `IssuingInvitationItem` null. Linked skip-nav `Visas` is not a source. Uniqueness is **one visa per person on the case** (`Visa_IssuingApplicationProfileInstanceSingleUse`).