# Issued header compose — preview-slot UI prototypes (shared process)

**One process** for every May-produce output header from case workspace **Issued records** / **New …**:

| Trigger | Header BO | Line BO | Prototypes |
|---------|-----------|---------|------------|
| **New invitation** | `Invitation` | `InvitationItem` | `issue-invitation-slot-01` … `04` |
| **New work permit** | `WorkPermit` | `WorkPermitItem` | [`issue-work-permit-slot-README.md`](./issue-work-permit-slot-README.md) (`01`–`04` person cards) |
| **New rejection** | `Rejection` | `RejectionItem` | `issue-rejection-slot-01` |
| **New border zone** | `BorderZone` | `BorderZoneItem` | `issue-border-zone-slot-01` |
| **New issued visa** (Path A) | `Visa` | issued `InvitationItem` (consumption) | `issue-issued-visa-slot-01` … `04` — [README](./issue-issued-visa-slot-README.md) |

Family overview: [`issue-issued-header-slot-00-family-overview.png`](./issue-issued-header-slot-00-family-overview.png)

## Shared UX (locked)

1. **Host:** `#visa-preview-slot` occupant (not modal XAF DetailView). One occupant at a time.
2. **Compose:** header fields (BO-specific) + people from the instance roster. Invitation / rejection / border zone keep the include table. **Work permit** uses per-employee cards (item fields) — see [`issue-work-permit-slot-README.md`](./issue-work-permit-slot-README.md).
3. **Lines = issued output only** — never input M2M Linked records (`ApplicationProfileInstance.InvitationItems` / etc.).
4. **Validate** → block Create when required person data missing (e.g. no passport).
5. **Create** stamps `*.ApplicationProfileInstance` + creates selected line BOs (roster helpers).
6. **After create:** stay in slot; workspace Issued records count updates. Issued visa Path A is a separate occupant (`issue-issued-visa-slot-*`); do not put **Issue visa** on invitation compose.

## Header field differences (compose form)

| Family | Header fields (prototype) |
|--------|---------------------------|
| Invitation | Number, Issued, Expiration, Visa category, Visa period, Border zone |
| Work permit | Number, Issued date, copy. Per-employee cards: item number, AS number, position, passport (read-only), Start, End, work-permitted locations |
| Rejection | Rejected doc number, Date, Reason |
| Border zone | Number, Start date, Validity duration, Expiration (computed) |

## Visibility

Tiles / New buttons follow profile **May produce** (`ShowInvitations`, `ShowWorkPermits`, `ShowRejections`, `ShowBorderZones`).

## Implementation (shipped)

Shared Blazor shell `IssueIssuedHeaderSlotPanel` + `IssueIssuedHeaderComposeService`, mode `VisaPreviewSlotMode.IssueIssuedHeader`, open via `ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose` from workspace **New invitation / work permit / rejection / border zone**. **New issued visa** is `VisaPreviewSlotMode.IssueIssuedVisa` — invitation lines ([`issue-issued-visa-slot-README.md`](./issue-issued-visa-slot-README.md)) or case roster ([`issue-issued-visa-instance-slot-README.md`](./issue-issued-visa-instance-slot-README.md)).

## Next

- F5 smoke on Inv/WP/RJ/BZ compose kinds and Path A **+ Add issued visa**
