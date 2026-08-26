# Issued header compose — preview-slot UI prototypes (shared process)

**One process** for every May-produce output header from case workspace **Issued records** / **New …**:

| Trigger | Header BO | Line BO | Prototypes |
|---------|-----------|---------|------------|
| **New invitation** | `Invitation` | `InvitationItem` | `issue-invitation-slot-01` … `04` |
| **New work permit** | `WorkPermit` | `WorkPermitItem` | `issue-work-permit-slot-01`, `04` |
| **New rejection** | `Rejection` | `RejectionItem` | `issue-rejection-slot-01` |
| **New border zone** | `BorderZone` | `BorderZoneItem` | `issue-border-zone-slot-01` |

Family overview: [`issue-issued-header-slot-00-family-overview.png`](./issue-issued-header-slot-00-family-overview.png)

## Shared UX (locked)

1. **Host:** `#visa-preview-slot` occupant (not modal XAF DetailView). One occupant at a time.
2. **Compose:** header fields (BO-specific) + **people on letter** table from instance roster (include checkboxes).
3. **Lines = issued output only** — never input M2M Linked records (`ApplicationProfileInstance.InvitationItems` / etc.).
4. **Validate** → block Create when required person data missing (e.g. no passport).
5. **Create** stamps `*.ApplicationProfileInstance` + creates selected line BOs (roster helpers).
6. **After create:** stay in slot; workspace Issued records count updates. Invitation-only: per-line **Issue visa** (`Visa.IssuingInvitationItem`).

## Header field differences (compose form)

| Family | Header fields (prototype) |
|--------|---------------------------|
| Invitation | Number, Issued, Expiration, Visa category, Visa period |
| Work permit | Number, Issued date (+ location defaults from instance when applicable) |
| Rejection | Rejected doc number, Date, Reason |
| Border zone | Number, Start date, Validity duration, Expiration (computed) |

## Visibility

Tiles / New buttons follow profile **May produce** (`ShowInvitations`, `ShowWorkPermits`, `ShowRejections`, `ShowBorderZones`).

## Implementation (shipped)

Shared Blazor shell `IssueIssuedHeaderSlotPanel` + `IssueIssuedHeaderComposeService`, mode `VisaPreviewSlotMode.IssueIssuedHeader`, open via `ApplicationWorkspaceIssueIssuedHeaderOpenHelper.TryOpenCompose` from workspace **New invitation / work permit / rejection / border zone**. Issued visa remains modal `TryCreate`. Invitation success UI keeps per-line **Issue visa**.

## Next

- F5 smoke on all four kinds; optional WP/RJ/BZ validation prototype frames
