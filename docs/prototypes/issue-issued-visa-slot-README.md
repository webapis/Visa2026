# Issue issued visa — preview-slot UI prototypes

Path A from case workspace **Issued records → + Add issued visa**. Host is `#visa-preview-slot` (not an XAF modal).

Diagram: [`invitation-item-issued-vs-input.mmd`](../diagrams/issued-visa-origin/invitation-item-issued-vs-input.mmd) · Canonical: [`APPLICATION_PROFILE_ISSUED_VISA_ORIGIN.md`](../APPLICATION_PROFILE_ISSUED_VISA_ORIGIN.md)

| # | File | Screen |
|---|------|--------|
| 01 | [`issue-issued-visa-slot-01-compose.png`](./issue-issued-visa-slot-01-compose.png) | Compose — per-person visa cards grouped by issued invitation |
| 02 | [`issue-issued-visa-slot-02-used-line.png`](./issue-issued-visa-slot-02-used-line.png) | Issued line that already has a visa is not selectable |
| 03 | [`issue-issued-visa-slot-03-validation.png`](./issue-issued-visa-slot-03-validation.png) | Block Create when no unused issued invitation line |
| 04 | [`issue-issued-visa-slot-04-created.png`](./issue-issued-visa-slot-04-created.png) | After create — stay in slot; workspace Issued visa count updates |

## Locked UX (officer agreed)

1. **One visa per person** on this `ApplicationProfileInstance` who already has an **issued invitation line** (`Invitation.ApplicationProfileInstance` → `InvitationItems`).
2. **Not a visa source:** input/linked M2M `ApplicationProfileInstance.InvitationItems` (cancel/change). People on the case with no issued invitation are omitted (footnote only).
3. **Per-person fields** (officer types; may differ): visa number (unique), type, category, period, **issued place**, issued date, expiration, **border zone**, **visa copy**. Pre-fill from invitation / case; still editable. Passport is read-only from the issued line.
4. **Layout:** one card per eligible person, grouped under each invitation letter (30 / 31). Include checked by default; officer may uncheck or Remove.
5. **Entry:** **+ Add issued visa** for create. Click an issued visa row to **edit** that visa in the same slot. Do not put **Issue visa** back on invitation compose.
6. **Extension / direct** (visa, no invitation): same slot, roster people — [`issue-issued-visa-instance-slot-README.md`](./issue-issued-visa-instance-slot-README.md).

## Stamps on Create

Each included unused line creates one `Visa`:

- `IssuingApplicationProfileInstance` = this case
- `IssuingInvitationItem` = that issued line
- `Passport` = passport on the line
- Invitation item marked used

## Implementation (shipped)

`VisaPreviewSlotMode.IssueIssuedVisa` + `IssueIssuedVisaSlotPanel` + `IssueIssuedVisaComposeService`. Open via **+ Add issued visa** when the profile produces visa (`CanOpenInSlot`). Invitation+visa uses issued invitation lines; visa-only uses the case roster. After create, stay in the slot; workspace Issued visa count refreshes.