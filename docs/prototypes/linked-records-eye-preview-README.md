# Linked records — document copy eye preview (2026-11-07)

Prototype + implementation notes for a per-row **eye** on the Linked records dialog so officers can verify scanned copies in `#visa-preview-slot` while Join/Save stays open.

Base dialog behaviour remains [application-profile-instance-linked-records-dialog-README.md](./application-profile-instance-linked-records-dialog-README.md). Person scan indexing/preview merge: [PERSON_DOCUMENT_COPIES.md](../PERSON_DOCUMENT_COPIES.md). Slot shell: [PREVIEW_SLOT.md](../PREVIEW_SLOT.md).

## Locked product decisions

| Topic | Decision |
|-------|----------|
| Eye placement | Fixed column **after label, before value** (not inside value text; not with `+` / `✎`) |
| Eye chrome | **Icon only** — no border, no outline box, no filled button. Quiet muted gray, less prominent than the gear |
| Eye labels | **No visible text** on the control. Unavailable state uses a fainter icon; tooltip **No copy** only |
| Which rows get an eye | Only linked-record kinds that **store document copies** (see table below) |
| Click | Open preview slot **straight on that record’s scan(s)** (merge-preview if multiple files); `stackWithCurrent` for multi-tab |
| No file | Show eye **disabled** (fainter) + tooltip **No copy** |
| Gear on | Eye always previews the **currently selected** dropdown choice |
| Popup + slot | Keep Linked records **open** while the right slot shows the PDF |
| Multiple eyes | Slot tab bar — one tab per opened property preview |

## Rows that get an eye (document-copy kinds)

| Linked record kind | Eye | Files from |
|--------------------|-----|------------|
| Passport | Yes | `PassportDocument` |
| Education | Yes | `EducationDocument` |
| Position | Yes | `EmployeePositionHistoryDocument` (order for assignment) |
| Address of residence | Yes | `AddressOfResidenceDocument` / `LodgingDocument` |
| Visa | Yes | `VisaDocument` |
| Invitation | Yes | `InvitationDocument` |
| Work permit | Yes | `WorkPermitDocument` |
| Medical | Yes | `MedicalRecordDocument` |
| Rejection | Yes | `RejectionDocument` |
| Border zone (item) | Yes | Parent `BorderZoneDocument` via header copies occupant |

## Rows that do **not** get an eye

| Kind / row | Why |
|------------|-----|
| Work duty / Gelmeginiň maksady | No document copies |
| Salary | No document copies |
| Travel history | No document copies |
| Requested border zone / WP **location chips** | Location multi-select, not a scan |

## Screens

| File | State |
|------|--------|
| [linked-records-eye-preview-01-overview.jpg](./linked-records-eye-preview-01-overview.jpg) | Overview: quiet borderless eyes after labels. |
| [linked-records-eye-preview-02-slot-tabs.jpg](./linked-records-eye-preview-02-slot-tabs.jpg) | Eye clicked: dialog stays open; right slot with tabs. |

## Implementation status

- `EmployeePositionHistory.Documents` / `EmployeePositionHistoryDocument` — order-for-assignment scans.
- Person document copies **Positions** section.
- Linked records Overview eye → person (or BZ header) preview with `stackWithCurrent`.